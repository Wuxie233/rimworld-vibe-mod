# Changelog

## v1.0.0 (2026-03-21)

### Features
- **8 action types**: set_work_priority, place_blueprint, place_template, designate, queue_bill, create_zone, set_draft, send_report
- **7 building templates**: bedroom, barracks, kitchen, hospital, killbox, storage, research
- **Colony state extraction**: L0/L1/L2 detail levels — colonists, resources, buildings, threats, research, power grid
- **Def discovery**: Runtime scan of available WorkTypes, buildable items, recipes, growable plants, stuff materials
- **SSE streaming**: Real-time text display during AI response
- **Retry with backoff**: 3 retries with 1s/3s/8s delay (skips 4xx except 429)
- **Conversation context**: Last 2 analysis results sent as multi-turn history
- **Event-driven analysis**: Harmony patches detect raids and mental breaks, auto-trigger AI analysis
- **Auto-execution**: Per-action-type toggles with safety limits per cycle
- **Session persistence**: Analysis history + execution log survive save/load
- **Keyboard shortcuts**: F8 toggle window, F9 quick analyze
- **Bilingual**: English + Simplified Chinese
