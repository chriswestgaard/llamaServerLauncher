# Llama Server Launcher

A Windows desktop GUI for launching and managing [llama-server](https://github.com/ggml-org/llama.cpp) (llama.cpp).

<img width="1311" height="1050" alt="screenshot" align="center" src="https://github.com/user-attachments/assets/db5024f0-ce97-46b5-93f9-a0a4da9e352f" />

## Features

- **Model selection** — browse a folder of `.gguf` files and pick a model from a drop-down
- **GPU offloading** — choose Auto, CPU only, GPU only, or a custom layer count (`-ngl`)
- **Live hardware monitoring** — real-time graphs for CPU, RAM, GPU, VRAM and context usage
- **Performance logging** — tracks tokens/sec per model and config; shows best result per model
- **Settings persistence** — all controls are saved to `config.json` on close and restored at startup
- **Command preview** — the full llama-server command is shown before launching
- **One-click chat UI** — opens the built-in llama.cpp web UI in your browser

## Requirements

- Windows 10 / 11
- [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [llama.cpp](https://github.com/ggml-org/llama.cpp/releases) — `llama-server.exe` must be on your PATH or set manually in the Advanced tab

## Getting Started

1. Download or build `LlamaServerLauncher.exe`
2. On first launch, select your model folder (the folder containing `.gguf` files)
3. Choose a model from the **Model File** drop-down
4. Adjust settings as needed (GPU layers, context size, etc.)
5. Click **Launch llama-server**
6. Click **Open Chat UI** to open the web interface

## Workflow

1. **Start with defaults** — they work for most cases; launch the server and verify it responds.
2. **Profile** — send a few requests and check the **Perf** tab for tokens/sec, then watch the live graphs for CPU, GPU, and memory pressure.
3. **Tweak** — change one setting at a time (GPU layers, context size, cache type, etc.) and note the effect.
4. **Iterate** — repeat until you find the sweet spot between speed, resource usage, and output quality.

## Settings

### Model tab

| Setting | Description |
|---|---|
| Model File | The `.gguf` model to load |
| GPU Layers | Auto / CPU only / GPU only / Custom layer count (`-ngl`) |
| Context Size | Token context window; "Model default" uses the value baked into the model (`-c`) |
| Batch / UBatch Size | Logical and physical batch sizes (`-b` / `-ub`) |
| Cache Type K/V | KV cache quantisation — `q8_0` halves VRAM with minimal quality loss (`-ctk` / `-ctv`) |
| Reasoning | Auto-detect, force on, or force off chain-of-thought tokens |
| Threads | CPU threads for generation; Auto = detect from core count (`-t`) |
| Parallel Slots | Number of simultaneous request slots (`-np`) |
| Flash Attention | Reduces VRAM for long contexts (`--flash-attn`) |
| Continuous Batching | Improves throughput under concurrent load (`-cb`) |
| mmap / mlock | Memory-mapping and RAM-locking options |

### Server tab

| Setting | Description |
|---|---|
| Host | Interface to listen on (default `127.0.0.1`) |
| Port | TCP port (default `8080`) |
| Tools | Built-in agentic tools (`--tools`) |

### Sampling tab

Temperature, Top-K, Top-P, Min-P, Repeat Penalty, and Seed.

### Advanced tab

API key, embedding mode, reranking mode, Prometheus metrics endpoint, custom extra arguments, and the path to `llama-server.exe`.

## Performance Tab

After running the server and sending requests, the **Perf** tab shows each model with its best recorded tokens/sec. Expand a model to see results per configuration. Double-click a result row to load that configuration back into the controls.

<img width="488" height="237" alt="screenshot2" src="https://github.com/user-attachments/assets/8164c114-74bc-4059-9a63-2b59c8af904e" />


## Building from Source

```
git clone https://github.com/chriswestgaard/llamaServerLauncher.git
cd llamaServerLauncher
dotnet build
```

Requires Visual Studio 2022 or the .NET 8 SDK.

## License

[MIT](LICENSE)
