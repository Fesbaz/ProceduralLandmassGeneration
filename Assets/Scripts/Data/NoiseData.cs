using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdatableData { // used to inherit from ScriptableObject, check UpdateableData.cs for info
    
    public Noise.NormalizeMode normalizeMode; // !!!!!!! README !!!!!!! using local normalize mode causes uneven chunk borders, use Global to fix

    public float noiseScale;

    public int octaves;
    [Range(0,1)] // This makes persistance 0-1
    public float persistance;
    
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    // void OnValidate is called when a script value is changed in inspector
    // Here we use it to clamp some of the values

    #if UNITY_EDITOR

    protected override void OnValidate() {
        if (lacunarity < 1) {
            lacunarity = 1;
        }
        if (octaves < 0) {
            octaves = 0;
        }

        base.OnValidate();
    }

    #endif

}
