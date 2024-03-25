using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (MapPreview))] // Without this, button wont show
public class MapPreviewEditor : Editor {
    
    public override void OnInspectorGUI() {
        MapPreview mapPreview = (MapPreview)target; // reference to map generator. target is the object customeditor is inspecting

        if (DrawDefaultInspector()) {   // IF any value changed
            if (mapPreview.autoUpdate) {    // and autoUpdate is on, generate map
                mapPreview.DrawMapInEditor();
            }
        }

        if (GUILayout.Button("Generate")) {
            mapPreview.DrawMapInEditor();
        }
    }
}
