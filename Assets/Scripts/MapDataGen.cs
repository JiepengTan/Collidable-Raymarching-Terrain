#define UNITY_UV_STARTS_AT_TOP //文件是否是上下颠倒

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;


public class MapDataGen : MonoBehaviour {
    public struct TaskResultInfo<T> {
        public readonly Action<T> callback;
        public readonly T parameter;

        public TaskResultInfo(Action<T> callback, T parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }

    public struct MeshTaskInfo {
        public MapData data;
        public int lod;
        public Action<MeshData> callBack;
        public MeshTaskInfo(MapData data, int lod, Action<MeshData> callBack) {
            this.data = data;
            this.lod = lod;
            this.callBack = callBack;
        }
    }
    public struct MapTaskInfo {
        public Vector2 offset;
        public Action<MapData> callBack;
        public MapTaskInfo(Vector2 offset, Action<MapData> callBack) {
            this.offset = offset;
            this.callBack = callBack;
        }
    }


    public Material mat;
    public float _MaxTerrianH;
    public float _PosScale;

    public const int CHUNK_SIZE = 60;
    public const int TexMapRate = 1;//texWid / CHUNK_SIZE   世界1m等于多少像素
    public const int TexWid = MapDataGen.CHUNK_SIZE * TexMapRate + 1;
    public const float WidInShader = (1.0f / TexMapRate) + MapDataGen.CHUNK_SIZE;
    public const float _NorVertDist = (1.0f / TexMapRate);

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    // Editor Test
    public bool isAutoUpdate = false;
    public bool isTestSingleTerrain;
    public Vector2 _Offset = Vector2.zero;

    Queue<MapTaskInfo> mapTasks = new Queue<MapTaskInfo>();
    Queue<TaskResultInfo<MeshData>> meshDataThreadInfoQueue = new Queue<TaskResultInfo<MeshData>>();

    Texture2D mapTex;
    RenderTexture rt;

    void Start() {
        if (!isTestSingleTerrain)
            return;
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

    void Update() {
        // deal map request
        var dt = DateTime.Now;
        while (mapTasks.Count > 0) {
            var task = mapTasks.Dequeue();
            DealMapRequest(task.offset, task.callBack);
            TimeSpan span = DateTime.Now - dt;
            if (span.TotalMilliseconds > 2.0) {//
                break;
            }
        }
        // deal mesh request result
        if (meshDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
                TaskResultInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    public void UpdateInEditor() {
        if (!isTestSingleTerrain) {
            return;
        }
        if (rt == null) {
            Init();
        }
    }



    public void RequestMapData(Vector2 offset, Action<MapData> callBack) {
        mapTasks.Enqueue(new MapTaskInfo(offset, callBack));
    }

    public void DealMapRequest(Vector2 offset, Action<MapData> callBack) {
        mat.SetFloat("_MaxTerrianH", _MaxTerrianH);
        mat.SetFloat("_PosScale", _PosScale);
        mat.SetFloat("_NorVertDist", _NorVertDist);
        mat.SetVector("_WidHigh", new Vector4(WidInShader, WidInShader, 0, 0));
        mat.SetVector("_Offset", offset);

        Graphics.Blit(null, rt, mat, 0);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        mapTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = prev;
        //rt.DiscardContents();//TODO Test
        var data = new MapData(mapTex.GetPixels());
        if (callBack != null) {
            callBack(data);
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
        ThreadPool.QueueUserWorkItem(_info => {
            DealMeshRequest((MeshTaskInfo)_info);
        }, new MeshTaskInfo(mapData, lod, callback));
    }

    void DealMeshRequest(MeshTaskInfo info) {
        var meshData = GenerateTerrainMesh(info.data, TexWid, info.lod);
        lock (meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue(new TaskResultInfo<MeshData>(info.callBack, meshData));
        }
    }


    public static MeshData GenerateTerrainMesh(MapData mapData, int width, int levelOfDetail) {
        var mapInfo = mapData.mapData;
#if UNITY_UV_STARTS_AT_TOP
        int btnLeftX = (width - 1) / -2;
        int btnLeftZ = (width - 1) / -2;
#else
        int topLeftX = (width - 1) / -2;
        int topLeftZ = (width - 1) / 2;
#endif
        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;
        MeshData meshData = DictionaryPool<MeshData>.Get(verticesPerLine);
        int vertexIndex = 0;

        for (int y = 0; y < width; y += meshSimplificationIncrement) {
            for (int x = 0; x < width; x += meshSimplificationIncrement) {
                var info = mapInfo[y * width + x];
#if UNITY_UV_STARTS_AT_TOP
                meshData.vertices[vertexIndex] = new Vector3((btnLeftX + x) / TexMapRate, info.r, (btnLeftZ + y) / TexMapRate);
#else
                meshData.vertices[vertexIndex] = new Vector3((topLeftX + x) / TexMapRate, info.r, (topLeftZ - y)/ TexMapRate);
#endif
                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)width);
                meshData.nors[vertexIndex] = new Vector3(info.g, info.b, info.a);
                vertexIndex++;
            }
        }

        for (int y = 0; y < verticesPerLine - 1; y++) {
            vertexIndex = verticesPerLine * y;
            for (int x = 0; x < verticesPerLine - 1; x++) {
                try {
                    int a00 = vertexIndex;
                    int a01 = vertexIndex + 1;
                    int a10 = vertexIndex + verticesPerLine;
                    int a11 = vertexIndex + verticesPerLine + 1;

#if UNITY_UV_STARTS_AT_TOP
                    meshData.AddTriangle(a00, a10, a11);
                    meshData.AddTriangle(a11, a01, a00);
#else
                    meshData.AddTriangle(a00, a11, a10);
                    meshData.AddTriangle(a11, a00, a01);
#endif
                    vertexIndex++;
                } catch (Exception) {
                    Debug.LogError("vertexIndex " + vertexIndex);
                    throw;
                }
            }
        }
        return meshData;
    }

}
