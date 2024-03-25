using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class MeshSettings : UpdatableData { // used to inherit from ScriptableObject, check UpdateableData.cs for info

    public const int numSupportedLODs = 5;
    public const int numSupportedChunkSizes = 9;
    public const int numSupportedFlatshadedChunkSizes = 3;
    public static readonly int[] supportedChunkSizes = {48,72,96,120,144,168,192,216,240};
    
    public float meshScale = 5f; // Scale of Terrain Mesh in X-Z-axis
    public bool useFlatShading;

    [Range(0, numSupportedChunkSizes-1)]
    public int chunkSizeIndex;
    [Range(0, numSupportedFlatshadedChunkSizes-1)]
    public int flatshadedChunkSizeIndex;

    /* 
    num verts per line of mesh rendered at LOD = 0.
    Includes the 2 extra verts that are excluded from final mesh, but used for calculating normals
    Chunk vertices amount. Dimensions of mesh we generate will be 1 less.
    Unity has maximum vertices per mesh of 255^2 ~65k.

    Flatshading generates more vertices, so we have to lower this to 96 (also divisible by all evens up to 12) from 239 to not go over the cap
    */
    public int numVertsPerLine {
        get {
            return supportedChunkSizes[(useFlatShading) ? flatshadedChunkSizeIndex : chunkSizeIndex] + 5;
        }
    }

    public float meshWorldSize{
        get {
            return (numVertsPerLine - 3) * meshScale;
        }
    }
}
