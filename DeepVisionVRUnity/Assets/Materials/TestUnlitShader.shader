Shader "Unlit/TestUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing // GPU Instancing
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
    
                UNITY_VERTEX_INPUT_INSTANCE_ID //Insert
            };

            //v2f output struct

            struct v2f
            {

                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;

                //UNITY_VERTEX_INPUT_INSTANCE_ID // GPU Instancing, necessary only if you want to access instanced properties in fragment Shader.
                UNITY_VERTEX_OUTPUT_STEREO //Insert
            };

            // GPU Instancing, empty
            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v)
            {
                v2f o;
    
                UNITY_SETUP_INSTANCE_ID(v); //Insert
                UNITY_INITIALIZE_OUTPUT(v2f, o); //Insert
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //Insert
                //UNITY_TRANSFER_INSTANCE_ID(v, o); // GPU Instancing, necessary only if you want to access instanced properties in the fragment Shader.
    
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            //UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex); //Insert

            fixed4 frag (v2f i) : SV_Target
            {
                
                //UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); //Insert
                //fixed4 col = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv); //Insert
                fixed4 col = tex2D(_MainTex, i.uv);

                return col;
            }
            ENDCG
        }
    }
}
