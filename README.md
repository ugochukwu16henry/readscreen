# Readscreen

Private, local-first AI desktop copilot for Windows (.NET 8 WPF).

Readscreen captures screen text and meeting audio, retrieves context from your personal memory and uploaded documents, and streams answers to a transparent overlay HUD — all running locally via Ollama.

## Prerequisites

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Ollama](https://ollama.com/download)

```powershell
ollama pull phi3
ollama pull nomic-embed-text
# Optional for audio transcription:
ollama pull whisper
```

## Build & Run

```powershell
cd readscreen
dotnet build
dotnet run --project src/Readscreen.App
```

## Features

| Feature | Description |
|---------|-------------|
| **Screen OCR** | Monitors a configurable screen region using Windows OCR |
| **Audio ASR** | Captures system audio loopback and transcribes via Ollama |
| **Local LLM** | Streams answers from Ollama (phi3, qwen2.5-coder, etc.) |
| **Personal Memory** | SQLite + vector RAG for resume, projects, skills |
| **Document Mode** | Upload PDF/DOCX/PPTX for presentation Q&A |
| **Overlay HUD** | Transparent, always-on-top answer panel |

## Hotkeys

| Hotkey | Action |
|--------|--------|
| `Ctrl+Shift+H` | Toggle overlay visibility |
| `Ctrl+Shift+P` | Pause / resume assistant |

## Project Structure

```
src/
  Readscreen.App/        WPF host, settings UI, DI
  Readscreen.Core/       Models, interfaces, orchestrator
  Readscreen.Perception/ Screen capture, OCR, audio, ASR
  Readscreen.Memory/     RAG, embeddings, document ingest
  Readscreen.Llm/        Ollama client
  Readscreen.Overlay/    Transparent HUD window
```

## Settings

Settings persist to `%AppData%/Readscreen/settings.json`. Data (memory DB, document DB, logs) is stored locally under `%AppData%/Readscreen/`.

## Answer Modes

- **Hybrid** — documents first, then personal memory, then general knowledge
- **DocumentOnly** — answers strictly from uploaded materials
- **PersonalMemory** — answers from your stored background
- **General** — LLM general knowledge only

## License

Personal use only.
