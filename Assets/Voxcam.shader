Shader "Voxcam/Voxcam"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _centerX;
            float _centerY;
            float _zoomAmount;

            float boundaryCheck(float2 uv)
            {
                return 0 < uv.x && uv.x < 1 &&
                    0 < uv.y && uv.y < 1;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                uv -= 0.5;  //[0,1] to [-0.5, 0.5]

                //uv += (float2(centerX, centerY) - 0.5); //Remap center to centerX centerY
                //uv /= 1.5;
                float2 tagCenter = float2(_centerX, _centerY);
                tagCenter -= 0.5f;

                uv *= _zoomAmount;
                uv += tagCenter;

                uv += 0.5;
                //uv -= 0.5 - float2(centerX, centerY);
                
                fixed4 col = tex2D(_MainTex, uv);
                col *= boundaryCheck(uv);

                /*float dist = distance(uv, float2(centerX, centerY));
                if(dist < 0.1)
                {
                    
                    //col.r += abs(dist*10);
                    col = fixed4(0,0,0,1);
                    
                }*/
                return col;
            }
            ENDCG
        }
    }
}
