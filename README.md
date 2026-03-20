# RimWorld Vibe Playing — AI Colony Advisor

AI-assisted colony management mod for RimWorld 1.5+.

You decide **what** to do, AI figures out **how**.

## Features (Phase 1 — MVP)

- **Colony State Extraction**: Serializes colonists, resources, buildings, threats, research into structured JSON
- **LLM Integration**: Async HTTP calls to any OpenAI-compatible API (OpenAI, Ollama, Groq, etc.)
- **Analysis UI**: In-game window with strategy input, quick presets, and analysis history
- **Auto-analysis**: Optional periodic analysis every N game-days
- **Bilingual**: English + Simplified Chinese

## Build

### Prerequisites

- .NET Framework 4.7.2 SDK or Visual Studio 2022+
- RimWorld installed (for DLL references)

### Steps

1. Set `RIMWORLD_DIR` environment variable to your RimWorld install path, or edit the path in `Source/VibePlaying/VibePlaying.csproj`

2. Build:
   ```
   cd Source
   dotnet build VibePlaying/VibePlaying.csproj -c Release
   ```

3. The compiled `VibePlaying.dll` will be placed in `Assemblies/`

4. Symlink or copy the mod folder to your RimWorld mods directory:
   ```
   mklink /D "C:\...\RimWorld\Mods\VibePlaying" "D:\CODE\rimworld-vibe-mod"
   ```

## Configuration

In-game: Options → Mod Settings → VibePlaying

- **API Endpoint**: Default `http://localhost:11434/v1/chat/completions` (Ollama)
- **API Key**: Leave empty for local Ollama, set for OpenAI/Groq
- **Model**: Default `qwen2.5:14b`

## Usage

1. Start a RimWorld game with the mod enabled
2. Click the "Vibe AI" button in the bottom bar
3. Type a strategy or pick from presets
4. Click "Analyze Colony"
5. Read AI recommendations

## Roadmap

- **Phase 2**: AI-suggested actions with confirm-before-execute (build, zone, work priority)
- **Phase 3**: Periodic automation with safety limits

## License

MIT
