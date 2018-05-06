using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;

public struct MeshTaskInfo {
    public int lod;
    public Action<MeshTaskInfo> callBack;
    public MeshData meshData;
    public MapData mapData;
    public MeshTaskInfo(MapData data, int lod, Action<MeshTaskInfo> callBack) {
        this.mapData = data;
        this.lod = lod;
        this.callBack = callBack;
        this.meshData = null;
    }
}
public struct MapTaskInfo {
    public Vector2 offset;
    public Action<MapTaskInfo> callBack;
    public MapData mapData;
    public MapTaskInfo(Vector2 offset, Action<MapTaskInfo> callBack) {
        this.offset = offset;
        this.callBack = callBack;
        this.mapData = null;
    }
}

public abstract class MapDataGenBase : MonoBehaviour, IMeshGenStratege {

    public Material mat;
    public float MapXZScale;

    public const int CHUNK_SIZE = 60;
    public const int TexMapRate = 1;//texWid / CHUNK_SIZE   世界1m等于多少像素
    public const int TexWid = MapDataGen.CHUNK_SIZE * TexMapRate + 1;
    public const float WidInShader = (1.0f / TexMapRate) + MapDataGen.CHUNK_SIZE;
    public const float MapEps = (1.0f / TexMapRate);
    public float MaxMapH;


    protected Queue<MapTaskInfo> mapTasks = new Queue<MapTaskInfo>();
    protected Queue<MeshTaskInfo> meshDataThreadInfoQueue = new Queue<MeshTaskInfo>();

    protected Texture2D mapTex;
    protected RenderTexture rt;

    void Start() {
        Init();
        //UpdateMapData(new TaskInfo(new Vector2(0, 0), _lod));
    }

    public void Init() {
        // create RenderTexture
        rt = new RenderTexture(TexWid, TexWid, 0, RenderTextureFormat.ARGBFloat);
        rt.filterMode = FilterMode.Point;
        rt.wrapMode = TextureWrapMode.Clamp;
        // create texture 
        mapTex = new Texture2D(TexWid, TexWid, TextureFormat.RGBAFloat, false);
        mapTex.filterMode = FilterMode.Point;
        mapTex.wrapMode = TextureWrapMode.Clamp;
    }

    public void OnUpdate() {
        // deal map request
        var dt = DateTime.Now;
        while (mapTasks.Count > 0) {
            var task = mapTasks.Dequeue();
            DealMapRequest(task);
            TimeSpan span = DateTime.Now - dt;
            if (span.TotalMilliseconds > 2.0) {//
                break;
            }
        }
        // deal mesh request result
        if (meshDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
                MeshTaskInfo threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callBack(threadInfo);
            }
        }
    }

    public virtual void RequestColliderMapData(Vector2 coord, Action<MapTaskInfo> callBack) {
        mapTasks.Enqueue(new MapTaskInfo(coord * CHUNK_SIZE, callBack));
    }
    public virtual void RequestMapData(Vector2 coord, Action<MapTaskInfo> callBack) {
        mapTasks.Enqueue(new MapTaskInfo(coord * CHUNK_SIZE, callBack));
    }

    public virtual void RequestMeshData(MapData mapData, int lod, Action<MeshTaskInfo> callback) {
        ThreadPool.QueueUserWorkItem(_info => {
            DealMeshRequest((MeshTaskInfo)_info);
        }, new MeshTaskInfo(mapData, lod, callback));
    }



    protected virtual void DealMapRequest(MapTaskInfo info) { }
    protected virtual void DealMeshRequest(MeshTaskInfo info) { }

}

