
// create by JiepengTan 
// https://github.com/JiepengTan/FishManShaderTutorial
// 2018-04-30  email: jiepengtan@gmail.com
Shader "FishManShaderTutorial/TerrianRender" {
    Properties{
        _MaxTerrianH ("_MaxTerrianH", float) = 500. // color
        _Offset ("_Offset", Vector) = (0.,0., 0., 0.) // color
        _WidHigh ("_WidHigh", Vector) = (241.,241., 1, 1) // color
    }
    SubShader{
        Tags { "RenderType"="Opaque" }
        LOD 100
        Pass {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM

            #pragma multi_compile_fwdbase   
#pragma vertex vert  
#pragma fragment frag  
// Need these files to get built-in macros
#include "Lighting.cginc"
#include "AutoLight.cginc"
#define USING_PERLIN_NOISE 1
#include "../ShaderLibs/FBM.cginc"

            
            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
			
			struct v2f {
				float4 pos : SV_POSITION;
				float3 worldNormal : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
                float3 loaclNor: TEXCOORD2;
				SHADOW_COORDS(3)
			};

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.loaclNor = v.normal;
                TRANSFER_SHADOW(o);
                
                return o;
            }
            
            float4 frag(v2f i) : SV_Target{
				
                fixed3 nor = normalize(i.worldNormal);
                fixed3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 col = float3(0.,0.,0.);
                /**/
                //base color 
                col = float3(0.10,0.09,0.08);
                //lighting     
                float amb = 0.5;//clamp(0.5+0.5*nor.y,0.0,1.0);
                float dif = clamp( dot( lightDir, nor ), 0.0, 1.0 );
                float bac = clamp( 0.2 + 0.8*dot( normalize( float3(-lightDir.x, 0.0, lightDir.z ) ), nor ), 0.0, 1.0 );
                
                fixed atten = 1.0;
                
                fixed sh = 1.;//SHADOW_ATTENUATION(i);
                //shadow
                sh = sh * atten;
        
                //brdf 
                float3 lin  = float3(0.0,0.0,0.0);
                lin += dif*float3(7.00,5.00,3.00)*float3( sh, sh*sh*0.5+0.5*sh, sh*sh*0.8+0.2*sh );
                lin += amb*float3(0.40,0.60,1.00)*1.2;
                lin += bac*float3(0.40,0.50,0.60); 
                col *= lin;

                // fog
                // float fo = 1.0-exp(-pow(0.1*rz/_MaxTerrianH,1.5));
                //float3 fco = 0.65*float3(0.4,0.65,1.0);
                //col = lerp( col, fco, fo );

                return float4(col,1.);
            }
            ENDCG
        }//end pass  
    }//end SubShader 
    FallBack "Diffuse"
}

