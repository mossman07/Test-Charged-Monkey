// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Extends UI/Default with crown reveal, dissolve edge, periodic shimmer, halo, and editor preview.
Shader "Test Charged Monkey/UI/Corona"
{
    Properties
    {
        [Header(UI Appearance)]
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(UI Stencil)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        [Header(UI Rendering)]
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        [Header(XR Motion Vectors)]
        [Toggle] _XRMotionVectorsPass("_XRMotionVectorsPass", Float) = 0.0

        [Header(Crown Textures)]
        [NoScaleOffset] _FXTex ("VFX Mask (R Reflect / G Reveal / B Rim / A Halo)", 2D) = "white" {}
        [NoScaleOffset] _NoiseTex ("Erosion Noise Map (R)", 2D) = "gray" {}

        [Header(Reveal and Dissolve)]
        _Reveal ("Reveal (0 Hidden - 1 Visible)", Range(0,1)) = 1
        _DissolveSoftness ("Dissolve Softness", Range(0.001,0.1)) = 0.018
        _DissolveNoise ("Dissolve Noise Contribution", Range(0,0.7)) = 0.28
        _NoiseTiling ("Noise Tiling", Range(1,12)) = 3

        [Header(Dissolve Edge)]
        _EdgeWidth ("Dissolve Edge Width", Range(0.001,0.15)) = 0.035
        [HDR] _EdgeColor ("Dissolve Edge Color", Color) = (1.5,0.65,0.12,1)
        _EdgeIntensity ("Dissolve Edge Intensity", Range(0,3)) = 0.8

        [Header(Shimmer)]
        [ToggleUI] _AutoShimmer ("Automatic Shimmer", Float) = 1
        _ShimmerPosition ("Manual Shimmer Position", Range(-1,1)) = -1
        _ShimmerPeriod ("Shimmer Period (Seconds)", Range(1,12)) = 4.5
        _ShimmerDuration ("Shimmer Duration (Seconds)", Range(0.1,2)) = 0.65
        _ShimmerOffset ("Shimmer Time Offset (Seconds)", Float) = 0
        _ShimmerWidth ("Shimmer Half Width", Range(0.01,0.3)) = 0.10
        _ShimmerIntensity ("Shimmer Intensity", Range(0,3)) = 0.9
        [HDR] _ShimmerColor ("Shimmer Color", Color) = (1.4,1.15,0.65,1)

        [Header(Halo)]
        [HDR] _GlowColor ("Halo Color", Color) = (1,0.45,0.08,1)
        _GlowIntensity ("Halo Intensity", Range(0,2)) = 0.38

        [Header(Opacity)]
        _Opacity ("General Opacity", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="False"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma shader_feature_local_fragment _ CORONA_EDITOR_PREVIEW


            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4  mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;

            // Crown textures and animation parameters.
            sampler2D _FXTex;
            sampler2D _NoiseTex;
            half4 _EdgeColor, _ShimmerColor, _GlowColor;
            float _Reveal, _DissolveSoftness, _DissolveNoise, _NoiseTiling;
            float _EdgeWidth, _EdgeIntensity;
            float _AutoShimmer, _ShimmerPosition, _ShimmerPeriod;
            float _ShimmerDuration, _ShimmerOffset, _ShimmerWidth, _ShimmerIntensity;
            float _GlowIntensity, _Opacity;


            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (v.vertex.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));


                if (_UIVertexColorAlwaysGammaSpace)
                {
                    if(!IsGammaSpace())
                    {
                        v.color.rgb = UIGammaToLinear(v.color.rgb);
                    }
                }

                OUT.color = v.color * _Color;
                return OUT;
            }

            half4 frag(v2f IN) : SV_Target
            {
                //Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
                //The incoming alpha could have numerical instability, which makes it very sensible to
                //HDR color transparency blend, when it blends with the world's texture.
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0/alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision)*invAlphaPrecision;

                half4 surface = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;
                half4 color = IN.color * surface;

                if (_Reveal <= 0 || _Opacity <= 0)
                {
                    return 0;
                }

                // Reveal and dissolve edge.
                half4 fx = tex2D(_FXTex, IN.texcoord);
                half noise = tex2D(_NoiseTex, IN.texcoord * _NoiseTiling).r;
                float rank = lerp(fx.g, noise, _DissolveNoise);
                float dissolveSoftness = max(_DissolveSoftness, fwidth(rank) * 0.75);
                float threshold = lerp(-dissolveSoftness, 1 + dissolveSoftness, saturate(_Reveal));
                half revealed = smoothstep(rank - dissolveSoftness, rank + dissolveSoftness, threshold);
                half edge = (1 - smoothstep(0, _EdgeWidth, max(0, threshold - rank))) * revealed;
                edge *= 1 - step(0.9999, _Reveal);

                // Automatic or controller-driven reflection sweep.
                float period = max(_ShimmerPeriod, 0.1);
                float duration = clamp(_ShimmerDuration, 0.01, period);
                float phase = frac((_Time.y + _ShimmerOffset) / period) * period;
                float autoPosition = lerp(-0.9, 0.9, saturate(phase / duration));
                float position = lerp(_ShimmerPosition, autoPosition, step(0.5, _AutoShimmer));
                half active = lerp(1, 1 - step(duration, phase), step(0.5, _AutoShimmer));

                float diagonal = dot(IN.texcoord - 0.5, float2(0.70710678, -0.70710678));
                half shimmer = 1 - smoothstep(_ShimmerWidth * 0.15, _ShimmerWidth, abs(diagonal - position));
                shimmer *= active * fx.r * lerp(0.65, 1, fx.b) * _ShimmerIntensity;
                half3 gold = lerp(surface.rgb, _ShimmerColor.rgb, saturate(shimmer));
                gold += _EdgeColor.rgb * edge * _EdgeIntensity;

                // Composite the metal and halo before the default UI tint and alpha processing.
                half metalAlpha = surface.a * revealed;
                half haloAlpha = saturate(fx.a * _GlowIntensity) * revealed;
                half alpha = metalAlpha + haloAlpha * (1 - metalAlpha);
                half3 composite = gold * metalAlpha + _GlowColor.rgb * haloAlpha * (1 - metalAlpha);
                color = half4(composite / (alpha > 0 ? alpha : 1), alpha) * IN.color;
                color.a *= _Opacity;

                #ifdef UNITY_UI_CLIP_RECT
                float2 softness = float2(_UIMaskSoftnessX, _UIMaskSoftnessY);
                float2 halfPixelBias = 0.5 - 0.5 * softness * IN.mask.zw;
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw + halfPixelBias);
                half2 hardThreshold = 0.5 + 1e-3 * step(0, IN.mask.xy);
                m = lerp(m, step(hardThreshold, m), step(softness, 0));
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                // Display-encoded color is needed only by the temporary editor preview material.
                #ifdef CORONA_EDITOR_PREVIEW
                half3 linearColor = color.rgb;
                half3 srgbLow = linearColor * 12.9232102;
                half3 srgbHigh = 1.055 * pow(max(abs(linearColor), 1.175494351e-38), 1.0 / 2.4) - 0.055;
                color.rgb = lerp(srgbLow, srgbHigh, step(0.0031308, linearColor));
                #endif

                color.rgb *= color.a;

                return color;
            }
        ENDCG
        }

        Pass
        {
            Name "XRMotionVectors"
            Tags { "LightMode" = "XRMotionVectors" }
            // Stencil write for obj motion pixels
            Stencil
            {
                WriteMask 1
                Ref 1
                Comp Always
                Pass Replace
            }
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            float4x4 unity_MatrixPreviousM;
            #define UNITY_MATRIX_M     unity_ObjectToWorld
            #define UNITY_PREV_MATRIX_M   unity_MatrixPreviousM
            float4x4 _PrevViewProjMatrixStereo[2];
            float4x4 _NonJitteredViewProjMatrixStereo[2];
            #define  _PrevViewProjMatrix  _PrevViewProjMatrixStereo[unity_StereoEyeIndex]
            #define  _NonJitteredViewProjMatrix _NonJitteredViewProjMatrixStereo[unity_StereoEyeIndex]
            float _SpaceWarpNDCModifier;

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP


            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 oldvertex: TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float4 positionCSNoJitter         : POSITION_CS_NO_JITTER;
                float4 previousPositionCSNoJitter : PREV_POSITION_CS_NO_JITTER;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4  mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;

            float _XRMotionVectorsPass;
            float xrMotionVectorsTransparentThreshold = 0.001;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                // We do not need jittered position in ASW
                float4 vPosition = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, v.vertex));
                OUT.positionCSNoJitter = vPosition;
                OUT.previousPositionCSNoJitter = mul(_PrevViewProjMatrix, mul(UNITY_PREV_MATRIX_M, v.oldvertex));
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (v.vertex.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));


                if (_UIVertexColorAlwaysGammaSpace)
                {
                    if(!IsGammaSpace())
                    {
                        v.color.rgb = UIGammaToLinear(v.color.rgb);
                    }
                }

                OUT.color = v.color * _Color;

                return OUT;
            }

            float4 frag(v2f IN) : SV_Target
            {
                //Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
                //The incoming alpha could have numerical instability, which makes it very sensible to
                //HDR color transparency blend, when it blends with the world's texture.
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0/alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision)*invAlphaPrecision;

                half4 color = IN.color * (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd);

                #ifdef UNITY_UI_CLIP_RECT
                float2 softness = float2(_UIMaskSoftnessX, _UIMaskSoftnessY);
                float2 halfPixelBias = 0.5 - 0.5 * softness * IN.mask.zw;
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw + halfPixelBias);
                half2 hardThreshold = 0.5 + 1e-3 * step(0, IN.mask.xy);
                m = lerp(m, step(hardThreshold, m), step(softness, 0));
                color.a *= m.x * m.y;
                #endif

                clip (color.a * _XRMotionVectorsPass - 0.001);

                float3 posNDC = IN.positionCSNoJitter.xyz * rcp(IN.positionCSNoJitter.w);
                float3 prevPosNDC = IN.previousPositionCSNoJitter.xyz * rcp(IN.previousPositionCSNoJitter.w);

                float3 velocity;
                // Calculate forward velocity
                velocity = (posNDC.xyz - prevPosNDC.xyz);

                #if UNITY_UV_STARTS_AT_TOP
                velocity.y = velocity.y * _SpaceWarpNDCModifier;
                #endif

                return float4(velocity.xyz, 1.0);
            }
        ENDCG
        }
    }
}