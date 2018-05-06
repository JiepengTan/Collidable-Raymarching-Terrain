using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public struct LODInfo {
    public int lod;
    public float visibleDstThreshold;
    public bool useForCollider;
}


class LODMesh {
    public Mesh mesh;
    public bool hasRequestedMesh;
    public bool hasMesh;
    public bool isWaitForData;//是否在等待回调
    int lod;
    MapDataGen mapGenerator;
    System.Action updateCallback;
    public LODMesh(MapDataGen mapGenerator, int lod, System.Action updateCallback) {
        this.lod = lod;
        this.updateCallback = updateCallback;
        this.mapGenerator = mapGenerator;
    }

    void OnMeshDataReceived(MeshTaskInfo info ) {
        MeshData meshData = info.meshData;
        //主线程回调
        isWaitForData = false;
        mesh = meshData.CreateMesh();// TODO pool mesh
        DictionaryPool<MeshData>.Return(meshData);
        hasMesh = true;
        updateCallback();
    }
    public void RequestMesh(MapData mapData) {
        hasRequestedMesh = true;
        isWaitForData = true;
        mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
    }

    public void Release() {
        GameObject.Destroy(mesh);
    }
}
