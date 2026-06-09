# Development Stages Checklist

## Stage 0 — Bootstrap
- [x] .NET solution with 6 projects + tests
- [x] `.gitignore`, `README.md`
- [x] `dotnet build` succeeds

## Stage 1 — Overlay HUD
- [x] Transparent always-on-top window
- [x] Opacity control
- [x] Click-through toggle
- [x] Ctrl+Shift+H hotkey

## Stage 2 — Screen Capture + OCR
- [x] Region capture via `Graphics.CopyFromScreen`
- [x] Windows.Media.Ocr integration
- [x] Change detection
- [x] Region picker UI

## Stage 3 — Local LLM
- [x] Ollama streaming client
- [x] PromptBuilder
- [x] Screen text → overlay pipeline

## Stage 4 — Audio + ASR
- [x] NAudio WasapiLoopbackCapture
- [x] Ollama ASR service
- [x] Transcript → orchestrator

## Stage 5 — Personal Memory RAG
- [x] SQLite memory store
- [x] Ollama embeddings
- [x] Vector cosine search
- [x] Memory editor UI

## Stage 6 — Document Mode
- [x] PDF/DOCX/PPTX/TXT ingest
- [x] Document sessions
- [x] Answer mode toggle

## Stage 7 — Context Orchestrator
- [x] Unified OCR + ASR + RAG pipeline
- [x] Debounce / cancellation
- [x] Status indicators

## Stages 8–9 — Polish & Testing
- [x] Settings UI with persistence
- [x] Serilog file logging
- [x] Pause/resume hotkey
- [x] Unit tests (ChangeDetector, PromptBuilder, VectorSearch)
- [ ] Manual: verify Ollama connectivity
- [ ] Manual: test screen OCR on browser window
- [ ] Manual: upload PDF and ask document question

## Manual Test Plan

1. Start Ollama: `ollama serve`
2. Run app: `dotnet run --project src/Readscreen.App`
3. Open a webpage with a question in the capture region
4. Confirm overlay shows streamed answer within ~15s
5. Add a memory entry via Manage Memory; ask a personal question
6. Upload a PDF; switch to DocumentOnly mode; ask about its content
7. Play audio; confirm transcript triggers response (requires whisper model)
