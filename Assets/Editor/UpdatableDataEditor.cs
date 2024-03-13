using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UpdatableData), true)]
public class UpdateableDataEditor : Editor {
    
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        UpdatableData data = (UpdatableData)target;

        if (GUILayout.Button("Update")) {
            data.NotifyOfUpdatedValues();
            EditorUtility.SetDirty(target); // notify that something has changed, fixes update not doing anything after saving
        }
    }
}
