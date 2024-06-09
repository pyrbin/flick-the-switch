Shader "Hidden/Pixelate/Edge Highlight Blit"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "Edge Highlight Blit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            float _ConvexHighlight;

            float _OutlineShadow;
            float _ConcaveShadow;

            struct Attributes
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID

                float4 positionHCS : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);

                Varyings output;
                output.positionCS = float4(input.positionHCS.xyz, 1.0);

                #if UNITY_UV_STARTS_AT_TOP
                output.positionCS.y *= -1;
                #endif

                output.texcoord = input.texcoord;
                return output;
            }

            float GetPixelSize()
            {
                return 1 / _ScreenParams.xy;
            }

            float clamp01(float v)
            {
                return clamp(v, 0.0, 1.0);
            }

            float3 Lighten(float3 color, float amount)
            {
                return color * (1.0 + amount);
            }

            float GetNormalDiff(float3 center, float3 side)
            {
                float normalDot = dot(center - side, float3(1.0, 1.0, 1.0));
                normalDot = clamp01(smoothstep(0.0, 0.01, normalDot));
                normalDot = (1.0 - dot(center, side)) * normalDot;
                return normalDot;
            }

            struct DepthNormal
            {
                float depth;
                float3 normal;
            };

            struct Neighbours
            {
                DepthNormal center;
                DepthNormal left;
                DepthNormal right;
                DepthNormal top;
                DepthNormal bottom;

                DepthNormal leftTop;
                DepthNormal rightTop;
                DepthNormal leftBottom;
                DepthNormal rightBottom;
            };

            float Sobel(Neighbours p, float2 uv)
            {
                float3 hor = float3(0, 0, 0);
                hor += p.leftBottom.depth  *  1.0;
                hor += p.rightBottom.depth * -1.0;
                hor += p.left.depth        *  1.0;
                hor += p.right.depth       * -1.0;
                hor += p.leftTop.depth     *  1.0;
                hor += p.rightTop.depth    * -1.0;

                float3 ver = float3(0, 0, 0);
                ver += p.leftBottom.depth  *  1.0;
                ver += p.bottom.depth      *  1.0;
                ver += p.rightBottom.depth *  1.0;
                ver += p.leftTop.depth     * -1.0;
                ver += p.top.depth         * -1.0;
                ver += p.rightTop.depth    * -1.0;

                return sqrt(hor * hor + ver * ver);
            }

            DepthNormal GetDepthNormal(float2 uv)
            {
                DepthNormal depthNormal;
                depthNormal.depth = _CameraDepthTexture.SampleLevel(sampler_CameraDepthTexture, uv, 0).r;
                depthNormal.normal = _CameraNormalsTexture.SampleLevel(sampler_CameraNormalsTexture, uv, 0);
                depthNormal.normal = TransformWorldToViewDir(depthNormal.normal, true);
                return depthNormal;
            }

            Neighbours GetDepthNormalsNeighbours(float2 uv, float2 pixelSize)
            {
                Neighbours neighbours;

                neighbours.center = GetDepthNormal(uv);
                neighbours.left   = GetDepthNormal(uv + float2(-pixelSize.x, 0.0));
                neighbours.right  = GetDepthNormal(uv + float2( pixelSize.x, 0.0));
                neighbours.top    = GetDepthNormal(uv + float2(0.0,  pixelSize.y));
                neighbours.bottom = GetDepthNormal(uv + float2(0.0, -pixelSize.y));

                neighbours.leftTop     = GetDepthNormal(uv + float2(-pixelSize.x,  pixelSize.y));
                neighbours.rightTop    = GetDepthNormal(uv + float2( pixelSize.x,  pixelSize.y));
                neighbours.leftBottom  = GetDepthNormal(uv + float2(-pixelSize.x, -pixelSize.y));
                neighbours.rightBottom = GetDepthNormal(uv + float2( pixelSize.x, -pixelSize.y));

                return neighbours;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 pixelSize = GetPixelSize();
                float4 originalColor = _CameraOpaqueTexture.SampleLevel(sampler_CameraOpaqueTexture, i.texcoord, 0);
                Neighbours p = GetDepthNormalsNeighbours(i.texcoord, pixelSize);

                float sobel = Sobel(p, i.texcoord);
                sobel = smoothstep(0.01, 1, sobel);
                sobel = sobel > 0.001 ? 1 : 0;

                float depthDiff = 0.0;
                depthDiff += p.center.depth - p.left.depth;
                depthDiff += p.center.depth - p.right.depth;
                depthDiff += p.center.depth - p.top.depth;
                depthDiff += p.center.depth - p.bottom.depth;
                depthDiff = smoothstep(0, 0.01, depthDiff);

                float normalDiff = 0;
                normalDiff += GetNormalDiff(p.center.normal, p.left.normal);
                normalDiff += GetNormalDiff(p.center.normal, p.right.normal);
                normalDiff += GetNormalDiff(p.center.normal, p.top.normal);
                normalDiff += GetNormalDiff(p.center.normal, p.bottom.normal);

                float concaveNormalDiff = 0;
                concaveNormalDiff += GetNormalDiff(p.left.normal, p.center.normal);
                concaveNormalDiff += GetNormalDiff(p.right.normal, p.center.normal);
                concaveNormalDiff += GetNormalDiff(p.top.normal, p.center.normal);
                concaveNormalDiff += GetNormalDiff(p.bottom.normal, p.center.normal);

                float outline = depthDiff;
                float convex = 0;
                float concave = 0;

                if (depthDiff > 0)
                {
                    convex = normalDiff;
                    convex = clamp01(convex);
                    convex = clamp01(convex - outline);
                    convex = smoothstep(0.02, 0.3, convex);
                    convex = clamp01(convex) * 2;
                }
                else
                {
                    concave = concaveNormalDiff;
                    concave = clamp01(concave);
                    concave = clamp01(concave - sobel);
                    concave = smoothstep(0, 1, concave);
                }

                float3 col = originalColor.rgb;
                col = Lighten(col, convex * _ConvexHighlight);
                col = Lighten(col, - outline * _OutlineShadow);
                col = Lighten(col, - concave * _ConcaveShadow * _OutlineShadow);

                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
