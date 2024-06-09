Shader "Hidden/Pixelate/Smooth-Pixel Upscale"
{
    Properties
    {
        [MainTexture] _MainTex("Texture", 2D) = "white"
        [MainTexture] _UITex("Texture", 2D) = "white"
        [MainTexture] _CursorTex("Texture", 2D) = "white"
    }

    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    struct Attributes
    {
        float4 positionOS   : POSITION;
        float2 uv           : TEXCOORD0;
    };

    struct Varyings
    {
        float4 positionHCS  : SV_POSITION;
        float2 uv           : TEXCOORD0;
    };

    TEXTURE2D(_MainTex);
    SamplerState sampler_bilinear_clamp;
    float4 _MainTex_ST;
    float4 _MainTex_TexelSize;

    TEXTURE2D(_UITex);
    float4 _UITex_ST;
    float4 _UITex_TexelSize;

    TEXTURE2D(_CursorTex);
    float4 _CursorTex_ST;
    float4 _CursorTex_TexelSize;

    Varyings Vert(Attributes IN)
    {
        Varyings OUT;
        OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
        OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
        return OUT;
    }

    float2 BilinearUV(float2 uv, float4 texelSize)
    {
        // Box filter size in texel units
        float2 box_size = clamp(fwidth(uv) * texelSize.zw, 1e-5, 1);
        // Scale uv by texture size to get texel coordinates
        float2 tx = uv * texelSize.zw - 0.5 * box_size;
        // Compute offset for pixel-sized box filter
        float2 tx_offset = smoothstep(1 - box_size, 1, frac(tx));
        // Compute bilinear sample uv coordinates
        float2 sample_uv = (floor(tx) + 0.5 + tx_offset) * texelSize.xy;
        return sample_uv;
    }

    half4 Frag(Varyings IN) : SV_Target
    {
        float2 game_color_uv = BilinearUV(IN.uv, _MainTex_TexelSize);
        float2 ui_color_uv = BilinearUV(IN.uv, _UITex_TexelSize);
        float2 cursor_color_uv = BilinearUV(IN.uv, _CursorTex_TexelSize);

        float4 game_color = SAMPLE_TEXTURE2D(_MainTex, sampler_bilinear_clamp, game_color_uv);
        float4 ui_color = SAMPLE_TEXTURE2D(_UITex, sampler_bilinear_clamp, ui_color_uv);
        float4 cursor_color = SAMPLE_TEXTURE2D(_CursorTex, sampler_bilinear_clamp, cursor_color_uv);

        if (cursor_color.a > 0)
        {
            return cursor_color;
        }

        bool is_transparent = ui_color.a == 0;

        half4 blended_color;
        if (is_transparent)
        {
            blended_color = game_color;
        }
        else
        {
            blended_color = lerp(game_color, ui_color, ui_color.a);
        }

        return blended_color;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma target 2.0
            #pragma editor_sync_compilation
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}