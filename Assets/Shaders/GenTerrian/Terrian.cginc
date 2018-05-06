// create by JiepengTan 
// https://github.com/JiepengTan/GPUAccelerateEndlessTerrian
// 2018-05-04  email: jiepengtan@gmail.com

#define USING_VALUE_NOISE 1
#include "../ShaderLibs/FBM.cginc"

float _PosScale;
float _MaxTerrianH;
float _Eps;

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
    float3 v1 = float3(0,h3-h1,_Eps);
    float3 v2 = float3(-_Eps,h0-h1,0);
    float3 nor1 =  normalize(cross(v1,v2)) ;

    float3 v3 = float3(0,h0-h2,-_Eps);
    float3 v4 = float3(_Eps,h3-h2,0);
    float3 nor2 =  normalize(cross(v3,v4)) ;
    return nor1 + nor2;
}


float3 NormalTerrian( float2 pos){                
    float h[9];
    for (int i = -1; i <= 1.1; ++i){
        for (int j = -1; j <= 1.2; ++j){
            float2 off = float2(j*_Eps,i*_Eps);
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

          
