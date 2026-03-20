# Design: RimWorld Vibe Playing — AI 辅助殖民地管理 Mod

> Spec: docs/specs/2026-03-21-vibe-playing.spec.md

## 1. 概述

"Vibe Playing" 是一个 RimWorld mod，让玩家以**自然语言**描述战略意图，由 LLM 分析殖民地现状并生成可执行方案。玩家只做高层决策（"下一步想做什么"），AI 负责细节执行（建筑设计、资源规划、优先级调整、态势分析）。

核心理念：**人决策，AI 执行**。

## 2. 已验证的技术基础

通过调研确认以下关键能力均已被现有 mod 证实可行：

| 能力 | 现有实现 | 来源 |
|---|---|---|
| RimWorld 内调用外部 LLM API | RimAI Framework (v4.2.1) | 支持 OpenAI / Ollama / Groq，流式+非流式 |
| 殖民地状态提取为结构化数据 | OpenRimWorldAI | `AI_Information` 类：仓库/研究/殖民者属性/殖民地 |
| 程序化下达 designation（狩猎/伐木/挖矿） | Colony Manager Redux | 6310 评分，长期维护 |
| AI 驱动 work tab 优先级 | Free Will mod | 根据 mood/skill/环境动态调整 |
| 建筑蓝图放置 | RimWorld 原生 API | `GenConstruct.PlaceBlueprintForBuild()` |
| 区域/Zone 创建与修改 | RimWorld 原生 API | `Zone` 子类 + `ZoneManager` |

## 3. 架构设计

```
┌─────────────────── RimWorld 进程 ───────────────────┐
│                                                      │
│  ┌──────────────┐   ┌─────────────────────────────┐ │
│  │  VibePlaying │   │    ColonyStateExtractor      │ │
│  │  Tab (UI)    │   │  ┌─────────────────────┐    │ │
│  │  - 策略输入  │   │  │ PawnSerializer      │    │ │
│  │  - 态势报告  │   │  │ ResourceSerializer  │    │ │
│  │  - 方案确认  │   │  │ BuildingSerializer  │    │ │
│  │  - 执行日志  │   │  │ ThreatSerializer    │    │ │
│  └──────┬───────┘   │  │ ResearchSerializer  │    │ │
│         │           │  │ PowerFoodSerializer │    │ │
│         │           │  └──────────┬──────────┘    │ │
│         │           └─────────────┤               │ │
│         │                         │ JSON           │ │
│  ┌──────▼─────────────────────────▼──────────────┐ │
│  │            VibePlayingCore                     │ │
│  │  - Tick 调度器（GameComponent）                │ │
│  │  - LLM 通信管理器（异步 HTTP）                │ │
│  │  - Action Schema 定义                         │ │
│  │  - 指令解析器（JSON → GameAction）            │ │
│  └──────────────────┬────────────────────────────┘ │
│                     │                               │
│  ┌──────────────────▼────────────────────────────┐ │
│  │            CommandExecutor                     │ │
│  │  - SetWorkPriority(pawn, work, priority)      │ │
│  │  - PlaceBlueprint(def, pos, rot, stuff)       │ │
│  │  - DesignateHunt(animal)                      │ │
│  │  - DesignateMine(cell)                        │ │
│  │  - CreateZone(type, cells)                    │ │
│  │  - SetDraft(pawn, bool)                       │ │
│  │  - QueueBill(workbench, recipe, count)        │ │
│  └───────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────┘
                        │
                   HTTP/HTTPS
                        │
                ┌───────▼───────┐
                │   LLM API     │
                │  (可配置)     │
                └───────────────┘
```

## 4. 核心模块

### 4.1 ColonyStateExtractor — 殖民地状态序列化

**目标**: 将当前殖民地状态压缩为 ≤ 8K token 的结构化 JSON。

**分层策略**:

```
Level 0 — Summary（~500 token）
  殖民者数量、总财富、天数、季节、威胁等级、食物/电力状态

Level 1 — Key Details（~2K token）
  每个殖民者：名字、主要技能、当前状态、心情概要
  关键资源：钢铁/组件/食物/药品/银/木材 存量
  防御：炮台数量、防御工事位置、最近战斗时间

Level 2 — Full Context（~5K token）
  详细殖民者属性与 hediff
  完整建筑列表与布局概要
  研究树进度
  事件历史
```

AI 请求时根据任务复杂度选择合适级别。日常分析用 L0+L1，详细规划用 L0+L1+L2。

**实现**: 每个 Serializer 类负责一个领域的提取和压缩。使用 `StringBuilder` 生成，避免运行时 JSON 库依赖。

### 4.2 LLM 通信层

**要求**:
- Provider 无关（参考 RimAI Framework 的 JSON 模板方式）
- 异步非阻塞（Unity `Task` / coroutine）
- 支持 function calling / tool use 模式
- 超时 30s，3 次重试

**Action Schema（给 LLM 的 function 定义）**:

```json
{
  "actions": [
    {
      "type": "set_work_priority",
      "params": { "pawn_name": "string", "work_type": "string", "priority": "int 1-4" }
    },
    {
      "type": "place_blueprint",
      "params": { "building_def": "string", "x": "int", "z": "int", "rotation": "0|1|2|3", "stuff": "string?" }
    },
    {
      "type": "designate_area",
      "params": { "action": "hunt|mine|harvest|cut", "target_area": {"x1","z1","x2","z2"} }
    },
    {
      "type": "queue_bill",
      "params": { "workbench_def": "string", "recipe_def": "string", "count": "int" }
    },
    {
      "type": "create_zone",
      "params": { "zone_type": "stockpile|growing|...", "cells": [{"x","z"}], "config": {} }
    },
    {
      "type": "send_report",
      "params": { "title": "string", "content": "string", "severity": "info|warning|critical" }
    }
  ]
}
```

### 4.3 CommandExecutor — 指令执行器

将 LLM 返回的 action JSON 翻译为 RimWorld API 调用。每个 action 类型对应一个 handler：

```csharp
public interface IActionHandler
{
    string ActionType { get; }
    ActionResult Execute(Map map, JObject parameters);
}

public class SetWorkPriorityHandler : IActionHandler
{
    public string ActionType => "set_work_priority";
    public ActionResult Execute(Map map, JObject parameters)
    {
        var pawnName = parameters["pawn_name"].ToString();
        var pawn = map.mapPawns.FreeColonists
            .FirstOrDefault(p => p.LabelShort == pawnName);
        if (pawn == null) return ActionResult.Fail($"Pawn '{pawnName}' not found");

        var workDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail(
            parameters["work_type"].ToString());
        if (workDef == null) return ActionResult.Fail("Invalid work type");

        var priority = parameters["priority"].ToObject<int>();
        pawn.workSettings.SetPriority(workDef, priority);
        return ActionResult.Success();
    }
}
```

### 4.4 UI — VibePlayingTab

游戏内底部 Tab（类似 Colony Manager 的方式）：

- **策略输入**: 文本框 + 预设快捷按钮（"优化食物生产"、"加强防御"、"扩大殖民地"等）
- **态势报告**: AI 生成的当前分析，Markdown 渲染
- **待确认操作**: 列表视图，逐条 ✓/✗
- **执行日志**: 时间线形式的历史操作记录
- **设置**: API 配置、自动化开关

### 4.5 GameComponent — 核心生命周期

```csharp
public class VibePlayingComponent : GameComponent
{
    private int lastAnalysisTick;
    private int analysisIntervalTicks = 60000; // ~1 游戏天

    public override void GameComponentTick()
    {
        if (Find.TickManager.TicksGame - lastAnalysisTick > analysisIntervalTicks)
        {
            if (Settings.AutoAnalyze)
            {
                TriggerAnalysis();
                lastAnalysisTick = Find.TickManager.TicksGame;
            }
        }
    }

    private async void TriggerAnalysis()
    {
        var state = ColonyStateExtractor.Extract(Find.CurrentMap, DetailLevel.L1);
        var response = await LLMClient.AnalyzeAsync(state, Settings.CurrentStrategy);
        ProcessResponse(response);
    }
}
```

## 5. System Prompt 设计要点

给 LLM 的 system prompt 需包含：

1. **角色定义**: "你是 RimWorld 殖民地的 AI 管理顾问…"
2. **游戏机制摘要**: 气候季节、食物腐烂、心情崩溃机制、raid 威力缩放、研究树关键节点
3. **Action Schema**: 可执行的操作类型和参数
4. **输出格式约束**: JSON action list + 文字分析
5. **当前策略上下文**: 用户的高层目标

## 6. Mod 文件结构

```
RimWorldVibePlaying/
├── About/
│   ├── About.xml
│   └── Preview.png
├── Assemblies/
│   └── VibePlaying.dll
├── Defs/
│   ├── MainButtonDefs/
│   │   └── VibePlaying_MainButton.xml
│   └── KeyBindingDefs/
│       └── VibePlaying_KeyBindings.xml
├── Languages/
│   ├── English/Keyed/VibePlaying.xml
│   └── ChineseSimplified/Keyed/VibePlaying.xml
├── Source/
│   ├── VibePlaying.sln
│   └── VibePlaying/
│       ├── Core/
│       │   ├── VibePlayingMod.cs          # ModBase 入口
│       │   ├── VibePlayingComponent.cs     # GameComponent
│       │   └── Settings.cs
│       ├── Extraction/
│       │   ├── ColonyStateExtractor.cs
│       │   ├── PawnSerializer.cs
│       │   ├── ResourceSerializer.cs
│       │   └── ...
│       ├── LLM/
│       │   ├── LLMClient.cs
│       │   ├── ActionSchema.cs
│       │   └── ResponseParser.cs
│       ├── Execution/
│       │   ├── CommandExecutor.cs
│       │   ├── IActionHandler.cs
│       │   └── Handlers/
│       │       ├── SetWorkPriorityHandler.cs
│       │       ├── PlaceBlueprintHandler.cs
│       │       └── ...
│       └── UI/
│           ├── VibePlayingTab.cs
│           ├── AnalysisPanel.cs
│           ├── ActionConfirmPanel.cs
│           └── ExecutionLogPanel.cs
└── Textures/
    └── UI/
        └── VibePlaying_Tab.png
```

## 7. 实现阶段

### Phase 1: MVP — 只读分析顾问（2-3 周）

1. Mod 骨架 + Settings 页面（API key 配置）
2. ColonyStateExtractor（L0 + L1 级别）
3. LLM 异步通信（非流式，单次 completion）
4. UI Tab 展示分析报告
5. 测试：小型殖民地（3-5 人）状态提取 + AI 分析

### Phase 2: 确认式执行（2-3 周）

1. Action Schema 定义 + function calling 集成
2. CommandExecutor + 核心 Handlers（工作优先级/区域指定/建造蓝图）
3. UI 操作确认面板
4. 执行日志
5. 测试：AI 建议 → 批准 → 执行 完整流程

### Phase 3: 周期自动化（2 周）

1. GameComponent tick 调度
2. 策略预设 + 自动化开关
3. 自动模式安全边界（最大建筑数/designations 上限）
4. 流式响应支持
5. 压力测试：大型殖民地

## 8. 技术风险与对策

| 风险 | 影响 | 对策 |
|---|---|---|
| LLM 空间推理弱，建筑布局质量差 | 生成的建筑布局不合理 | 提供预设建筑模板（killbox、宿舍、仓库等）+ 约束检查 |
| Token 预算超限 | 大殖民地信息丢失 | 分层摘要 + 增量更新（只传变化部分） |
| API 延迟影响体验 | 用户等待 AI 响应 | 异步 + 进度指示 + 游戏不暂停 |
| 指令解析失败率高 | AI 返回格式不合规 | 严格 JSON Schema + 重试 + 人工回退 |
| 版本兼容性断裂 | RimWorld 更新后 API 变化 | 只用稳定公开 API，避免 internal 方法 |

## 9. 竞品对比

| 现有 mod | 与 Vibe Playing 的区别 |
|---|---|
| Colony Manager Redux | 基于规则的自动化，无 AI 推理，需手动配置阈值 |
| OpenRimWorldAI | LLM 集成但只做被动报告，不执行操作 |
| RimAI Framework | 底层 SDK 框架，非面向玩家的完整体验 |
| Free Will | 仅自动化 work tab，无建筑/资源规划 |
| **Vibe Playing** | **端到端 AI 殖民地管理：自然语言输入 → 态势分析 → 执行方案 → 操作反馈** |

## 10. Open Questions

1. **建筑布局生成策略**: 纯 LLM 文本描述坐标 vs 预设模板 + LLM 选择/参数化？后者更可靠但灵活性低
2. **多地图支持**: 首版只支持单地图？还是需要处理商队/前哨站场景？
3. **Mod 兼容性**: 如何处理第三方 mod 添加的 WorkType / ThingDef？动态发现 or 白名单？
4. **本地模型优化**: Ollama 本地运行时，是否需要针对 RimWorld 做 prompt 模板优化？
5. **Language**: UI 首版支持英文 + 中文？还是只做英文？
