Shader "Griptonite/TextureVertColorsAlpha"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _MainTexInt ("Base Intensity", Range(0,4)) = 1
    }
    SubShader
    {
        Tags { "QUEUE"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "QUEUE"="Transparent" "RenderType"="Transparent" }
            Fog { Mode Off }
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct appdata_t
            {
                float4 texcoord0 : TEXCOORD0;
                float4 color : COLOR;
                float3 vertex : POSITION;
            };
            struct v2f
            {
                float2 texcoord0 : TEXCOORD0;
                float4 color : COLOR;
                float4 vertex : POSITION;
            };
            float4 _MainTex_ST;
            float4 _glesMultiTexCoord0;
            float4 _glesColor;
            float4 _glesVertex;
            float _MainTexInt;
            sampler2D _MainTex;
            float4 _MainColor;
            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.texcoord0 = ((v.texcoord0.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
                return o;
            }
            float4 frag(v2f i) : SV_TARGET
            {
                float4 c;
                float4 tmpvar_1;
                tmpvar_1 = tex2D (_MainTex, i.texcoord0);
                float4 tmpvar_2;
                tmpvar_2 = (tmpvar_1 * _MainColor);
                c = tmpvar_2;
                float3 tmpvar_3;
                tmpvar_3 = (c.xyz * (i.color.xyz * _MainTexInt));
                c.xyz = tmpvar_3;
                c.w = (c.w * i.color.w);
                return c;
            }
            ENDCG
        }
    }
}
