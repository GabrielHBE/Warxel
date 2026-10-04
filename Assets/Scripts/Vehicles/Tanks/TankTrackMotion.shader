Shader "Warxel/TankTrackMotion"
{
    Properties
    {
        [MainTexture] _BaseMap("Track Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _TrackBounds("Track Bounds", Vector) = (0, 0, 1, 1)
        [HideInInspector] _TrackPhase("Track Phase", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            float4 _TrackBounds; // center Y, center Z, wheel radius, half of straight run
            float _TrackPhase;
        CBUFFER_END

        static const float TRACK_PI = 3.14159265359;

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            float3 positionWS : TEXCOORD2;
        };

        void OriginalTrackPose(float2 q, float radius, float straight,
            out float distance, out float2 radial, out float2 tangent, out float radialDistance)
        {
            if (q.y > straight)
            {
                float2 fromWheel = q - float2(0, straight);
                float angle = atan2(fromWheel.x, fromWheel.y);
                distance = 2 * straight + (TRACK_PI * 0.5 - angle) * radius;
                radial = float2(sin(angle), cos(angle));
                tangent = float2(-cos(angle), sin(angle));
                radialDistance = length(fromWheel);
            }
            else if (q.y < -straight)
            {
                float2 fromWheel = q - float2(0, -straight);
                float angle = atan2(fromWheel.x, fromWheel.y);
                if (angle > 0) angle -= 2 * TRACK_PI;
                distance = 4 * straight + TRACK_PI * radius + (-TRACK_PI * 0.5 - angle) * radius;
                radial = float2(sin(angle), cos(angle));
                tangent = float2(-cos(angle), sin(angle));
                radialDistance = length(fromWheel);
            }
            else if (q.x >= 0)
            {
                distance = q.y + straight;
                radial = float2(1, 0);
                tangent = float2(0, 1);
                radialDistance = q.x;
            }
            else
            {
                distance = 2 * straight + TRACK_PI * radius + straight - q.y;
                radial = float2(-1, 0);
                tangent = float2(0, -1);
                radialDistance = -q.x;
            }
        }

        void MovingTrackPose(float distance, float radius, float straight,
            out float2 centerline, out float2 radial, out float2 tangent)
        {
            float frontArc = TRACK_PI * radius;
            if (distance < 2 * straight)
            {
                centerline = float2(radius, -straight + distance);
                radial = float2(1, 0);
                tangent = float2(0, 1);
            }
            else if (distance < 2 * straight + frontArc)
            {
                float angle = TRACK_PI * 0.5 - (distance - 2 * straight) / radius;
                radial = float2(sin(angle), cos(angle));
                tangent = float2(-cos(angle), sin(angle));
                centerline = float2(0, straight) + radius * radial;
            }
            else if (distance < 4 * straight + frontArc)
            {
                centerline = float2(-radius, straight - (distance - 2 * straight - frontArc));
                radial = float2(-1, 0);
                tangent = float2(0, -1);
            }
            else
            {
                float angle = -TRACK_PI * 0.5 - (distance - 4 * straight - frontArc) / radius;
                radial = float2(sin(angle), cos(angle));
                tangent = float2(-cos(angle), sin(angle));
                centerline = float2(0, -straight) + radius * radial;
            }
        }

        void AnimateTrack(inout float3 positionOS, inout float3 normalOS)
        {
            float radius = max(_TrackBounds.z, 0.001);
            float straight = max(_TrackBounds.w, 0);
            float2 q = positionOS.yz - _TrackBounds.xy;

            float distance;
            float2 oldRadial, oldTangent;
            float radialDistance;
            OriginalTrackPose(q, radius, straight, distance, oldRadial, oldTangent, radialDistance);

            float perimeter = 4 * straight + 2 * TRACK_PI * radius;
            distance = frac((distance + _TrackPhase) / perimeter) * perimeter;

            float2 centerline, newRadial, newTangent;
            MovingTrackPose(distance, radius, straight, centerline, newRadial, newTangent);
            positionOS.yz = _TrackBounds.xy + centerline + (radialDistance - radius) * newRadial;

            float radialNormal = dot(normalOS.yz, oldRadial);
            float tangentNormal = dot(normalOS.yz, oldTangent);
            normalOS.yz = radialNormal * newRadial + tangentNormal * newTangent;
        }

        Varyings TrackVertex(Attributes input)
        {
            Varyings output;
            float3 positionOS = input.positionOS.xyz;
            float3 normalOS = input.normalOS;
            AnimateTrack(positionOS, normalOS);
            output.positionWS = TransformObjectToWorld(positionOS);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(normalOS);
            output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
            return output;
        }
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex TrackVertex
            #pragma fragment TrackFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            half4 TrackFragment(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 normal = normalize(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half diffuse = saturate(dot(normal, mainLight.direction));
                half3 ambient = SampleSH(normal);
                half3 lighting = ambient + mainLight.color * diffuse * mainLight.shadowAttenuation;
                return half4(albedo.rgb * lighting, albedo.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex TrackVertex
            #pragma fragment ShadowFragment
            half4 ShadowFragment(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex TrackVertex
            #pragma fragment DepthFragment
            half4 DepthFragment(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
