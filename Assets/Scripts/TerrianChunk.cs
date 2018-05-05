using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TerrainChunk {

    GameObject meshObject;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    LODInfo[] detailLevels;
    LODMesh[] lodMeshes;
    LODMesh collisionLODMesh;

    MapData mapData;
    int previousLODIndex = -1;
    public Vector2i coord;

    public System.Action<TerrainChunk> OnDestoryCallback;
    public System.Action<TerrainChunk> OnStatuChanged;

    public bool isWaitMapData;

    public enum EChunkStatus {
        EWaitMapData = 0,
        ELive = 1,
        EWaitToDestory = 2,
        EDestoryed = 3
    };
    public EChunkStatus status = EChunkStatus.EWaitMapData;
    MapDataGen mapGenerator;
    public TerrainChunk(MapDataGen mapGenerator, Vector2i coord, int size, LODInfo[] detailLevels, Transform parent, Material material) {
        this.mapGenerator = mapGenerator;
        this.detailLevels = detailLevels;
        this.coord = coord;


        meshObject = new GameObject("Terrain Chunk " + coord.ToString());
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;

        var position = coord * size;
        Vector3 positionV3 = new Vector3(position.x, 0, position.y);
        meshObject.transform.position = positionV3;
        meshObject.transform.parent = parent;
        meshObject.transform.localScale = Vector3.one;
        SetVisible(false);

        lodMeshes = new LODMesh[detailLevels.Length];
        for (int i = 0; i < detailLevels.Length; i++) {
            lodMeshes[i] = new LODMesh(mapGenerator, detailLevels[i].lod, OnMeshDataReceived);
            if (detailLevels[i].useForCollider) {
                collisionLODMesh = lodMeshes[i];
            }
        }
        isWaitMapData = true;
        mapGenerator.RequestMapData(position, OnMapDataReceived);
    }

    void StatusChanged() {
        if (OnStatuChanged != null) {
            OnStatuChanged(this);
        }
    }

    void OnMapDataReceived(MapData mapData) {
        if (status == EChunkStatus.EWaitMapData) {
            status = EChunkStatus.ELive;
        }
        isWaitMapData = false;
        this.mapData = mapData;
        //meshRenderer.material.mainTexture = texture;
        StatusChanged();
    }
    void OnMeshDataReceived() {
        StatusChanged();
    }

    public void SetVisible(bool visible) {
        try {
            meshObject.SetActive(visible);
        } catch (System.Exception) {
            Debug.LogError(" Missing coord = " + coord);
            throw;
        }
    }

    public bool IsVisible() {
        return meshObject.activeSelf;
    }

    private bool CanDestroy() {
        if (isWaitMapData)
            return false;
        for (int i = 0; i < lodMeshes.Length; i++) {
            if (lodMeshes[i].isWaitForData) {
                return false;
            }
        }
        return true;
    }
    public void UpdateStatus() {
        // check for destroy
        if (status == EChunkStatus.EWaitToDestory) {
            if (CanDestroy()) {
                DestroySelf();
            }
        }
    }
    public void UpdateChunkCollider() {
        if (status != EChunkStatus.ELive) return;

        var diff = coord - EndlessTerrain.viewerCoordOld;
        int maxDist = Mathf.Max(Mathf.Abs(diff.x), Mathf.Abs(diff.y));
        if (maxDist <= EndlessTerrain.maxColliderDist) {//
            if (collisionLODMesh.hasMesh) {
                meshCollider.sharedMesh = collisionLODMesh.mesh;
            } else if (!collisionLODMesh.hasRequestedMesh) {
                collisionLODMesh.RequestMesh(mapData);
            }
        }
    }

    public void UpdateChunkMesh() {
        if (status != EChunkStatus.ELive) return;
        float viewerDstFromNearestEdge = (coord - EndlessTerrain.viewerCoordOld).magnitude;
        bool visible = viewerDstFromNearestEdge <= EndlessTerrain.maxViewDistChunkNum;
        if (visible) {
            int lodIndex = 0;
            // get lod idx
            for (int i = 0; i < detailLevels.Length - 1; i++) {
                if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold) {
                    lodIndex = i + 1;
                } else {
                    break;
                }
            }
            if (lodIndex != previousLODIndex) {
                LODMesh lodMesh = lodMeshes[lodIndex];
                if (lodMesh.hasMesh) {
                    previousLODIndex = lodIndex;
                    meshFilter.mesh = lodMesh.mesh;
                } else if (!lodMesh.hasRequestedMesh) {
                    lodMesh.RequestMesh(mapData);
                }
            }
        }
        SetVisible(visible);
    }
    public void Release() {
        status = EChunkStatus.EWaitToDestory;
        StatusChanged();
    }
    void DestroySelf() {
        status = EChunkStatus.EDestoryed;
        //TODO Pool it
        for (int i = 0; i < lodMeshes.Length; i++) {
            lodMeshes[i].Release();
            lodMeshes[i] = null;
        }
        GameObject.Destroy(meshObject);
        if (OnDestoryCallback != null) {
            OnDestoryCallback(this);
        }
        //TODO mapData
    }
    public override bool Equals(object other) {
        if (!(other is TerrainChunk)) {
            return false;
        }
        return this.coord == ((TerrainChunk)other).coord;
    }

    public bool Equals(TerrainChunk other) {
        return this == other;
    }

    public override int GetHashCode() {
        return coord.GetHashCode();
    }
}
