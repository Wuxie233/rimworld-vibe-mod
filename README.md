# RimWorld Vibe Playing — AI Colony Advisor

AI-assisted colony management mod for RimWorld 1.5+.

You decide **what** to do, AI figures out **how**.

## Features

### Core
- **Colony State Extraction**: Serializes colonists, resources, buildings, threats, research, power grid into structured JSON (L0/L1/L2 detail levels)
- **LLM Integration**: Async HTTP with SSE streaming + retry with exponential backoff
- **Conversation Context**: Last 2 analysis results are sent as conversation history for continuity
- **Def Discovery**: Runtime scan of available WorkTypes, buildable items, recipes, growable plants — ensures AI uses correct defNames even with modded content
- **Bilingual**: English + Simplified Chinese

### Actions (8 types)
| Action | Description |
|--------|-------------|
| `set_work_priority` | Change a colonist's work priority (0-4) |
| `place_blueprint` | Place a single building blueprint |
| `place_template` | Place a predefined room template (bedroom, barracks, kitchen, hospital, killbox, storage, research) |
| `designate` | Designate hunt/mine/cut/harvest |
| `queue_bill` | Queue a crafting/cooking bill |
| `create_zone` | Create stockpile or growing zones |
| `set_draft` | Draft/undraft colonists for combat |
| `send_report` | Send in-game notifications |

### Automation
- **Auto-analysis**: Optional periodic analysis every N game-days
- **Per-action auto-execution**: Toggle which action types execute without confirmation
- **Safety limits**: Max blueprints, designations, bills, work changes per cycle
- **Safety counter**: Automatic reset at each cycle boundary

### UI
- In-game window with 3 tabs (Analysis / Actions / Log)
- Strategy input with 5 quick presets
- Live streaming text display during AI response
- Per-action approve/reject + batch approve all
- Status badges and color-coded execution log

## Build

```
cd Source
dotnet build VibePlaying/VibePlaying.csproj -c Release
```

The compiled `VibePlaying.dll` goes to `Assemblies/`. No local RimWorld install needed — uses `Krafs.Rimworld.Ref` NuGet package for compile-time references.

Symlink or copy the mod folder to your RimWorld mods directory:
```
mklink /D "C:\...\RimWorld\Mods\VibePlaying" "D:\CODE\rimworld-vibe-mod"
```

## Configuration

In-game: Options → Mod Settings → VibePlaying

- **API Endpoint**: Default `http://localhost:11434/v1/chat/completions` (Ollama)
- **API Key**: Leave empty for local Ollama, set for OpenAI/Groq
- **Model**: Default `qwen2.5:14b`

## Development Phases

- [x] **Phase 1**: Mod skeleton + colony state extraction + LLM + read-only analysis UI
- [x] **Phase 2**: Action models + 5 handlers + response parser + confirm-before-execute UI
- [x] **Phase 3**: Periodic automation + per-action auto-execution + safety limits
- [x] **Phase 4**: SSE streaming + retry + CreateZone/SetDraft handlers + power grid extraction
- [x] **Phase 5**: Building templates + conversation history + def discovery

## License

MIT
