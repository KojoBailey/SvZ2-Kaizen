Shader "GripUnlit/TwoTextureBlend (Supports Lightmap)_LM"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _SecondTex ("Second (RGB)", 2D) = "white" {}
        _LightMap ("Light Map", 2D) = "white" {}
    }
    SubShader
    { 
        Tags { "RenderType"="Opaque" }
        Pass {
            Tags { "LIGHTMODE"="Vertex" "RenderType"="Opaque" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 _MainTex_ST, _SecondTex_ST, _LightMap_ST;
            sampler2D _MainTex;
            sampler2D _SecondTex, _LightMap;
            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
            };
            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.texcoord0 = v.texcoord0;
                o.texcoord1 = v.texcoord1 * _MainTex_ST.xy + _MainTex_ST.zw;
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                return lerp(tex2D(_MainTex, i.texcoord0 * _MainTex_ST.xy + _MainTex_ST.zw), tex2D(_SecondTex, i.texcoord0 * _SecondTex_ST.xy + _SecondTex_ST.zw), 1.0 - i.color.a) * i.color * (tex2D(_LightMap, i.texcoord1) * 2.0);
            }
            ENDCG
        }
    }
}