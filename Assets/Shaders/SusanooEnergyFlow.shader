Shader "Custom/SusanooEnergyFlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _HasMap ("Has Texture", Float) = 1.0
        [HDR] _Color1 ("Energy Color", Color) = (0.627, 0.329, 0.992, 1.0) 
        [HDR] _Color2 ("Energy Highlight", Color) = (0.6198, 0.4626, 0.7578, 1.0)
        _NoiseScale ("Noise Scale", Float) = 10.0
        _NoiseMin ("Noise Min", Float) = 0.2
        _NoiseMax ("Noise Max", Float) = 0.8
        _FlowSpeed ("Flow Speed", Float) = 6.0
        _ModelScale ("Model Scale", Float) = 1.0
        _FresnelIntensity ("Fresnel Intensity", Range(0.0, 5.0)) = 0.5
    }
    SubShader
    {
        // Trở về mô hình Đặc (Opaque) để che hoàn toàn các khớp nối và background
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float3 restPos : TEXCOORD1; // Lấy tọa độ Rest Pose từ kênh UV2
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 localPos : TEXCOORD1;
                float3 worldNormal : NORMAL;
                float3 viewDir : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _HasMap;
            float4 _Color1;
            float4 _Color2;
            float _NoiseScale;
            float _NoiseMin;
            float _NoiseMax;
            float _FlowSpeed;
            float _ModelScale;
            float _FresnelIntensity;

            // --- 2. HÀM GLSL_NOISE (Value Noise chuẩn của ThreeJS) ---
            float hash(float3 p) {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float noise(float3 x) {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(hash(i + float3(0,0,0)), hash(i + float3(1,0,0)), f.x),
                               lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                           lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x),
                               lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y), f.z);
            }
            // --- End Noise ---

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                // BÍ QUYẾT TỐI THƯỢNG: Nếu C# đã đóng băng Rest Pose thành công vào UV2 thì dùng nó!
                // Nếu FBX chưa bật Read/Write, biến này bằng (0,0,0) nên ta fallback về v.vertex
                float3 rest = v.restPos;
                if (length(rest) < 0.0001) {
                    rest = v.vertex.xyz;
                }
                o.localPos = rest;
                
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(UnityWorldSpaceViewDir(worldPos));
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.viewDir);
                
                float fresnel = 1.0 - max(dot(viewDir, normal), 0.0);
                fresnel = pow(fresnel, 2.0);
                
                float4 texColor = float4(1.0, 1.0, 1.0, 1.0);
                if (_HasMap > 0.5) 
                {
                    texColor = tex2D(_MainTex, i.uv);
                }

                // KHẮC PHỤC LỖI FBX LẬT TRỤC (Biến dạng tại chỗ thay vì chảy lên trên)
                float3 localUp = mul(unity_WorldToObject, float4(0.0, 1.0, 0.0, 0.0)).xyz;
                localUp = normalize(localUp);
                float3 flowDir = -localUp * _Time.y * _FlowSpeed;
                
                // Dùng lại Value Noise chuẩn
                float n1 = noise((i.localPos * _ModelScale) * _NoiseScale + flowDir);
                
                float intensity = smoothstep(_NoiseMin, _NoiseMax, n1);
                
                // Blend màu chuẩn 100% ThreeJS
                float3 finalColor = lerp(_Color1.rgb, _Color2.rgb, intensity) + (_Color1.rgb * fresnel * _FresnelIntensity);
                
                if (_HasMap > 0.5) 
                {
                    float4 texColor = tex2D(_MainTex, i.uv);
                    finalColor *= texColor.rgb * 2.5;
                }
                
                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}
