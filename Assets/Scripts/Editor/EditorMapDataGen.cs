using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(MapDataGen))]
public class MapDataGenEditor : Editor {

    public override void OnInspectorGUI() {
        MapDataGen mapGen = (MapDataGen)target;

        if (DrawDefaultInspector()) {
            if (mapGen.isAutoUpdate) {
                mapGen.UpdateInEditor();
            }
        }

        if (GUILayout.Button("Refresh")) {
            mapGen.UpdateInEditor();
        }
    }
}
