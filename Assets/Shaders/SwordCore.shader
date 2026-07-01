Shader "Custom/SwordCore" {
    Properties {
        [HDR] _Color ("Core Color", Color) = (3.0, 2.5, 3.5, 1.0)
        _WobbleSpeed ("Wobble Speed", Float) = 15.0
        _WobbleScale ("Wobble Scale", Float) = 10.0
        _WobbleAmount ("Wobble Amount", Float) = 0.05
    }
    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Off
        
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            float4 _Color;
            float _WobbleSpeed;
            float _WobbleScale;
            float _WobbleAmount;

            float hash(float n) { return frac(sin(n) * 43758.5453123); }
            float noise3D(float3 x) {
                float3 p = floor(x);
                float3 f = frac(x);
                f = f*f*(3.0-2.0*f);
                float n = p.x + p.y*57.0 + 113.0*p.z;
                return lerp(lerp(lerp( hash(n+  0.0), hash(n+  1.0),f.x),
                                 lerp( hash(n+ 57.0), hash(n+ 58.0),f.x),f.y),
                            lerp(lerp( hash(n+113.0), hash(n+114.0),f.x),
                                 lerp( hash(n+170.0), hash(n+171.0),f.x),f.y),f.z);
            }

            v2f vert (appdata v) {
                v2f o;
                
                // 1. Lấy tọa độ và pháp tuyến trong Không Gian Thế Giới (World Space)
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                
                // 2. Tính toán Noise dựa trên World Space để hoàn toàn miễn nhiễm với Scale của Object!
                float3 noiseLoc = worldPos * _WobbleScale + float3(0, -_Time.y * _WobbleSpeed, 0);
                float displacement = noise3D(noiseLoc) * _WobbleAmount;
                
                // 3. Phình to các đỉnh theo hướng Normal ngay trong World Space
                worldPos += worldNormal * displacement;
                
                // 4. Chuyển thẳng từ World Space sang màn hình (Clip Space)
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                
                o.color = _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                return i.color;
            }
            ENDCG
        }
    }
}
