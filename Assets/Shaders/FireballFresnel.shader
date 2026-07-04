Shader "Custom/FireballFresnel"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewNormal : TEXCOORD1;
            };

            sampler2D _MainTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; // Dùng lại UV mặc định mượt mà của Unity Sphere

                // Three.js: vNormal = normalize(normalMatrix * normal)
                o.viewNormal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 scrolledUv = i.uv;

                // Đổi dấu từ -0.4 thành 0.4 để đảo ngược dòng chảy: tỏa ra từ đầu và tụ lại ở đuôi
                scrolledUv.x += _Time.y * (-0.1);
                scrolledUv.y += _Time.y * (0.4);

                float3 fireColor = tex2D(_MainTex, scrolledUv).rgb;

                // Color grading giong Three.js
                fireColor *= float3(1.5, 0.8, 0.2) * 1.5;

                // Fresnel: vien sang o ria qua cau
                float3 vNormal = normalize(i.viewNormal);
                float fresnel = pow(1.0 - abs(vNormal.z), 2.5);
                fireColor += float3(1.0, 0.5, 0.1) * fresnel * 1.5;

                return fixed4(fireColor, 1.0);
            }
            ENDCG
        }
    }
}
