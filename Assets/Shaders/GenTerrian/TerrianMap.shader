// create by JiepengTan 
// https://github.com/JiepengTan/GPUAccelerateEndlessTerrian
// 2018-05-04  email: jiepengtan@gmail.com
Shader "FishManShaderTutorial/TerrianMap" {
    Properties{
        _MaxTerrianH ("_MaxTerrianH", float) = 500. 
        _PosScale ("_PosScale", float) = 500.
        _NorVertDist ("_NorVertDist", float) = 1. 
        _Offset ("_Offset", Vector) = (0.,0., 0., 0.) // color
        _WidHigh ("_WidHigh", Vector) = (241.,241., 1, 1) // color
    }
    SubShader{
        Pass {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
#define USING_VALUE_NOISE 1
#pragma vertex vert  
#pragma fragment frag  
#include "../ShaderLibs/FBM.cginc"

            float _PosScale;
            float _MaxTerrianH;
            float2 _Offset;
            float2 _WidHigh;
            float _NorVertDist;

            float TerrainH(float2 pos){
                float2 p = pos*_PosScale/_MaxTerrianH;
                float a = 0.0;
                float b = 0.491;
                float2  d = float2(0.0,0.);
                for( int i=0; i<5.; i++ ){
                    float n = VNoise(p);
                    a += b*n;
                    b *= 0.49;
                    p = p*2.01;
                }
                return _MaxTerrianH*a ;
            }  


            float3 SumRectNorm(float h0,float h1, float h2,float h3){ 
                float3 v1 = float3(0,h3-h1,_NorVertDist);
                float3 v2 = float3(-_NorVertDist,h0-h1,0);
                float3 nor1 =  normalize(cross(v1,v2)) ;

                float3 v3 = float3(0,h0-h2,-_NorVertDist);
                float3 v4 = float3(_NorVertDist,h3-h2,0);
                float3 nor2 =  normalize(cross(v3,v4)) ;
                return nor1 + nor2;
            }


            float3 NormalTerrian( float2 pos){                
                float h[9];
                for (int i = -1; i <= 1.1; ++i){
                    for (int j = -1; j <= 1.2; ++j){
                        float2 off = float2(j*_NorVertDist,i*_NorVertDist);
                        h[(i+1)*3+(j+1)] = TerrainH(pos + off);
                    }
                }

                float3 nor = float3(0.,0.,0.);
                nor += SumRectNorm(h[0],h[1],h[3],h[4]);
                nor += SumRectNorm(h[1],h[2],h[4],h[5]);
                nor += SumRectNorm(h[3],h[4],h[6],h[7]);
                nor += SumRectNorm(h[4],h[5],h[7],h[8]);
                return -normalize(nor);
            }

            sampler2D _MainTex;
            float4 _MainTex_ST;
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

            float4 frag(v2f i) : SV_Target{
                float2 pos = floor(i.uv  * _WidHigh) + _Offset;
                float y = TerrainH(pos);
                float3 nor = NormalTerrian(pos);
                return float4(y,nor);
            }
            ENDCG
        }//end pass  
    }//end SubShader 
    FallBack Off
}

