#define UNITY_UV_STARTS_AT_TOP //文件是否是上下颠倒

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;

public class MapDataGen : MapDataGenBase {

    protected override void DealMapRequest(MapTaskInfo info) {
        mat.SetFloat("_MaxTerrianH", MaxMapH);
        mat.SetFloat("_PosScale", MapXZScale);
        mat.SetFloat("_Eps", MapEps);
        mat.SetVector("_WidHigh", new Vector4(WidInShader, WidInShader, 0, 0));
        mat.SetVector("_Offset", info.offset);

        Graphics.Blit(null, rt, mat, 0);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        mapTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = prev;
        //rt.DiscardContents();//TODO Test
        info.mapData = new MapData(mapTex.GetPixels());
        if (info.callBack != null) {
            info.callBack(info);
        }
    }


    protected override void DealMeshRequest(MeshTaskInfo info) {
        info.meshData = GenerateTerrainMesh(info.mapData, TexWid, info.lod);
        lock (meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue(info);
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
