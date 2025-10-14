Shader "UI/CutoutMask"
{
  Properties
    {
        _Color ("Color", Color) = (0,0,0,0.7)
        _HoleCenter ("Hole Center", Vector) = (0.5, 0.5, 0, 0)
        _HoleSize ("Hole Size", Vector) = (0.3, 0.3, 0, 0)
        _Feather ("Feather", Range(0,0.1)) = 0.01
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };
            float4 _Color;
            float4 _HoleCenter;
            float4 _HoleSize;
            float _Feather;

            v2f vert (appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 diff = abs(i.uv - _HoleCenter.xy);
                float2 edge = step(_HoleSize.xy, diff);
                float hole = max(edge.x, edge.y);
                float alpha = lerp(0, _Color.a, smoothstep(0.0, _Feather, hole));
                return float4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}