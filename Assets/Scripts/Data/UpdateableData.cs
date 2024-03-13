using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Changing Noise/Terrain values in editor wouldnt automatically show the results, youd have to click generate every time, which was annoying
// So now we use this Script to do that
public class UpdatableData : ScriptableObject { 
    
    public event System.Action OnValuesUpdated;  
    public bool autoUpdate;

    
    // OnValidate is called when values change in inspector, scripts compile, among others
    protected virtual void OnValidate() {
        if (autoUpdate) { // I dont see why anyone would NOT want to see the changes automatically, but ye its a bool
            UnityEditor.EditorApplication.update += NotifyOfUpdatedValues; 
        }
    }

    public void NotifyOfUpdatedValues() {
        // We dont want this to be called every frame after script compilation, so we unsub once this method is called
        UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
        if (OnValuesUpdated != null) {
            OnValuesUpdated();
        }
    }
}
