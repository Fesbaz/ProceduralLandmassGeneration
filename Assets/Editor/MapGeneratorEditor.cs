using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (MapGenerator))] // Without this, button wont show
public class MapGeneratorEditor : Editor {
    
    public override void OnInspectorGUI() {
        MapGenerator mapGen = (MapGenerator)target; // reference to map generator. target is the object customeditor is inspecting

        if (DrawDefaultInspector()) {   // IF any value changed
            if (mapGen.autoUpdate) {    // and autoUpdate is on, generate map
                mapGen.DrawMapInEditor();
            }
        }

        if (GUILayout.Button("Generate")) {
            mapGen.DrawMapInEditor();
        }
    }
}
