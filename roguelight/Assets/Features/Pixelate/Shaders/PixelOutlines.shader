

Shader "Hidden/PixelOutlines"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white"
    	_DepthEdgeStrength("_DepthEdgeStrength", Float) = 0.3
    	_NormalEdgeStrength("_NormalEdgeStrength", Float) = 0.4
        _Intensity("_Intensity", Float) = 0.3
    	_Threshold("_Threshold", Float) = 0.4
        _Tint("_Tint", Vector) = (1., 1., 1., 1.)
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
        #pragma vertex vert
        #pragma fragment frag

        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

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

        TEXTURE2D(_MainTex);
        float4 _MainTex_TexelSize;
		float4 _MainTex_ST;
        
        sampler2D _CameraNormalsTexture;
        float4 _CameraNormalsTexture_TexelSize;

        sampler2D _CameraDepthTexture;
        float4 _CameraDepthTexture_TexelSize;

        SamplerState sampler_point_clamp;
        
        uniform float _DepthEdgeStrength;
        uniform float _NormalEdgeStrength;
        uniform float _Intensity;
        uniform float _Threshold;
        float4 _Tint;

        float getDepth(int x, int y, float2 vUv) {
        	#if UNITY_REVERSED_Z
            return 1 - tex2D( _CameraDepthTexture, vUv + float2(x, y)*_MainTex_TexelSize.xy ).r;
			#else
        	return tex2D( _CameraDepthTexture, vUv + float2(x, y)*_MainTex_TexelSize.xy ).r;
        	#endif
        }

        float3 getNormal(int x, int y, float2 vUv) {
            return tex2D( _CameraNormalsTexture, vUv + float2(x, y)*_MainTex_TexelSize.xy ).rgb * 2. - 1.;
        }

		float depthEdgeIndicator(float depth, float2 vUv) {
			float diff = 0.0;
			diff += clamp(getDepth(1, 0, vUv) - depth, 0.0, 1.0);
			diff += clamp(getDepth(-1, 0, vUv) - depth, 0.0, 1.0);
			diff += clamp(getDepth(0, 1, vUv) - depth, 0.0, 1.0);
			diff += clamp(getDepth(0, -1, vUv) - depth, 0.0, 1.0);
			return floor(smoothstep(0.01, 0.02, diff) * 2.) / 2.;
		}
        
        float neighborNormalEdgeIndicator(int x, int y, float depth, float3 normal, float2 vUv)
        {
			float depthDiff = getDepth(x, y, vUv) - depth;
			float3 neighborNormal = getNormal(x, y, vUv);
			
			// Edge pixels should yield to faces who's normals are closer to the bias normal.
			float3 normalEdgeBias = float3(1., 1., 1.); // This should probably be a parameter.
			float normalDiff = dot(normal - neighborNormal, normalEdgeBias);
			float normalIndicator = clamp(smoothstep(-.01, .01, normalDiff), 0.0, 1.0);
			
			// Only the shallower pixel should detect the normal edge.
			float depthIndicator = clamp(sign(depthDiff * .25 + .0025), 0.0, 1.0);

			// return (1.0 - dot(normal, neighborNormal)) * depthIndicator * normalIndicator;
            return distance(normal, neighborNormal) * depthIndicator * normalIndicator;
		}

		float normalEdgeIndicator(float depth, float3 normal, float2 vUv)
        {
			float indicator = 0.0;

			indicator += neighborNormalEdgeIndicator(0, -1, depth, normal, vUv);
			indicator += neighborNormalEdgeIndicator(0, 1, depth, normal, vUv);
			indicator += neighborNormalEdgeIndicator(-1, 0, depth, normal, vUv);
			indicator += neighborNormalEdgeIndicator(1, 0, depth, normal, vUv);

			return step(0.1, indicator);

		}

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
            OUT.screenPosition = ComputeScreenPos(OUT.positionHCS);
            return OUT;
        }
        ENDHLSL

        Pass
        {
            Name "Pixelation"

            HLSLPROGRAM
            float4 frag(Varyings IN) : SV_TARGET
            {
                float4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_point_clamp, IN.uv);

				float depth = 0.0;
				float3 normal = float3(0., 0., 0.);

				if (_DepthEdgeStrength > 0.0 || _NormalEdgeStrength > 0.0) {
					depth = getDepth(0, 0, IN.uv);
					normal = getNormal(0, 0,IN.uv);
				}

				float dei = 0.0;
				if (_DepthEdgeStrength > 0.0) 
					dei = depthEdgeIndicator(depth, IN.uv);

				float nei = 0.0; 
				if (_NormalEdgeStrength > 0.0) 
					nei = normalEdgeIndicator(depth, normal, IN.uv);

            	float strength = dei > 0.0 ? (1.0 - _DepthEdgeStrength * dei) : (1.0 + _NormalEdgeStrength * nei);
            	float borders = float4(nei, nei, nei, 1);

            	// Camera's FAR and NEAR properties directlly correlates to depth outlines since they define the range
            	// of the camera values. Smaller Camera FAR value results in more depth outlines
				// float d = getDepth(0, 0, IN.uv);
				// float4 depthRender = float4(d, d, d, 1);
            	// float4 normalRender = float4(getNormal(0, 0, IN.uv), 1.);
                //return float4(dei, dei, dei, 1);
                float shadowSample = SampleScreenSpaceShadowmap(IN.screenPosition);
            	float lighBorders = borders;

                // if shadowsample black pixels align with neiColor, then reduce that pixel in neiColor by 0.5
                if (shadowSample <= 0 && lighBorders == 1) {
                    lighBorders = 0;
                }

                float shadowBorders = (borders - lighBorders);
                return texel + (texel * lighBorders * _Intensity) + (texel * -shadowBorders * _Intensity);

            	// return depthRender;
            	// return neiColor;
                //float lum = Luminance(outColor);
               // float ratio = _Threshold / (_Threshold + lum);/// GetCurrentExposureMultiplier());

                //return float4(lerp(outColor, lerp(outColor, _Tint.xyz * lum, ratio), _Intensity), 1);
            	
                //return float4(normal, 1);
                //return float4(dei, dei, dei, 1);
                // return float4(nei, nei, nei, 1);
            }
            ENDHLSL
        }

        
    }
}