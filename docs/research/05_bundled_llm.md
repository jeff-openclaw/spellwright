# 05 — Bundled LLM: Eliminating the Ollama Dependency

## Problem

The original architecture required players to install and run Ollama separately. This is a dealbreaker for casual players — they won't install a CLI tool and download models manually.

**Goal:** Bundle the LLM with the game. Zero external dependencies. Download → Play.

---

## Option Comparison

| Criteria | **LLamaSharp** | ONNX Runtime | Unity Sentis | Bundled Ollama |
|---|---|---|---|---|
| **Model format** | GGUF (llama.cpp) | ONNX | ONNX/Sentis | GGUF |
| **Unity support** | ✅ Proven ([Unity demo](https://github.com/eublefar/LLAMASharpUnityDemo)) | ⚠️ Possible but no Unity-specific tooling | ✅ Native Unity package | ⚠️ Subprocess management |
| **GPU acceleration** | Metal, CUDA 11/12, Vulkan, CPU | CUDA, DirectML, CPU | GPU compute shaders | Metal, CUDA, CPU |
| **LLM chat API** | ✅ Built-in (ChatSession, streaming) | ❌ Raw tensor I/O only | ❌ Raw tensor I/O only | ✅ REST API |
| **C# API quality** | ✅ High-level, well-documented | ⚠️ Low-level tensor ops | ⚠️ Low-level, no LLM abstractions | N/A (HTTP) |
| **Model ecosystem** | ✅ All GGUF models on HuggingFace | ⚠️ Must convert to ONNX | ⚠️ Must convert to Sentis format | ✅ Same as LLamaSharp |
| **Quantization** | Q4_K_M, Q5, Q8, etc. | INT4/INT8 | Limited | Same as LLamaSharp |
| **Install complexity** | NuGet + native DLL | NuGet | Unity Package Manager | Ship binary + manage process |
| **3B model feasibility** | ✅ Excellent | ⚠️ Requires custom pipeline | ❌ Not designed for LLMs this size | ✅ Works |
| **Maturity for LLMs** | ✅ Production-ready | ⚠️ Generic ML, not LLM-focused | ❌ Targeting CV/small models | ✅ Production-ready |

## Recommendation: **LLamaSharp**

LLamaSharp is the clear winner:

1. **Proven Unity integration** — community demo exists with StreamingAssets model loading
2. **High-level chat API** — `ChatSession` with streaming, no need to manage tokens/tensors manually
3. **Cross-platform GPU** — Metal on Mac (automatic with CPU backend!), CUDA on Windows, Vulkan as fallback
4. **GGUF ecosystem** — thousands of pre-quantized models on HuggingFace
5. **Active development** — tracks llama.cpp closely, regular releases

ONNX Runtime and Sentis lack LLM-specific abstractions (tokenizer, chat templates, KV cache management). You'd be reimplementing half of llama.cpp in C#. Bundled Ollama works but adds process management complexity and ~200MB binary overhead.

---

## Model Distribution Strategy

### Recommended Model: Llama 3.2 3B Q4_K_M

- **File size:** ~1.8 GB GGUF
- **RAM usage:** ~2.5-3 GB at runtime
- **Quality:** Excellent for constrained game tasks (clue generation, NPC dialogue)

### Distribution Options

| Strategy | Pros | Cons |
|---|---|---|
| **StreamingAssets (ship with game)** | Zero friction, works offline | +1.8 GB download size |
| **First-run download** | Smaller initial download | Requires internet, download UI |
| **Hybrid (ship small, download large)** | Flexibility | More complexity |

**Recommendation:** Ship in StreamingAssets for v1. A 1.8 GB model inside a ~2.5 GB total game download is acceptable for a desktop game. Add optional "download better model" later.

### File Location

```
Assets/
└── StreamingAssets/
    └── Models/
        └── llama-3.2-3b-q4_k_m.gguf    # 1.8 GB
```

At runtime: `Path.Combine(Application.streamingAssetsPath, "Models", "llama-3.2-3b-q4_k_m.gguf")`

---

## Cross-Platform Considerations

### macOS (Apple Silicon) — Primary Dev Platform
- **Backend:** `LLamaSharp.Backend.Cpu` includes Metal support automatically
- **Performance:** M1 Pro runs 3B Q4 at ~40-60 tokens/sec via Metal
- **No extra setup needed**

### Windows (CUDA / CPU)
- **With NVIDIA GPU:** Ship `LLamaSharp.Backend.Cuda12` native DLLs → 50-80 tok/s on RTX 3060+
- **CPU fallback:** Ship CPU backend DLLs alongside → 10-20 tok/s (still playable for short clues)
- **Strategy:** Include both CUDA and CPU DLLs; LLamaSharp auto-selects best available

### Linux
- **CUDA or Vulkan** for GPU acceleration
- **CPU fallback** works everywhere
- Ship all three backend DLLs; auto-detection handles it

### Unity Build: Native Library Bundling
Per the [Unity demo](https://github.com/eublefar/LLAMASharpUnityDemo):
1. Download backend NuGet packages manually
2. Extract native libraries from `runtimes/<platform>/` folders
3. Place in `Assets/Plugins/<platform>/`
4. Unity includes them in builds automatically
5. **Important:** Library must be named `libllama.dll` (not `llama.dll`) on Windows

```
Assets/Plugins/
├── macOS/          libllama.dylib (Metal+CPU, universal binary)
├── Windows-x64/    libllama.dll (CPU), cublas libs (CUDA)
└── Linux-x64/      libllama.so (CPU), CUDA libs
```

---

## Memory & Performance Expectations

### 3B Q4_K_M Model on Consumer Hardware

| Platform | Tokens/sec | Load Time | RAM Usage | Notes |
|---|---|---|---|---|
| M1 Pro 16GB | 40-60 | ~2s | ~3 GB | Metal GPU, excellent |
| RTX 3060 12GB | 50-80 | ~2s | ~3 GB VRAM | Full GPU offload |
| RTX 2060 6GB | 40-60 | ~2s | ~3 GB VRAM | Full GPU offload |
| Intel i7 (CPU) | 10-20 | ~3s | ~3 GB RAM | Playable for short outputs |
| 8GB RAM laptop (CPU) | 8-15 | ~4s | ~3 GB RAM | Tight but works |

### For Spellwright's Use Case
- Clue generation: ~50-100 tokens output → **<2s on GPU, <5s on CPU**
- NPC dialogue: ~100-200 tokens → **<3s on GPU, <10s on CPU**
- Context window: 2048 tokens is plenty (system prompt + short exchange)
- **Verdict:** 3B model is ideal — fast enough for real-time gameplay on most hardware

### Memory Management
- Load model once at game start, keep in memory throughout session
- `ContextSize = 2048` keeps KV cache small (~200 MB)
- Dispose model on scene unload or game exit
- Total runtime memory: ~3-3.5 GB (model + KV cache)

---

## Code Example: LLamaSharp Chat in Unity

```csharp
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using Cysharp.Threading.Tasks; // UniTask for Unity-friendly async

public class LLMExample : MonoBehaviour
{
    private LLamaWeights _model;
    private LLamaContext _context;

    async void Start()
    {
        // Load model from StreamingAssets
        var modelPath = Path.Combine(
            Application.streamingAssetsPath, "Models", "llama-3.2-3b-q4_k_m.gguf");

        var parameters = new ModelParams(modelPath)
        {
            ContextSize = 2048,
            GpuLayerCount = 99,  // Offload all layers to GPU if available
        };

        // Load on background thread to avoid blocking Unity main thread
        _model = await UniTask.RunOnThreadPool(() => LLamaWeights.LoadFromFile(parameters));
        _context = _model.CreateContext(parameters);
    }

    public async UniTask<string> GenerateClue(string systemPrompt, string userMessage)
    {
        var executor = new InteractiveExecutor(_context);
        var session = new ChatSession(executor);

        var history = new ChatHistory();
        history.AddMessage(AuthorRole.System, systemPrompt);
        history.AddMessage(AuthorRole.User, userMessage);

        var sb = new StringBuilder();
        var inferenceParams = new InferenceParams
        {
            MaxTokens = 200,
            Temperature = 0.8f,
            AntiPrompts = new[] { "User:" }
        };

        await foreach (var token in session.ChatAsync(
            history, inferenceParams, CancellationToken.None))
        {
            sb.Append(token);
        }

        return sb.ToString();
    }

    void OnDestroy()
    {
        _context?.Dispose();
        _model?.Dispose();
    }
}
```

### Key Unity Gotchas

1. **Thread safety:** LLamaSharp inference is blocking — always run on a background thread via `UniTask.RunOnThreadPool()` or `Task.Run()`
2. **Model loading is slow (~2-4s):** Do it during a loading screen, not during gameplay
3. **NuGetForUnity:** Use this to install LLamaSharp package; manually copy native backend DLLs
4. **IL2CPP:** LLamaSharp uses P/Invoke to native code — works with IL2CPP, but test your build
5. **`libllama.dll` naming:** On Windows, the native library MUST be named `libllama.dll`
6. **Single inference at a time:** Use a semaphore — concurrent calls to the same context will corrupt state

---

## Migration from OllamaService

| Old (OllamaService) | New (LLMService) |
|---|---|
| `HttpClient` → Ollama REST API | `LLamaWeights` + `LLamaContext` in-process |
| `BaseUrl = "http://localhost:11434"` | `ModelPath = StreamingAssets/Models/*.gguf` |
| NDJSON streaming parse | `IAsyncEnumerable<string>` from ChatSession |
| `CheckModelsAsync()` via GET /api/tags | Model file exists on disk check |
| `SetKeepAliveAsync()` | Not needed (in-process memory management) |
| Requires Ollama installed + running | Zero dependencies |

See `LLMService.cs` in `docs/prepared/scripts/` for the full implementation.
