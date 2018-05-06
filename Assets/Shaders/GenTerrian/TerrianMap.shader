// create by JiepengTan 
// https://github.com/JiepengTan/GPUAccelerateEndlessTerrian
// 2018-05-04  email: jiepengtan@gmail.com
Shader "FishManShaderTutorial/TerrianMap" {
    Properties{
        _MaxTerrianH ("_MaxTerrianH", float) = 500. 
        _PosScale ("_PosScale", float) = 500.
        _Eps ("_Eps", float) = 1. 
        _Offset ("_Offset", Vector) = (0.,0., 0., 0.) // color
        _WidHigh ("_WidHigh", Vector) = (241.,241., 1, 1) // color
    }
    SubShader{
        Pass {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
#pragma vertex vert  
#pragma fragment frag  
#include "Terrian.cginc"

            float2 _Offset;
            float2 _WidHigh;

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
                //float3 nor = NormalTerrian(pos);
                return float4(y,0.,1.,0.);
            }
            ENDCG
        }//end pass  
    }//end SubShader 
    FallBack Off
}

