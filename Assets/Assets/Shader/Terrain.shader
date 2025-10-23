Shader "Custom/Terrain"
{
    Properties
    {

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        #pragma target 3.0

        float minHeight;
        float maxHeight;

        struct Input
        {
            float 3 worldPos;
        };

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {

            o.Albedo=float3 (0,1,0);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
