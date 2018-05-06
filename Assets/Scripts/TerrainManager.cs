// create by JiepengTan 
// https://github.com/JiepengTan/GPU-Endless-Terrian
// 2018-05-04  email: jiepengtan@gmail.com

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapData {
    public Color[] mapData;
    public MapData(Color[] gpuData) {
        this.mapData = gpuData;
    }
}


public class TerrainManager : MonoBehaviour {

    //public MeshSettings meshSetting;
    TerrianColliderMgr colliderMgr;

    public float coliderViewDist;
    public float coliderRatainDist;
    public int coliderMeshLod;

    public Transform viewer;
    MapDataGen mapGenerator;
    void Start() {
        mapGenerator = FindObjectOfType<MapDataGen>();
        mapGenerator.Init();
        colliderMgr = TerrianColliderMgr.Instance;

        var colMgrTrans = new GameObject("TerrianColliders").transform;
        colMgrTrans.parent = transform;
        colMgrTrans.localPosition = Vector3.zero;

        colliderMgr.mapGenerator = mapGenerator;
        colliderMgr.meshLod = coliderMeshLod;
        colliderMgr.Init(colMgrTrans, viewer, MapDataGen.CHUNK_SIZE, coliderViewDist, coliderRatainDist);


        colliderMgr.CreateChunks();
        //meshMgr.CreateChunks();
    }

    void Update() {
        mapGenerator.OnUpdate();
        colliderMgr.OnUpdate();
        //meshMgr.OnUpdate();
    }
}
