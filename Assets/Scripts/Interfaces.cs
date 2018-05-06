using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface ILodSiwtchStratege {
    void ReleaseUselessData(Chunk chunk);//卸载不必要的资源  
    int UpdateLodIdx(Chunk chunk);
}

public interface IMeshGenStratege {
    void Init();
    void RequestMapData(Vector2 offset, Action<MapTaskInfo> callBack);
    void RequestMeshData(MapData mapData, int lod, Action<MeshTaskInfo> callback);
}
