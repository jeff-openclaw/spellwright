Shader "Hidden/Spellwright/CRT"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "CRT"
            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // CRT parameters (set from C#)
            float _ScanlineIntensity;
            float _ScanlineFrequency;
            float _BarrelDistortion;
            float _ChromaticAberration;
            float _VignetteIntensity;
            float _VignetteRoundness;
            float _PhosphorIntensity;
            float _Brightness;
            float2 _Resolution;

            // Barrel distortion — warps UV to simulate curved CRT glass
            float2 BarrelDistort(float2 uv, float amount)
            {
                float2 centered = uv * 2.0 - 1.0;
                float r2 = dot(centered, centered);
                centered *= 1.0 + amount * r2;
                return centered * 0.5 + 0.5;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // --- Barrel distortion ---
                float2 distortedUV = BarrelDistort(uv, _BarrelDistortion);

                // Clip pixels outside the curved screen edge
                if (distortedUV.x < 0.0 || distortedUV.x > 1.0 ||
                    distortedUV.y < 0.0 || distortedUV.y > 1.0)
                    return half4(0, 0, 0, 1);

                // --- Chromatic aberration — offset R and B channels ---
                float2 caOffset = float2(_ChromaticAberration, 0.0);
                half r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, distortedUV - caOffset).r;
                half g = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, distortedUV).g;
                half b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, distortedUV + caOffset).b;
                half3 color = half3(r, g, b);

                // --- Scanlines — horizontal darkening ---
                float scanline = sin(distortedUV.y * _ScanlineFrequency * 3.14159) * 0.5 + 0.5;
                scanline = lerp(1.0, scanline, _ScanlineIntensity);
                color *= scanline;

                // --- Phosphor grid — RGB subpixel simulation ---
                float pixelX = frac(distortedUV.x * _Resolution.x / 3.0) * 3.0;
                float3 phosphor = float3(
                    smoothstep(0.0, 1.0, 1.0 - abs(pixelX - 0.5)),
                    smoothstep(0.0, 1.0, 1.0 - abs(pixelX - 1.5)),
                    smoothstep(0.0, 1.0, 1.0 - abs(pixelX - 2.5))
                );
                color *= lerp(float3(1, 1, 1), phosphor, _PhosphorIntensity);

                // --- Vignette — darken edges ---
                float2 vignetteUV = uv * 2.0 - 1.0;
                float vignette = 1.0 - dot(vignetteUV * _VignetteRoundness, vignetteUV * _VignetteRoundness);
                vignette = saturate(pow(abs(vignette), _VignetteIntensity));
                color *= vignette;

                // --- Brightness adjustment ---
                color *= _Brightness;

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
