using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Chunk {
    public Vector2i coord;

    public System.Action<Chunk> OnDestoryCallback;
    public System.Action<Chunk> OnStatusChanged;
    public virtual void Release() { }
    public virtual void SetVisible(bool visible) { }
    public virtual void UpdateStatus() { }

    protected void NotifyStatusChanged() {
        if (OnStatusChanged != null) {
            OnStatusChanged(this);
        }
    }
}

public class RegionManager {
    public class ChunkComparer : IEqualityComparer<Chunk> {
        bool IEqualityComparer<Chunk>.Equals(Chunk x, Chunk y) { return x.coord == y.coord; }
        int IEqualityComparer<Chunk>.GetHashCode(Chunk obj) { return obj.coord.GetHashCode(); }
    }
    public static readonly ChunkComparer _Comparer = new ChunkComparer();

    Dictionary<Vector2i, Chunk> chunks = new Dictionary<Vector2i, Chunk>();
    HashSet<Chunk> needUpdateChunk = new HashSet<Chunk>(_Comparer);
    List<Chunk> updateList = new List<Chunk>();

    public int IntMaxViewChunkDist;// 最大可见chunk数量
    public float SqrtMaxViewChunkDist;// 最大可见chunk数量
    public float MaxViewChunkDist;// 最大可见chunk数量
    public float SqrMaxRetainChunkDist;// 最大的保留chunk 

    public float chunkSize;
    public float sqrChunkSize;
    public Transform transform;
    public Transform viewer;
    public Vector2i viewerCoordOld;
    public Vector2 viewerPosOld;
    public void Init(Transform self, Transform viewer, int chunkSize, float MaxViewChunkDist, float MaxRetainChunkDist) {
        transform = self;
        this.viewer = viewer;
        this.chunkSize = chunkSize;
        sqrChunkSize = chunkSize * chunkSize;
        this.IntMaxViewChunkDist = Mathf.FloorToInt(MaxViewChunkDist);
        this.MaxViewChunkDist = MaxViewChunkDist;
        SqrtMaxViewChunkDist = MaxViewChunkDist * MaxViewChunkDist;
        this.SqrMaxRetainChunkDist = MaxRetainChunkDist * MaxRetainChunkDist;
    }

    public void OnUpdate() {
        var curViewerPos = new Vector2(viewer.position.x, viewer.position.z);
        var curViewCoord = new Vector2i(Mathf.RoundToInt(curViewerPos.x / chunkSize),
                                        Mathf.RoundToInt(curViewerPos.y / chunkSize));
        if (viewerCoordOld != curViewCoord
            && Vector2.Distance(curViewerPos, viewerPosOld) > 0.15 * chunkSize) {
            viewerPosOld = curViewerPos;
            viewerCoordOld = curViewCoord;
            UpdateVisibleChunks();
        }
        UpdateChunks();
    }

    // Init from center
    public void CreateChunks() {
        List<Vector2i> initList = new List<Vector2i>();
        for (int yOffset = -IntMaxViewChunkDist; yOffset <= IntMaxViewChunkDist; yOffset++) {
            for (int xOffset = -IntMaxViewChunkDist; xOffset <= IntMaxViewChunkDist; xOffset++) {
                initList.Add(new Vector2i(xOffset, yOffset));
            }
        }
        initList.Sort((a, b) => { return Mathf.FloorToInt(a.sqrMagnitude - b.sqrMagnitude); });
        var curViewerPos = new Vector2(viewer.position.x, viewer.position.z);
        var curViewCoord = new Vector2i(Mathf.RoundToInt(curViewerPos.x / MapDataGen.CHUNK_SIZE), Mathf.RoundToInt(curViewerPos.y / MapDataGen.CHUNK_SIZE));

        for (int i = 0; i < initList.Count; i++) {
            var offset = initList[i];
            Vector2i viewedChunkCoord = curViewCoord + offset;
            if (offset.sqrMagnitude <= SqrtMaxViewChunkDist) {
                UpdateOrCreateChunk(viewedChunkCoord);
            }
        }
    }
    private void UpdateVisibleChunks() {
        var curViewCoord = viewerCoordOld;
        // delete far chunk
        foreach (var item in chunks) {
            var chunk = item.Value;
            if (item.Key == new Vector2i(0, -8)) {
                int i = 10;
                i++;
            }
            chunk.SetVisible(false);
            var dist = chunk.coord - curViewCoord;
            if (dist.sqrMagnitude > SqrMaxRetainChunkDist) {
                chunk.Release();
            }
        }
        for (int yOffset = -IntMaxViewChunkDist; yOffset <= IntMaxViewChunkDist; yOffset++) {
            for (int xOffset = -IntMaxViewChunkDist; xOffset <= IntMaxViewChunkDist; xOffset++) {
                var offset = new Vector2i(xOffset, yOffset);
                Vector2i chunkCoord = curViewCoord + offset;
                if (offset.sqrMagnitude <= SqrtMaxViewChunkDist) {
                    UpdateOrCreateChunk(chunkCoord);
                }
            }
        }
    }

    private void UpdateOrCreateChunk(Vector2i chunkCoord) {
        if (!chunks.ContainsKey(chunkCoord)) {
            var chunk = CreateChunk(viewerCoordOld, chunkCoord, chunkSize);
            chunk.OnDestoryCallback += OnChunkDestroy;
            chunk.OnStatusChanged += OnChunkStatusChanged;
            chunks.Add(chunkCoord, chunk);
        }
        needUpdateChunk.Add(chunks[chunkCoord]);
    }

    // update from center
    private void UpdateChunks() {
        if (needUpdateChunk.Count == 0) return;
        foreach (var item in needUpdateChunk) {
            updateList.Add(item);
        }
        updateList.Sort((a, b) => { return Mathf.FloorToInt((a.coord - viewerCoordOld).sqrMagnitude - (b.coord - viewerCoordOld).sqrMagnitude); });

        foreach (var item in updateList) {
            item.UpdateStatus();
        }
        needUpdateChunk.Clear();
        updateList.Clear();
    }

    void OnChunkStatusChanged(Chunk chunk) {
        needUpdateChunk.Add(chunk);
    }
    void OnChunkDestroy(Chunk chunk) {
        chunk.OnDestoryCallback -= OnChunkDestroy;
        chunk.OnStatusChanged -= OnChunkStatusChanged;
        bool ret = chunks.Remove(chunk.coord);
        Debug.Assert(ret);
    }
    protected virtual Chunk CreateChunk(Vector2i viewerCoord, Vector2i chunkCoord, float chunkSize) {
        return null;
    }

}