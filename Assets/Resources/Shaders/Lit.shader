// everything solid in the room: lit by up to two lamps (set from Game.cs as globals) plus a
// dim blue ambient, with no Unity lights involved, so it looks the same everywhere.
// the wall uses it too, with a softer falloff and a little grain.
Shader "ShadowSet/Lit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Emission ("Emission", Color) = (0,0,0,0)
        _Wrap ("Light wrap", Range(0,1)) = 0.3
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst", Float) = 0
        [Enum(Off,0,On,1)] _ZWrite ("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color, _Emission;
            float _Wrap;
            float4 _LampPos0, _LampPos1;        // xyz, w = brightness (0 = off)
            float4 _LampColor, _Ambient;

            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 wpos : TEXCOORD1; float3 n : TEXCOORD2; };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.n = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            float lamp(float4 L, float3 p, float3 n)
            {
                float3 d = L.xyz - p;
                float dist2 = dot(d, d);
                float ndl = dot(n, normalize(d));
                // two-sided for paper cut-outs, with a little wrap so edges aren't pitch black
                ndl = saturate((abs(ndl) + _Wrap) / (1 + _Wrap));
                return L.w * ndl * 170.0 / (dist2 + 30.0);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv) * _Color;
                float3 n = normalize(i.n);
                float light = lamp(_LampPos0, i.wpos, n) + lamp(_LampPos1, i.wpos, n);
                float3 c = tex.rgb * (_Ambient.rgb + _LampColor.rgb * light) + _Emission.rgb;
                // soft roll-off, so things right next to the bulb glow instead of blowing out
                c = 1 - exp(-c * 1.15);
                return fixed4(c, tex.a);
            }
            ENDCG
        }
    }
}
