Shader "Hidden/Pixelate/Dithering"
{
    Properties
    {
        [MainTexture] _MainTex("Texture", 2D) = "white"
        _DitherThreshold("DitherThreshold", Float) = 0.5
    	_DitherStrength("DitherStrength", Float) = 0.5
        _DitherScale("DitherScale", Float) = 1
    }

    HLSLINCLUDE
    #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"


    TEXTURE2D(_MainTex);
    SamplerState sampler_bilinear_clamp;
    uniform float4 _MainTex_ST;
    uniform float4 _MainTex_TexelSize;

    uniform float _DitherThreshold;
    uniform float _DitherStrength;
    uniform float _DitherScale;

    struct Attributes
    {
        float4 positionOS : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct Varyings
    {
        float4 positionHCS : SV_POSITION;
        float2 uv : TEXCOORD0;
        float4 screenPosition : TEXCOORD1;
    };
    
    half ShadowAttenuation(float2 shadowCoord)
    {
        //return MainLightRealtimeShadow(TransformWorldToShadowCoord(worldPosition));
        return half(SAMPLE_TEXTURE2D(_ScreenSpaceShadowmapTexture, sampler_bilinear_clamp, shadowCoord).x);
    }

    float4 GetTexelSize(float width, float height)
    {
        return float4(1/width, 1/height, width, height);
    }
    
    float Get4x4TexValue(float2 uv, float brightness, float4x4 pattern)
    {        
        uint x = uv.x % 4;
        uint y = uv.y % 4;
        if((brightness * _DitherThreshold) < pattern[x][y]) 
            return 0;
        else 
            return 1;
    }      
    
    Varyings Vert(Attributes IN)
    {
        Varyings OUT;
        OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
        OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
        OUT.screenPosition = ComputeScreenPos(OUT.positionHCS);
        return OUT;
    }

    float4 Frag (Varyings IN) : SV_Target
    {
        float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_bilinear_clamp, IN.uv);
        float4 texelSize = GetTexelSize(1, 1);

        float2 screenPos = IN.screenPosition.xy / IN.screenPosition.w;
        uint2 ditherCoordinate = screenPos * _ScreenParams.xy * texelSize.xy;

        ditherCoordinate /= _DitherScale;
        
        float shadowSample = SampleScreenSpaceShadowmap(IN.screenPosition);
        float brightness = shadowSample;

        float4x4 ditherPattern = float4x4
        (
            0 , 0.5 , 0 , 0.5 ,
            0.5 , 0 , 0.5 , 0 ,
            0 , 0.5 , 0 , 0.5 ,
            0.5 , 0 , 0.5 , 0 
        );

        float ditherPixel = Get4x4TexValue(ditherCoordinate.xy, brightness, ditherPattern);
        return color * ditherPixel;        
    }
    ENDHLSL
    
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Tags { "RenderPipeline" = "UniversalPipeline"}
        Pass
        {
            HLSLPROGRAM
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
