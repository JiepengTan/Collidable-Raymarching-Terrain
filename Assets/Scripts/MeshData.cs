using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


public class MeshData : IPoolElem {
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;
    public Vector3[] nors;

    int wid = 0;
    int triangleIndex = 0;
    int lod;
    public MeshData() { }
    public int HashVal { get { return wid; } }
    public void OnRecycle() {
        triangleIndex = 0;
    }
    public void OnInit(int _wid) {
        wid = _wid;
        if (vertices != null)
            return;
        vertices = new Vector3[wid * wid];
        uvs = new Vector2[wid * wid];
        nors = new Vector3[wid * wid];

        triangles = new int[(wid - 1) * (wid - 1) * 6];
    }
    public void AddTriangle(int a, int b, int c) {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = nors;
        if (wid < MapDataGen.CHUNK_SIZE * 0.7) {
            mesh.RecalculateNormals();
        }
        return mesh;
    }
}
