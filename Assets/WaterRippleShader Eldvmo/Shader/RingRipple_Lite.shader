Shader "Custom/RingRipple_Lite_WorldSpace"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Texture("Texture", 2D) = "white" {}
        _Decay("Decay", Range(0,20)) = 5
        _WaveLiftTime("Wave Life Time", Range(1,10)) = 2
        _WaveFrequency("Wave Frequency", Range(0,100)) = 25
        _WaveSpeed("Wave Speed", Range(0,10)) = 0.1
        _WaveStrength("Wave Strength", Range(0,5)) = 0.5
        _StencilRef("Stencil Ref", Range(0,255)) = 1
    }

    SubShader
    {
        Pass
        {   
            Stencil
            {
                Ref [_StencilRef]
                Comp Equal
                Pass Replace
            }

            CGPROGRAM
            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag

            float4 _Color;
            sampler2D _Texture;
            float4 _Texture_ST;
            float _Decay, _WaveLiftTime, _WaveFrequency, _WaveSpeed, _WaveStrength;

            // InputCentre array : xy = input centre (world-space), z = start time
            float4 _InputCentre[10];

            struct VertexInput
            {
                float4 pos : POSITION;
                float2 uv  : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            // World-space ripple wave function
            float Wave(float3 worldPos, float2 centre, float startTime)
            {
                if (startTime <= 0) return 0;

                float age = _Time.y - startTime;
                if (age > _WaveLiftTime) return 0;

                float2 offset = worldPos.xz - centre;
                float distanceFromCentre = length(offset);

                // wave radius grows with time
                float rippleRadius = age * _WaveSpeed;

                float wave = 1.0 - abs(distanceFromCentre - rippleRadius) * _WaveFrequency;
                wave = saturate(wave);

                // distance-based decay
                float spatialDecay = 1.0 - saturate(distanceFromCentre * _Decay);

                // time decay
                float decay = spatialDecay * (1 - age / _WaveLiftTime);

                return wave * _WaveStrength * decay;
            }

            VertexOutput vert(VertexInput i)
            {
                VertexOutput o;

                // get world position for correct scale
                float3 worldPos = mul(unity_ObjectToWorld, i.pos).xyz;

                // accumulate up to 10 waves
                float combinedWave = 0;
                UNITY_LOOP
                for (int n = 0; n < 10; n++)
                {
                    combinedWave += Wave(worldPos, _InputCentre[n].xy, _InputCentre[n].z);
                }

                // offset vertex by ripple height
                worldPos.y += combinedWave * 0.5;

                // transform back to clip space
                o.pos = UnityWorldToClipPos(worldPos);
                o.worldPos = worldPos;
                o.uv = TRANSFORM_TEX(i.uv, _Texture);

                return o;
            }

            float4 frag(VertexOutput o) : SV_TARGET
            {
                float4 tex = tex2D(_Texture, o.uv);
                float combinedWave = 0;

                UNITY_LOOP
                for (int n = 0; n < 10; n++)
                {
                    combinedWave += Wave(o.worldPos, _InputCentre[n].xy, _InputCentre[n].z);
                }

                float4 color = tex * _Color;
                color.rgb += saturate(combinedWave);

                return color;
            }

            ENDCG
        }
    }
}
