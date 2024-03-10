using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class TerrainData : UpdatableData { // used to inherit from ScriptableObject, check UpdateableData.cs for info
    
    public bool useFlatShading;
    public bool useFalloff;

    public float uniformScale = 5f; // Scale of Terrain Mesh in X-Z-axis
    public float meshHeightMultiplier; // Scale in Y-axis
    public AnimationCurve meshHeightCurve;
}
