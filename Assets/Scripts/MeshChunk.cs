using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class MeshChunk : Chunk {
    public enum EChunkStatus {
        EWaitMapData = 0,
        ELive = 1,
        EWaitToDestory = 2,
        EDestoryed = 3
    };
    public bool isWaitMapData;
    protected MapData mapData;
    protected EChunkStatus status = EChunkStatus.EWaitMapData;

    protected void OnMapDataReceived(MapTaskInfo info) {
        if (status == EChunkStatus.EWaitMapData) {
            status = EChunkStatus.ELive;
        }
        isWaitMapData = false;
        mapData = info.mapData;
        NotifyStatusChanged();
    }

    public override void Release() {
        status = EChunkStatus.EWaitToDestory;
        NotifyStatusChanged();
    }
    public override void UpdateStatus() {
        if (status == EChunkStatus.EWaitToDestory) {
            if (CanDestroy()) {
                DestroySelf();
                return;
            }
        }
        if (status != EChunkStatus.ELive) return;
        OnUpdateStatus();
    }
    protected void DestroySelf() {
        status = EChunkStatus.EDestoryed;
        //TODO Pool it
        OnDestory();
        if (OnDestoryCallback != null) {
            OnDestoryCallback(this);
        }
        //TODO mapData
    }
    protected virtual void OnUpdateStatus() { }
    protected virtual void OnDestory() { }
    protected virtual bool CanDestroy() {
        return !isWaitMapData;
    }
}