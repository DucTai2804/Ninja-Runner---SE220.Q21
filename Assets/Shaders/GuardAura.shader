Shader "Custom/GuardAura" {
    Properties {
        [HDR] _Color ("Aura Color", Color) = (0.5, 0.1, 1.0, 1.0)
        _WobbleSpeed ("Wobble Speed", Float) = 15.0
        _WobbleScale ("Wobble Scale", Float) = 8.0
        _WobbleAmount ("Wobble Amount", Float) = 0.15
        _Thickness ("Aura Thickness", Float) = 0.4
        _RimPower ("Rim Power", Float) = 1.0
        _MeshScale ("Mesh Scale", Float) = 2.0
        _ZStretch ("Tip Stretch (Z)", Float) = 0.03
        _BoundsMinZ ("Bounds Min Z", Float) = 0.0
        _BoundsCenter ("Bounds Center", Vector) = (0,0,0,0)
        _UniformScale ("Uniform Scale", Float) = 1.0
        
        // THUẬT TOÁN HỌC HỎI TỪ THREEJS
        _RippleSpeed ("Ripple Speed", Float) = 10.0
        _RippleDensity ("Ripple Density", Float) = 15.0
        _RippleAmount ("Ripple Amount", Float) = 0.2 // Tăng lên một chút vì ảo ảnh cần độ bóp méo rõ hơn
    }
    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+50" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
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
                float3 localPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            float4 _Color;
            float _WobbleSpeed;
            float _WobbleScale;
            float _Thickness;
            float _MeshScale;
            float _ZStretch;
            float _BoundsMinZ;
            float4 _BoundsCenter;
            float _UniformScale;
            float _RippleSpeed;
            float _RippleDensity;
            float _RippleAmount;

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
                
                // KIẾN TRÚC MỚI: TẠO CANVAS KHỔNG LỒ (Vùng Đệm) LẤY TÂM LÀM GỐC
                float3 localPos = v.vertex.xyz;
                
                // 0. Phóng to ĐỒNG ĐỀU tĩnh (Uniform Scale) từ trọng tâm
                localPos -= _BoundsCenter.xyz;
                localPos *= _UniformScale;
                localPos += _BoundsCenter.xyz;
                
                // TẮT HOÀN TOÀN BIẾN DẠNG VẬT LÝ VÌ MÔ HÌNH LOW-POLY SẼ BỊ PHÌNH TO!
                // Mọi hiệu ứng gợn sóng sẽ được chuyển xuống Fragment Shader để làm méo dòng năng lượng (Ảo ảnh)!
                
                // 1. Phóng to bề ngang (X, Y) từ tâm hình học (Dành riêng cho Lưỡi kiếm, nhưng Chắn kiếm không dùng nên vẫn an toàn)
                localPos -= _BoundsCenter.xyz;
                localPos.x *= _MeshScale; 
                localPos.y *= _MeshScale; 
                localPos += _BoundsCenter.xyz;
                
                // --- THUẬT TOÁN GỢN SÓNG (BẺ UỐN ĐỈNH 3D NHƯ THREEJS) ---
                // Tính khoảng cách 2D trên mặt phẳng XY từ tâm
                float distXY = length(localPos.xy - _BoundsCenter.xy);
                // Tạo dải sóng hình sin
                float wave = sin(distXY * _RippleDensity - _Time.y * _RippleSpeed) * _RippleAmount;
                // BÍ QUYẾT: Thay vì đẩy ra hai bên làm nó phình to, ta bẻ cong nó dọc theo trục Z (chiều dài kiếm).
                // Nó sẽ uốn lượn nhấp nhô như sóng nước hoặc dải lụa mà không hề bị to ra!
                localPos.z += wave;
                // --------------------------------------------------------
                
                // 2. Kéo dài mũi kiếm (Z) từ đáy chuôi kiếm
                float zOffset = localPos.z - _BoundsMinZ;
                localPos.z += zOffset * _ZStretch; 
                
                o.localPos = localPos;
                
                float3 worldPos = mul(unity_ObjectToWorld, float4(localPos, 1.0)).xyz;
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Đưa tọa độ về gốc tâm hình học để tính khoảng cách
                float3 trueLocal = i.localPos - _BoundsCenter.xyz;
                
                // A. Tạo Gradient Lõi (Core Gradient)
                float dist = length(trueLocal.xy); 
                float shellRadius = _Thickness * _MeshScale; 
                float gradientCore = 1.0 - saturate(dist / shellRadius);
                
                // B. Chuẩn hóa Tọa độ Nhiễu (Bí quyết chống vỡ hạt TV Static)
                float3 normalizedPos = trueLocal / (_Thickness + 0.001); // Tránh chia for 0
                float3 noiseLoc = normalizedPos * float3(_WobbleScale, _WobbleScale, _WobbleScale * 0.25);
                noiseLoc.z -= _Time.y * _WobbleSpeed; 
                
                float n1 = noise3D(noiseLoc);
                float n2 = noise3D(noiseLoc * 2.0 - float3(0, 0, _Time.y * _WobbleSpeed * 1.2));
                float scrollingNoise = (n1 * 0.7) + (n2 * 0.3); 
                
                // C. Kỹ thuật Cắt Viền (Alpha Erosion)
                float energy = gradientCore + scrollingNoise;
                
                // Cắt viền Anime 2D
                float fireMask = smoothstep(0.5, 0.6, energy);
                
                // ĐIỂM XUYẾT TÀN LỬA (Embers)
                float sparkNoise = noise3D(noiseLoc * 4.0 - float3(0, 0, _Time.y * _WobbleSpeed * 1.5));
                float sparks = smoothstep(0.95, 1.0, sparkNoise) * saturate(gradientCore); 
                fireMask += sparks;
                
                // MÀU SẮC HDR TỪ NGƯỜI DÙNG (User-defined Color)
                // Lấy 100% màu sắc và cường độ sáng từ Material Inspector
                float3 finalColor = _Color.rgb; 
                
                float alpha = fireMask * _Color.a;
                clip(alpha - 0.05); // Xóa sổ tàng hình tuyệt đối
                
                // Bỏ nhân đôi độ sáng (finalColor * 2.0) để không bị trắng xóa vì Bloom
                return float4(finalColor, saturate(alpha));
            }
            ENDCG
        }
    }
}
