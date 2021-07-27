Shader "Unlit/UnlitTwoSidedColormap"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color1("Color 1", Color) = (0, 0, 1, 1)
        _Color2("Color 2", Color) = (1, 0, 0, 1)
        _TransitionColor("Transition Color", Color) = (0, 0, 0, 1)
        _TransitionValue("Transition value", Range(0.0, 1.0)) = 0.5
        [Space(15)][Enum(Off,2,On,0)] _Cull("Double Sided", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull[_Cull]
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing // GPU Instancing
            #pragma multi_compile_fog // make fog work

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // Single Pass Instanced rendering
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                //UNITY_VERTEX_INPUT_INSTANCE_ID // GPU Instancing, necessary only if you want to access instanced properties in fragment Shader.
                UNITY_VERTEX_OUTPUT_STEREO // Single Pass Instanced rendering
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color1;
            float4 _Color2;
            float4 _TransitionColor;
            float _TransitionValue;

            // GPU Instancing, empty
            //UNITY_INSTANCING_BUFFER_START(Props)
            //UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v); // Single Pass Instanced rendering
                UNITY_INITIALIZE_OUTPUT(v2f, o); // Single Pass Instanced rendering
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); // Single Pass Instanced rendering

                //UNITY_TRANSFER_INSTANCE_ID(v, o); // GPU Instancing, necessary only if you want to access instanced properties in the fragment Shader.

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //UNITY_SETUP_INSTANCE_ID(i); // GPU Instancing, necessary only if you want to access instanced properties in the fragment Shader.

                // sample the texture
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); // Single Pass Instanced rendering
                //fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 col = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST)); // Single Pass Instanced rendering
                float3 colRGB = { col.r, col.g, col.b };
                float mag = length(colRGB);
                float factorColor1 = clamp((_TransitionValue - mag) / (_TransitionValue + 1e-6), 0, 1);
                float factorColor2 = clamp((mag - _TransitionValue) / (1 - _TransitionValue + 1e-6), 0, 1);
                float factorTransitionColor = clamp(1 - factorColor1 - factorColor2, 0, 1);
                col = factorColor1 * _Color1 + factorColor2 * _Color2 + factorTransitionColor * _TransitionColor;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
