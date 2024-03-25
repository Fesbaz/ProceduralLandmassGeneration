using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

[CreateAssetMenu()]
public class HeightMapSettings : UpdatableData { // used to inherit from ScriptableObject, check UpdateableData.cs for info

    public NoiseSettings noiseSettings;
    public float heightMultiplier; // Scale in Y-axis
    public AnimationCurve heightCurve;

    public float minHeight {
        get {
            return heightMultiplier * heightCurve.Evaluate(0);
        }
    }

    public float maxHeight {
        get {
            return heightMultiplier * heightCurve.Evaluate(1);
        }
    }

    // void OnValidate is called when a script value is changed in inspector
    // Here we use it to clamp some of the values

    #if UNITY_EDITOR

    protected override void OnValidate() {
        noiseSettings.ValidateValues();
        base.OnValidate();
    }

    #endif

}
