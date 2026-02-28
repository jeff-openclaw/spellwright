# Spellwright — Technical Research Report

**Date:** 2026-02-28  
**Purpose:** Drive architectural decisions for Spellwright, a word-guessing roguelike with LLM-powered NPCs.

---

## 1. C# Hunspell Integration

### Library Comparison

| Library | NuGet | .NET Compat | Native Deps | License | Status |
|---------|-------|-------------|-------------|---------|--------|
| **WeCantSpell.Hunspell** | v7.0.1 | .NET Standard 2.0+ / .NET 6-10 / Framework 4.6.1+ | **None** (pure managed C#) | MPL/LGPL/GPL tri-license | Active, maintained |
| **NHunspell** | v1.2.5554 | .NET Framework only (P/Invoke to native Hunspell) | Yes — native DLLs (Windows only) | LGPL/GPL/MPL | Abandoned (~2015) |
| **Symspell** | v6.7.2 | .NET Standard 2.0+ | None | MIT | Active, but edit-distance only — no affix/morphology |

### Recommendation: **WeCantSpell.Hunspell**

**Justification:**
- **Pure managed code** — no native DLLs, works on all Unity platforms (Windows, Linux, macOS, WebGL potentially)
- **Thread-safe** — can be queried concurrently, important for game loops
- **Competitive performance** — Check 7000 words in ~5.4ms on .NET 10; Suggest 300 words in ~1.4s (faster than NHunspell's 8.2s)
- **Full Hunspell support** — affix rules, compound words, suggestions — critical for a word game
- **Active maintenance** — latest release is v7.0.1 with .NET 10 support
- NHunspell is dead and Windows-only via native bindings

**API Surface:**
```csharp
using WeCantSpell.Hunspell;

// Load from files (or streams for Unity Resources/StreamingAssets)
var dictionary = WordList.CreateFromFiles("en_US.dic");
// Or from streams:
var dict = WordList.CreateFromStreams(dicStream, affStream);

bool valid = dictionary.Check("wizard");       // true
bool invalid = dictionary.Check("wizerd");     // false
var suggestions = dictionary.Suggest("wizerd"); // ["wizard", "wired", ...]
```

**Encoding Note for Non-UTF8 dictionaries (Romanian, French):**
```csharp
// Required for ISO-8859 encoded dictionaries
System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
```

**Unity Integration:**
- Install via NuGetForUnity (`https://github.com/GlitchEnzo/NuGetForUnity`) or copy DLL directly
- Package: `WeCantSpell.Hunspell` on NuGet
- Place dictionary files in `StreamingAssets/Dictionaries/` and load via streams

### Dictionary Files

**Source:** LibreOffice Dictionaries Repository  
- **URL:** `https://github.com/LibreOffice/dictionaries`
- Structure: each language has its own folder with `.dic` and `.aff` files

| Language | Path in Repo | Files |
|----------|-------------|-------|
| English (US) | `en/en_US.dic`, `en/en_US.aff` | ~62K words |
| English (GB) | `en/en_GB.dic`, `en/en_GB.aff` | ~68K words |
| Romanian | `ro/ro_RO.dic`, `ro/ro_RO.aff` | ~150K words |
| French | `fr_FR/fr_FR.dic`, `fr_FR/fr_FR.aff` | ~90K words |

**Alternative sources:**
- SCOWL (Spell Checker Oriented Word Lists): `http://wordlist.aspell.net/` — for custom English word lists by difficulty level
- Hunspell GitHub: `https://github.com/hunspell/hunspell` (test dictionaries)

---

## 2. Ollama Integration from Unity

### Ollama REST API

**Base URL:** `http://localhost:11434`

#### Key Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/generate` | POST | Single-prompt completion (streaming by default) |
| `/api/chat` | POST | Multi-turn chat completion (streaming by default) |
| `/api/tags` | GET | List locally available models |
| `/api/show` | POST | Show model info |
| `/api/pull` | POST | Download a model |

#### Chat Completion — Request Format
```json
{
  "model": "llama3.2:3b",
  "messages": [
    {"role": "system", "content": "You are a mysterious shopkeeper in a word-magic world."},
    {"role": "user", "content": "What do you sell?"}
  ],
  "stream": true,
  "options": {
    "temperature": 0.8,
    "num_predict": 150,
    "num_ctx": 2048
  }
}
```

#### Streaming Response (NDJSON)
Each line is a JSON object:
```json
{"model":"llama3.2:3b","created_at":"...","message":{"role":"assistant","content":"I"},"done":false}
{"model":"llama3.2:3b","created_at":"...","message":{"role":"assistant","content":" sell"},"done":false}
...
{"model":"llama3.2:3b","created_at":"...","message":{"role":"assistant","content":""},"done":true,"total_duration":...,"eval_count":42}
```

#### JSON Mode / Structured Output
```json
{
  "model": "llama3.2:3b",
  "messages": [{"role": "user", "content": "Give me an NPC greeting as JSON with fields: greeting, mood, hint"}],
  "format": {
    "type": "object",
    "properties": {
      "greeting": {"type": "string"},
      "mood": {"type": "string"},
      "hint": {"type": "string"}
    },
    "required": ["greeting", "mood", "hint"]
  },
  "stream": false
}
```

### UnityWebRequest vs HttpClient

| Aspect | UnityWebRequest | HttpClient (.NET) |
|--------|----------------|-------------------|
| Main thread safe | ✅ Yes (coroutine-based) | ❌ Needs marshaling back to main thread |
| Streaming support | ⚠️ Via custom DownloadHandler | ✅ Native via ReadAsStreamAsync |
| Coroutine friendly | ✅ Native | ❌ Async/await (needs UniTask or similar) |
| IL2CPP / WebGL | ✅ Full support | ⚠️ Limited on some platforms |
| Ease of streaming | Moderate — need custom DownloadHandlerScript | Easy — standard stream reading |

**Recommendation: UnityWebRequest with custom DownloadHandler** for maximum Unity compatibility, OR **HttpClient + UniTask** if targeting desktop only (which Spellwright is).

Since Spellwright targets desktop with local Ollama, **HttpClient + async/await** is the cleaner approach. Use UniTask for Unity main-thread marshaling.

### Streaming Implementation (Typewriter NPC Dialogue)

```csharp
using System.IO;
using System.Net.Http;
using System.Text;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class OllamaClient : MonoBehaviour
{
    private static readonly HttpClient _http = new();
    private const string BASE_URL = "http://localhost:11434";

    public async void StreamChat(string systemPrompt, string userMessage, System.Action<string> onToken, System.Action onDone)
    {
        var payload = new
        {
            model = "llama3.2:3b",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            stream = true,
            options = new { temperature = 0.8, num_predict = 200 }
        };

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}/api/chat") { Content = content };
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line)) continue;

            var obj = JObject.Parse(line);
            var token = obj["message"]?["content"]?.ToString();
            if (!string.IsNullOrEmpty(token))
            {
                // Must dispatch to main thread if updating UI
                onToken(token);
            }
            if (obj["done"]?.Value<bool>() == true)
            {
                onDone();
                break;
            }
        }
    }
}
```

**Main-thread dispatch:** Use `SynchronizationContext.Current` captured at Start(), or UniTask's `await UniTask.SwitchToMainThread()`.

### Existing Unity + Ollama Projects

- **srcnalt/OpenAI-Unity** (`https://github.com/srcnalt/OpenAI-Unity`) — OpenAI API client for Unity, easily adaptable to Ollama since Ollama supports OpenAI-compatible endpoints at `/v1/chat/completions`
- **Ollama OpenAI compatibility:** Ollama exposes OpenAI-compatible API at `http://localhost:11434/v1/` — meaning any OpenAI Unity client works with minimal URL change
- **NPC dialogue pattern:** Use non-streaming for structured JSON (game mechanics data) and streaming for typewriter dialogue display

---

## 3. Model Selection for 8GB VRAM (AMD RX 6600 XT)

### VRAM Requirements at Q4_K_M Quantization

| Model | Parameters | Q4_K_M Size | Est. VRAM (loaded) | Fits 8GB? | Context Window |
|-------|-----------|-------------|--------------------|-----------| --------------|
| **Llama 3.2 3B** | 3B | ~2.0 GB | ~2.5 GB | ✅ Easily | 128K |
| **Phi-3.5 Mini** | 3.8B | ~2.3 GB | ~2.9 GB | ✅ Easily | 128K |
| **Mistral 7B v0.3** | 7.2B | ~4.1 GB | ~5.0 GB | ✅ Yes | 32K |
| **Qwen 2.5 7B** | 7.6B | ~4.4 GB | ~5.3 GB | ✅ Yes | 128K |
| **Llama 3.1 8B** | 8B | ~4.7 GB | ~5.7 GB | ⚠️ Tight | 128K |

*VRAM estimates include KV cache overhead for ~2K context. Larger contexts consume more.*

### AMD RX 6600 XT + ROCm Compatibility

The RX 6600 XT uses the **Navi 23 (gfx1032)** GPU die.

**Current status (as of early 2026):**
- **ROCm 6.x** officially supports gfx1030 (RX 6800/6900 series) but **not gfx1032 natively**
- **Community workaround:** Set `HSA_OVERRIDE_GFX_VERSION=10.3.0` to trick ROCm into treating the 6600 XT as a 6800
- **Ollama support:** Ollama bundles ROCm support and many users report the 6600 XT working with the override env var
- **Performance:** ~70-80% of theoretical throughput compared to officially supported GPUs; some users report occasional stability issues with large models

**Setup:**
```bash
# Set before running Ollama
export HSA_OVERRIDE_GFX_VERSION=10.3.0
ollama serve
```

Or in systemd service file:
```ini
[Service]
Environment="HSA_OVERRIDE_GFX_VERSION=10.3.0"
```

**Fallback:** If ROCm is unstable, Ollama falls back to **CPU inference** via llama.cpp. The 3B models run acceptably on CPU (~10-15 tok/s on modern CPUs).

### Multilingual Quality

| Model | English | Romanian | French | Notes |
|-------|---------|----------|--------|-------|
| **Llama 3.2 3B** | ★★★★ | ★★☆ | ★★★ | Training data skews English; Romanian is weak |
| **Phi-3.5 Mini** | ★★★★ | ★★☆ | ★★★ | Microsoft model, English-focused |
| **Qwen 2.5 7B** | ★★★★ | ★★★ | ★★★★ | Best multilingual of the bunch — trained on diverse data |
| **Mistral 7B** | ★★★★ | ★★★ | ★★★★★ | French company, excellent French; decent Romanian |
| **Llama 3.1 8B** | ★★★★★ | ★★★ | ★★★★ | Strong all-around but tight on VRAM |

### Recommendation

**Primary Model: Qwen 2.5 7B (Q4_K_M)**
- Best balance of multilingual quality, VRAM fit (~5.3 GB), and instruction following
- 128K context window (useful for long NPC conversation history)
- Strong structured output / JSON mode support
- Ollama tag: `qwen2.5:7b`

**Fallback Model: Llama 3.2 3B (Q4_K_M)**
- Only ~2.5 GB VRAM — guaranteed to fit, leaves room for the game
- Good enough for short NPC barks and hints
- Fast inference (~30+ tok/s on GPU)
- Ollama tag: `llama3.2:3b`

**Strategy:** Load Qwen 2.5 7B by default. If VRAM pressure is detected (game stutters), fall back to Llama 3.2 3B via Ollama's model switching. Use `keep_alive` parameter to manage model loading/unloading.

---

## 4. Unity MCP + Claude Code Setup

### Overview

**Repository:** `https://github.com/CoplayDev/unity-mcp`  
**License:** MIT  
**What it does:** Bridges Claude Code (or other MCP clients) with the Unity Editor, allowing AI to manage assets, control scenes, edit scripts, and automate tasks via Model Context Protocol.

### Prerequisites

- **Unity 2021.3 LTS** or newer (2022.3 LTS recommended for best compatibility)
- **Python 3.10+** with `uv` package manager
  - Install uv: `curl -LsSf https://astral.sh/uv/install.sh | sh`
- **Claude Code** CLI installed

### Installation Steps

1. **Install Unity Package:**
   - Unity → Window → Package Manager → "+" → Add package from git URL:
   ```
   https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main
   ```
   
2. **Start the MCP Server:**
   - Unity → Window → MCP for Unity
   - Click "Start Server" (launches HTTP server on `localhost:8080`)

3. **Configure Claude Code:**
   Add to your Claude Code MCP config (`.claude/mcp.json` or equivalent):
   ```json
   {
     "mcpServers": {
       "unityMCP": {
         "url": "http://localhost:8080/mcp"
       }
     }
   }
   ```

   Or for stdio transport:
   ```json
   {
     "mcpServers": {
       "unityMCP": {
         "command": "uvx",
         "args": ["--from", "mcpforunityserver", "mcp-for-unity", "--transport", "stdio"]
       }
     }
   }
   ```

4. **Verify:** Look for 🟢 "Connected ✓" in the Unity MCP window

### Available Tools (Subset Relevant to Spellwright)

- `manage_scene` — create/modify scenes
- `manage_gameobject` — CRUD for GameObjects
- `manage_components` — add/configure components
- `create_script` / `manage_script` — write and edit C# scripts
- `manage_material` / `manage_shader` — material/shader management
- `manage_ui` — UI creation
- `batch_execute` — batch multiple operations (10-100x faster)
- `read_console` — read Unity console output
- `run_tests` — execute unit tests

### Tips & Known Issues

- **Performance:** Always prefer `batch_execute` for multiple operations
- **Roslyn validation:** For strict code validation, install NuGetForUnity → Microsoft.CodeAnalysis v5.0, add `USE_ROSLYN` to Scripting Define Symbols
- **Multiple instances:** If running multiple Unity editors, use `set_active_instance` to target the right one
- **Troubleshooting guides:**
  - `https://github.com/CoplayDev/unity-mcp/wiki/2.-Fix-Unity-MCP-and-Claude-Code`
  - `https://github.com/CoplayDev/unity-mcp/wiki/3.-Common-Setup-Problems`

### Recommended Unity Version

**Unity 2022.3 LTS** — best balance of stability, .NET Standard 2.1 support (needed for modern NuGet packages like WeCantSpell.Hunspell), and long-term support.

---

## 5. CRT Aesthetic Resources

### CRT Shader Packages

| Package | Pipeline | URL | License | Notes |
|---------|----------|-----|---------|-------|
| **Kino** (keijiro) | HDRP | `https://github.com/keijiro/Kino` | Unlicense | Glitch, scanlines, color drift effects. HDRP only. |
| **RetroLook Pro** | URP/Built-in | Unity Asset Store (paid ~$20) | Commercial | Most complete CRT package |
| **unity-CRT-shader** | Built-in | Search GitHub "unity CRT shader" | Various | Multiple small open-source options |
| **Stylized Post Processing** | URP | Asset Store (free tier available) | Commercial | Includes CRT-like effects |

**DIY approach (recommended for full control):**
A CRT shader is straightforward in URP with a custom Renderer Feature:
```hlsl
// Core CRT effects to implement:
// 1. Scanlines — horizontal darkening every N pixels
// 2. Phosphor grid — RGB sub-pixel simulation via mask texture
// 3. Curvature — barrel distortion via UV warp
// 4. Vignette — darken edges
// 5. Chromatic aberration — slight RGB channel offset
// 6. Bloom/glow — soft light bleeding (use Unity's built-in)
```

A single full-screen shader pass with these effects is ~50-80 lines of HLSL and gives complete artistic control.

### Screen Shake

```csharp
// Cinemachine approach (recommended):
// Add CinemachineImpulseSource + CinemachineImpulseListener
// Trigger: impulseSource.GenerateImpulse(force);

// Manual approach:
public IEnumerator Shake(float duration = 0.2f, float magnitude = 0.1f)
{
    Vector3 original = transform.localPosition;
    float elapsed = 0f;
    while (elapsed < duration)
    {
        float x = Random.Range(-1f, 1f) * magnitude;
        float y = Random.Range(-1f, 1f) * magnitude;
        transform.localPosition = original + new Vector3(x, y, 0);
        elapsed += Time.deltaTime;
        yield return null;
    }
    transform.localPosition = original;
}
```

### Typography Animation (Typewriter Effect)

```csharp
// TextMeshPro typewriter with per-character animation
using TMPro;

public class TypewriterEffect : MonoBehaviour
{
    public TMP_Text textComponent;
    public float charDelay = 0.03f;

    public IEnumerator Type(string fullText)
    {
        textComponent.text = fullText;
        textComponent.maxVisibleCharacters = 0;

        for (int i = 0; i <= fullText.Length; i++)
        {
            textComponent.maxVisibleCharacters = i;
            // Optional: play keystroke sound, apply per-char wobble
            yield return new WaitForSeconds(charDelay);
        }
    }
}
```

For per-character animation (wobble, wave, glitch), manipulate `TMP_TextInfo.meshInfo` vertex positions in `LateUpdate()`. TextMeshPro exposes per-character vertex data for this purpose.

### Free Fonts with Latin Extended Support

All fonts below support Latin Extended (Romanian ă, â, î, ș, ț and French é, è, ê, ë, ç, etc.):

| Font | Style | URL | License |
|------|-------|-----|---------|
| **IBM Plex Mono** | Monospace, clean | `https://fonts.google.com/specimen/IBM+Plex+Mono` | OFL 1.1 |
| **JetBrains Mono** | Monospace, coding | `https://fonts.google.com/specimen/JetBrains+Mono` | OFL 1.1 |
| **Space Mono** | Monospace, retro | `https://fonts.google.com/specimen/Space+Mono` | OFL 1.1 |
| **VT323** | Pixel/CRT aesthetic | `https://fonts.google.com/specimen/VT323` | OFL 1.1 |
| **Share Tech Mono** | Tech/terminal | `https://fonts.google.com/specimen/Share+Tech+Mono` | OFL 1.1 |
| **Fira Code** | Monospace, ligatures | `https://github.com/tonsky/FiraCode` | OFL 1.1 |
| **Press Start 2P** | Pixel art | `https://fonts.google.com/specimen/Press+Start+2P` | OFL 1.1 |

**Recommendation:** **IBM Plex Mono** for UI/dialogue (clean, excellent Latin Extended coverage, multiple weights) + **VT323** or **Press Start 2P** for headings/titles (retro CRT feel).

**Unity integration:** Import .ttf/.otf into Unity, create TextMeshPro font asset via Window → TextMeshPro → Font Asset Creator. Enable **Latin Extended** character sets in the atlas generation settings.

---

## Summary of Key Decisions

| Area | Recommendation |
|------|---------------|
| Spell checking | WeCantSpell.Hunspell v7.0.1 (NuGet) |
| Dictionaries | LibreOffice/dictionaries repo (en_US, ro_RO, fr_FR) |
| LLM integration | Ollama REST API via HttpClient + async/await |
| Primary model | Qwen 2.5 7B Q4_K_M (`qwen2.5:7b`) |
| Fallback model | Llama 3.2 3B Q4_K_M (`llama3.2:3b`) |
| AMD GPU setup | `HSA_OVERRIDE_GFX_VERSION=10.3.0` for ROCm on RX 6600 XT |
| Unity MCP | CoplayDev/unity-mcp via git URL, HTTP transport |
| Unity version | 2022.3 LTS |
| CRT shader | Custom URP Renderer Feature (scanlines + barrel distortion + phosphor) |
| Fonts | IBM Plex Mono (body) + VT323 (headers) |
