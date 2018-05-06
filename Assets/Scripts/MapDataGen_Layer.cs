using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;


//将整个地图分成不同的layer 
//顶点法线计算: 
//贴图大小:
//每层的贴图大小一致
//法线计算通过两次渲染来得到
//1.get highmap
//2.generate the normal from highmap 

// mesh生成： 
// 1.将通过Lod边界 类型判定
// 2.获取相应的layer 贴图
// 3.根据边界类型生成相应的mesh
public class MapDataGen_Layer : MapDataGen {

}

