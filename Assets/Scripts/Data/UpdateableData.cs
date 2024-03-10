using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Changing Noise/Terrain values in editor wouldnt automatically show the results, youd have to click generate every time, which was annoying
// So now we use this Script to do that
public class UpdatableData : ScriptableObject { 
    
    public event System.Action OnValuesUpdated;  
    public bool autoUpdate;

    // Called when values change in inspector, scripts compile and others
    protected virtual void OnValidate() {
        UnityEditor.EditorApplication.delayCall += _OnValidate;
    }

    protected virtual void _OnValidate() {
        if (autoUpdate) { // I dont see why anyone would NOT want to see the changes automatically, but ye its a bool
            NotifyOfUpdatedValues();
        }
    }

    public void NotifyOfUpdatedValues() {
        if (OnValuesUpdated != null) {
            OnValuesUpdated();
        }
    }
}
