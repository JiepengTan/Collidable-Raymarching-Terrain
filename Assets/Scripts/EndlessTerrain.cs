using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * //TODO
 * MaxDistance MD=1000.  ChunkSize = 50 (CS)
 * 越近的Mesh LOD 越小(1 2 3 4 5 6)
 * Near Distance 2CS  
 * Mid Distance 2CS~6CS
 * Far Distance 6CS~
 * 策略：
 * (1) 初始化：
 * 1.D< 50 Collider生成
 * 玩家一旦移动超过0.25*ChunkSize  update nearest Collider immediately 
 * 2.from near to mid  Lod3 mesh生成
 * 3.near Lod2生成
 * 4.all lod3生成
 * 5.near lod1 生成
 * 6.from near to mid lod2 生成
 * 7.gen 2CS 所有的collider
 * 
 * LOD 越小 MeshData的更新优先级越高
 * 
 * (2) Mapdata 丢弃
 * . D<4CS的保留
 * . D<8CS的保留到第二网格生成
 * . 其他的生成后直接抛弃 
 * 
 * (3) Mesh 生成时机
 * . D<3CS的时候就生成Mesh1
 * . D<7CS的时候就生成Mesh2
 * 
 * (4) Mesh 抛弃
 * . D>3CS的时候抛弃第1网格
 * . D>8CS的时候抛弃2rd网格
 * . D>MD+4CS Max Distance全部抛弃
 * 
 * (4) Collider 网格抛弃
 * . D>5CS的全部抛弃
 */

// 只在Viewer 的移动大于10的时候才开始检测所有的chunk LOD的更新
// 只在Viewer 的移动大于15的时候才开始检测Mesh的更新



[System.Serializable]
public class MapUpdateInfo : ScriptableObject {
    public int lod;
    public float viewDst;//LOD 切换距离
    public float mapDataKeepDst;//保留map data的距离
    public float genMeshDst;//网格开始生成距离
    public float delMeshDst;//网格开始删除的距离
    public float delColDst;//
}

public class MapData {
    public Color[] mapData;
    public MapData(Color[] gpuData) {
        this.mapData = gpuData;
    }
}


public class EndlessTerrain : MonoBehaviour {

    const float scale = 2.5f;

    const float viewerMoveThresholdForChunkUpdate = MapDataGen.CHUNK_SIZE;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public LODInfo[] detailLevels;
    public static int maxViewDistChunkNum = 8;
    public static int maxRatainDist = 10;
    public static int maxColliderDist = 1;//最大的碰撞mesh 更新距离

    public Transform viewer;
    public Material mapMaterial;

    static MapDataGen mapGenerator;

    Dictionary<Vector2i, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2i, TerrainChunk>();

    public static Vector2i viewerCoordOld;
    public static Vector2 viewerPosOld;

    public static HashSet<TerrainChunk> needUpdateChunk = new HashSet<TerrainChunk>();
    List<TerrainChunk> updateList = new List<TerrainChunk>();

    void Start() {
        maxViewDistChunkNum = Mathf.FloorToInt(detailLevels[detailLevels.Length - 1].visibleDstThreshold);
        maxRatainDist = maxViewDistChunkNum + 2;

        mapGenerator = FindObjectOfType<MapDataGen>();
        mapGenerator.Init();
        InitChunks();
    }

    // Init from center
    void InitChunks() {

        List<Vector2i> initList = new List<Vector2i>();
        for (int yOffset = -maxViewDistChunkNum; yOffset <= maxViewDistChunkNum; yOffset++) {
            for (int xOffset = -maxViewDistChunkNum; xOffset <= maxViewDistChunkNum; xOffset++) {
                initList.Add(new Vector2i(xOffset, yOffset));
            }
        }
        initList.Sort((a, b) => { return Mathf.FloorToInt(a.sqrMagnitude - b.sqrMagnitude); });
        var curViewerPos = new Vector2(viewer.position.x, viewer.position.z);
        var curViewCoord = new Vector2i(Mathf.RoundToInt(curViewerPos.x / MapDataGen.CHUNK_SIZE), Mathf.RoundToInt(curViewerPos.y / MapDataGen.CHUNK_SIZE));

        for (int i = 0; i < initList.Count; i++) {
            var offset = initList[i];
            Vector2i viewedChunkCoord = curViewCoord + offset;
            UpdateOrCreateChunk(viewedChunkCoord);
        }
    }

    void Update() {
        var curViewerPos = new Vector2(viewer.position.x, viewer.position.z);
        var curViewCoord = new Vector2i(Mathf.RoundToInt(curViewerPos.x / MapDataGen.CHUNK_SIZE), Mathf.RoundToInt(curViewerPos.y / MapDataGen.CHUNK_SIZE));
        if (viewerCoordOld != curViewCoord //格子变化时候才变动
            && Vector2.Distance(curViewerPos, viewerPosOld) > 0.15 * MapDataGen.CHUNK_SIZE) //避免来回的切换
        {
            viewerPosOld = curViewerPos;
            viewerCoordOld = curViewCoord;
            UpdateVisibleChunks();
        }
        UpdateChunks();
    }


    void UpdateVisibleChunks() {
        var curViewCoord = new Vector2i(Mathf.RoundToInt(viewerPosOld.x / MapDataGen.CHUNK_SIZE), Mathf.RoundToInt(viewerPosOld.y / MapDataGen.CHUNK_SIZE));

        // 删除不再需要的Terrain
        foreach (var item in terrainChunkDictionary) {
            var chunk = item.Value;
            chunk.SetVisible(false);
            var dist = chunk.coord - curViewCoord;
            if (Mathf.Max(Mathf.Abs(dist.x), Mathf.Abs(dist.y)) > maxRatainDist) {
                chunk.Release();
            }
        }
        for (int yOffset = -maxViewDistChunkNum; yOffset <= maxViewDistChunkNum; yOffset++) {
            for (int xOffset = -maxViewDistChunkNum; xOffset <= maxViewDistChunkNum; xOffset++) {
                Vector2i viewedChunkCoord = curViewCoord + new Vector2i(xOffset, yOffset);
                UpdateOrCreateChunk(viewedChunkCoord);
            }
        }
    }

    private void UpdateOrCreateChunk(Vector2i viewedChunkCoord) {
        if (!terrainChunkDictionary.ContainsKey(viewedChunkCoord)) {
            var chunk = new TerrainChunk(mapGenerator, viewedChunkCoord, MapDataGen.CHUNK_SIZE, detailLevels, transform, mapMaterial);
            chunk.OnDestoryCallback += OnChunkDestroy;
            chunk.OnStatuChanged += OnChunkStatusChanged;
            terrainChunkDictionary.Add(viewedChunkCoord, chunk);
        }
        needUpdateChunk.Add(terrainChunkDictionary[viewedChunkCoord]);
    }
    void OnChunkStatusChanged(TerrainChunk chunk) {
        needUpdateChunk.Add(chunk);
    }
    private void UpdateChunks() {
        if (needUpdateChunk.Count == 0) return;
        foreach (var item in needUpdateChunk) {
            updateList.Add(item);
        }
        //从中心开始更新
        updateList.Sort((a, b) => { return Mathf.FloorToInt((a.coord - viewerCoordOld).sqrMagnitude - (b.coord - viewerCoordOld).sqrMagnitude); });
        // collider first
        int count = updateList.Count;
        for (int i = 0; i < count; i++) {
            updateList[i].UpdateChunkCollider();
        }
        for (int i = 0; i < count; i++) {
            updateList[i].UpdateChunkMesh();
        }
        for (int i = 0; i < count; i++) {
            updateList[i].UpdateStatus();
        }
        needUpdateChunk.Clear();
        updateList.Clear();
    }

    void OnChunkDestroy(TerrainChunk chunk) {
        chunk.OnDestoryCallback -= OnChunkDestroy;
        chunk.OnStatuChanged -= OnChunkStatusChanged;
        bool ret = terrainChunkDictionary.Remove(chunk.coord);
        Debug.Assert(ret);
    }
}
