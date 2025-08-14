Shader "Unlit/ConcentricRings2D"
{
    Properties{
        _Color       ("Color", Color) = (0.75,0.95,1,1)
        _Intensity   ("Intensity", Range(0,5)) = 1

        // Radius & edges
        _OuterRadius ("Outer Radius", Range(0,3)) = 1
        _EdgeSoft    ("Outer Edge Softness", Range(0.001,1)) = 0.2

        // Ring pattern
        _Freq        ("Ring Frequency", Range(0.5,30)) = 8
        _Speed       ("Inward Speed", Range(-20,20)) = 3
        _LineWidth   ("Ring Line Width", Range(0.001,0.2)) = 0.035

        // Center fade (NEW)
        _InnerSoft   ("Inner Fade Radius", Range(0,1)) = 0.25
        _InnerPow    ("Inner Fade Power", Range(0.5,4)) = 1.5
    }
    SubShader{
        Tags{ "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            fixed4 _Color;
            float  _Intensity, _OuterRadius, _EdgeSoft;
            float  _Freq, _Speed, _LineWidth;
            float  _InnerSoft, _InnerPow;

            v2f vert (appdata v){
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target{
                // UV in -1..1 so center is 0
                float2 p = (i.uv * 2.0 - 1.0);
                float  r = length(p);

                // Fade out at the outer edge
                float outer = 1.0 - smoothstep(_OuterRadius, _OuterRadius + _EdgeSoft, r);

                // Rings: lines where phase is near an integer; rings move inward as time increases
                float phase = r * _Freq + _Time.y * _Speed;
                float cyc   = frac(phase);
                float dist  = min(cyc, 1.0 - cyc);                  // distance to nearest integer line
                float ring  = 1.0 - smoothstep(0.0, _LineWidth, dist);

                // NEW: fade as rings approach center
                float inner = pow(smoothstep(0.0, _InnerSoft, r), _InnerPow);

                float a = ring * outer * inner * _Intensity;
                return fixed4(_Color.rgb * a, a);
            }
            ENDCG
        }
    }
}
