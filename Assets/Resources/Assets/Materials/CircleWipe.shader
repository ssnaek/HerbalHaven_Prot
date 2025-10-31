Shader "Custom/CircleWipe"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0,0,0,1)
        _Radius ("Radius", Range(0, 1.5)) = 1
        _Smoothness ("Edge Smoothness", Range(0, 0.5)) = 0.001
        [Toggle] _HardEdge ("Hard Edge", Float) = 1
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Overlay" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True"
        }
        
        LOD 100
        
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _HARDEDGE_ON
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Radius;
            float _Smoothness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Center UVs (0.5, 0.5 = center)
                float2 center = float2(0.5, 0.5);
                float2 uv = i.uv;
                
                // Distance from center (max distance is ~0.7 for corners)
                float dist = distance(uv, center);
                
                float circle;
                
                #ifdef _HARDEDGE_ON
                    // Hard edge - step function for crisp cutoff
                    circle = step(_Radius, dist);
                #else
                    // Smooth edge - smoothstep for soft transition
                    // When _Radius is 0, everything is black
                    // When _Radius is large, circle opens to show screen
                    circle = smoothstep(_Radius - _Smoothness, _Radius + _Smoothness, dist);
                #endif
                
                // Apply color with circle mask as alpha
                fixed4 col = _Color;
                col.a = circle;
                
                return col;
            }
            ENDCG
        }
    }
}
