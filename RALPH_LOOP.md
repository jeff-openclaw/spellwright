# 🐦 Ralph Wiggum Loop — Unity CRT + Text Pass
> Claude Code agentic loop. Run this with: `claude --loop` or paste into a Claude Code session.
> Ralph says: "I picked a bad day to stop wearing my helmet." Let's fix the game instead.

---

## LOOP GOAL
You are an autonomous agent improving a Unity game. You will:
1. Audit and fix text sizes across all UI elements
2. Enhance the existing CRT post-processing effect with:
   - Screen curvature (barrel/pincushion distortion like Balatro)
   - Scanline artifacts
   - Chromatic aberration noise ("purici" — pixel noise/static grain)
   - Vignette darkening at edges

Work **iteratively**: complete one task, verify, move to the next. Never stop after one file.

---

## PHASE 1 — TEXT SIZE AUDIT

### Step 1.1 — Find all UI text components
```bash
grep -r "fontSize\|TextMeshPro\|Text\b" Assets/ --include="*.cs" -l
grep -r "fontSize\|Font Size" Assets/ --include="*.unity" --include="*.prefab" -l
```

### Step 1.2 — Apply text size standards
Scan every `.cs`, `.unity`, and `.prefab` file. Enforce this scale:

| Context | Size |
|---|---|
| Body / default UI | 24–28 |
| Labels / captions | 20–22 |
| Headings / titles | 36–48 |
| HUD / in-game status | 26–30 |
| Tooltips / small | 18 |

- For TextMeshPro components: set `fontSize` in code or serialized field
- For legacy `UnityEngine.UI.Text`: set `.fontSize`
- Prefer **relative sizing** via Canvas Scaler (`Scale With Screen Size`, ref 1920×1080)

### Step 1.3 — Verify Canvas Scaler
Find `Canvas` GameObjects. Ensure:
```
UI Scale Mode: Scale With Screen Size
Reference Resolution: 1920 x 1080
Screen Match Mode: Match Width Or Height (0.5)
```

---

## PHASE 2 — CRT SHADER ENHANCEMENT

### Step 2.1 — Locate existing CRT shader/script
```bash
find Assets/ -name "*.shader" -o -name "*.hlsl" | xargs grep -l -i "crt\|scanline\|vignette" 2>/dev/null
find Assets/ -name "*.cs" | xargs grep -l -i "crt\|scanline\|postprocess" 2>/dev/null
```

### Step 2.2 — Upgrade or create CRT shader

Create or replace `Assets/Shaders/CRTEffect.shader` with the following full implementation:

```hlsl
Shader "Custom/CRTEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        
        // Curvature
        _CurvatureAmount ("Curvature Amount", Range(0, 0.5)) = 0.12
        
        // Scanlines
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.25
        _ScanlineCount ("Scanline Count", Range(100, 800)) = 320.0
        
        // Chromatic Aberration
        _ChromaticAberration ("Chromatic Aberration", Range(0, 0.01)) = 0.003
        
        // Noise / "Purici" (static grain)
        _NoiseIntensity ("Noise Intensity (Purici)", Range(0, 0.15)) = 0.04
        _NoiseSpeed ("Noise Speed", Range(0, 20)) = 8.0
        
        // Vignette
        _VignetteIntensity ("Vignette Intensity", Range(0, 2)) = 0.8
        _VignetteSmoothness ("Vignette Smoothness", Range(0.1, 1)) = 0.4
        
        // Brightness / contrast
        _Brightness ("Brightness", Range(0.5, 1.5)) = 1.05
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always Cull Off ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            
            float _CurvatureAmount;
            float _ScanlineIntensity;
            float _ScanlineCount;
            float _ChromaticAberration;
            float _NoiseIntensity;
            float _NoiseSpeed;
            float _VignetteIntensity;
            float _VignetteSmoothness;
            float _Brightness;

            // --- Barrel / CRT curvature distortion ---
            float2 CurveUV(float2 uv)
            {
                uv = uv * 2.0 - 1.0;
                float2 offset = uv.yx * uv.yx * _CurvatureAmount;
                uv += uv * offset;
                uv = uv * 0.5 + 0.5;
                return uv;
            }

            // --- Pseudo-random noise for "purici" ---
            float rand(float2 co, float time)
            {
                return frac(sin(dot(co.xy + time * 0.001, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = CurveUV(i.uv);
                
                // Kill pixels outside curved screen edges (black border)
                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                    return fixed4(0, 0, 0, 1);

                // Chromatic aberration — R/G/B channels slightly offset
                float2 dir = (uv - 0.5) * _ChromaticAberration;
                float r = tex2D(_MainTex, uv + dir).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, uv - dir).b;
                fixed4 col = fixed4(r, g, b, 1.0);

                // Scanlines
                float scan = sin(uv.y * _ScanlineCount * 3.14159);
                scan = scan * 0.5 + 0.5; // remap to [0,1]
                col.rgb -= (1.0 - scan) * _ScanlineIntensity;

                // "Purici" — animated pixel static/grain
                float noise = rand(uv, floor(_Time.y * _NoiseSpeed));
                col.rgb += (noise - 0.5) * _NoiseIntensity;

                // Vignette
                float2 vigUV = uv * (1.0 - uv.yx);
                float vig = vigUV.x * vigUV.y * 15.0;
                vig = pow(vig, _VignetteIntensity * _VignetteSmoothness);
                col.rgb *= vig;

                // Brightness
                col.rgb *= _Brightness;

                return col;
            }
            ENDCG
        }
    }
}
```

### Step 2.3 — Create or update CRT Post-Process script

Create `Assets/Scripts/CRTPostProcess.cs`:

```csharp
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CRTPostProcess : MonoBehaviour
{
    [Header("Shader")]
    public Shader crtShader;
    private Material _mat;

    [Header("Curvature")]
    [Range(0f, 0.5f)] public float curvatureAmount = 0.12f;

    [Header("Scanlines")]
    [Range(0f, 1f)]   public float scanlineIntensity = 0.25f;
    [Range(100, 800)] public float scanlineCount = 320f;

    [Header("Chromatic Aberration")]
    [Range(0f, 0.01f)] public float chromaticAberration = 0.003f;

    [Header("Purici (Static Noise)")]
    [Range(0f, 0.15f)] public float noiseIntensity = 0.04f;
    [Range(0f, 20f)]   public float noiseSpeed = 8f;

    [Header("Vignette")]
    [Range(0f, 2f)]    public float vignetteIntensity = 0.8f;
    [Range(0.1f, 1f)]  public float vignetteSmoothness = 0.4f;

    [Header("Color")]
    [Range(0.5f, 1.5f)] public float brightness = 1.05f;

    private void OnEnable()
    {
        if (crtShader == null)
            crtShader = Shader.Find("Custom/CRTEffect");

        if (crtShader != null)
            _mat = new Material(crtShader);
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (_mat == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        _mat.SetFloat("_CurvatureAmount",     curvatureAmount);
        _mat.SetFloat("_ScanlineIntensity",   scanlineIntensity);
        _mat.SetFloat("_ScanlineCount",       scanlineCount);
        _mat.SetFloat("_ChromaticAberration", chromaticAberration);
        _mat.SetFloat("_NoiseIntensity",      noiseIntensity);
        _mat.SetFloat("_NoiseSpeed",          noiseSpeed);
        _mat.SetFloat("_VignetteIntensity",   vignetteIntensity);
        _mat.SetFloat("_VignetteSmoothness",  vignetteSmoothness);
        _mat.SetFloat("_Brightness",          brightness);

        Graphics.Blit(src, dest, _mat);
    }

    private void OnDisable()
    {
        if (_mat != null)
            DestroyImmediate(_mat);
    }
}
```

### Step 2.4 — Wire it up
- Attach `CRTPostProcess` to the **main camera** (or a dedicated post-process camera)
- Assign `CRTEffect` shader to the `crtShader` field
- If using **URP/HDRP**: wrap as a `ScriptableRendererFeature` — see note below

---

## PHASE 3 — URP / HDRP ADAPTATION (if needed)

Check render pipeline:
```bash
grep -r "UniversalRenderPipeline\|HDRenderPipeline" Assets/ ProjectSettings/ --include="*.asset" -l
```

If **URP** is detected, `OnRenderImage` won't work. Instead:

1. Create `Assets/Scripts/CRTRenderFeature.cs` as a `ScriptableRendererFeature`
2. Create `Assets/Scripts/CRTRenderPass.cs` as a `ScriptableRenderPass`
3. Use `Blit(cmd, source, dest, material)` inside the render pass
4. Register the feature in the URP Renderer asset

_(Ask Claude Code to generate the URP version if the pipeline check above finds URP assets.)_

---

## PHASE 4 — VERIFICATION LOOP

After each phase, run these checks:

```bash
# Check no compile errors exist in modified files
grep -rn "TODO\|FIXME\|HACK" Assets/Scripts/CRTPostProcess.cs Assets/Shaders/CRTEffect.shader

# Confirm shader was created
ls -la Assets/Shaders/CRTEffect.shader
ls -la Assets/Scripts/CRTPostProcess.cs

# Sanity check text component counts
grep -r "fontSize" Assets/ --include="*.cs" | wc -l
```

---

## LOOP EXIT CONDITIONS

Stop and report when:
- [ ] All Text/TMP components have audited font sizes
- [ ] `CRTEffect.shader` exists and compiles (no `#error` lines)
- [ ] `CRTPostProcess.cs` exists and is attached to camera in at least one scene
- [ ] No null-reference errors in the modified scripts
- [ ] Curvature, scanlines, chromatic aberration, and purici parameters are all exposed and tunable in the Inspector

---

## BALATRO REFERENCE VALUES
For a Balatro-like feel, use these starting values:
```
Curvature:           0.10 – 0.15
Scanline Intensity:  0.20 – 0.30
Scanline Count:      240 – 400
Chromatic Aberration: 0.002 – 0.005
Noise (Purici):      0.03 – 0.06  (subtle but visible)
Vignette:            0.7 – 1.0
Brightness:          1.02 – 1.08
```

---

*Ralph says: "My cat's breath smells like cat food." Your game will smell like Balatro. 🐱📺*
