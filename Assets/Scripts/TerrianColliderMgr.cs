using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class TerrianColliderMgr : RegionManager {
    public static TerrianColliderMgr _Instance;
    public static TerrianColliderMgr Instance { get { if (_Instance == null) _Instance = new TerrianColliderMgr(); return _Instance; } }
    public int meshLod;
    public MapDataGen mapGenerator;
    TerrianColliderMgr(){}
    protected override Chunk CreateChunk(Vector2i viewerCoord, Vector2i coord, float chunkSize) {
        var pos = MapDataGen.CHUNK_SIZE * coord;
        var chunk = new TerrianColliderChunk(meshLod,mapGenerator, transform, coord, new Vector3(pos.x, 0, pos.y));
        return chunk;
    }

    public static bool IsVisibleFormViewer(Vector2i coord) {
        var diff = coord - Instance.viewerCoordOld;
        int chunkDist = Mathf.Max(Mathf.Abs(diff.x), Mathf.Abs(diff.y));
         return chunkDist <= Instance.IntMaxViewChunkDist;
    }


    public class TerrianColliderChunk : MeshChunk {
        public GameObject meshObject;
        public MeshCollider meshCollider;
        LODMesh colliderMesh;
        public TerrianColliderChunk(int meshLod,MapDataGen mapGenerator, Transform parent, Vector2i coord, Vector3 pos) {
            this.coord = coord;
            meshObject = new GameObject("Collider Chunk " + coord.ToString());
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshObject.transform.position = pos;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one;
            isWaitMapData = true;
            colliderMesh = new LODMesh(mapGenerator, meshLod, OnMeshDataReceived);

            mapGenerator.RequestColliderMapData(coord, OnMapDataReceived);

            //TODO request mesh
        }
        void OnMeshDataReceived() {
            NotifyStatusChanged();
        }

        public override void SetVisible(bool visible) { }
        protected override void OnUpdateStatus() {
            bool visible = TerrianColliderMgr.IsVisibleFormViewer(coord);
            if (visible) {
                if (colliderMesh.hasMesh) {
                    meshCollider.sharedMesh = colliderMesh.mesh;
                } else if (!colliderMesh.hasRequestedMesh) {
                    colliderMesh.RequestMesh(mapData);
                }
            }
        }
        protected override bool CanDestroy() {
            return !isWaitMapData && !colliderMesh.isWaitForData;
        }

        protected override void OnDestory() {
            colliderMesh.Release();
            colliderMesh = null;
            GameObject.Destroy(meshObject);
        }
    }
}