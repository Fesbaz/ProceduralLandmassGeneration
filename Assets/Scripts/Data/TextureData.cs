using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu]
public class TextureData : UpdatableData {

    public Color[] baseColours;
    [Range(0,1)]
    public float[] baseStartHeights;

    float savedMinHeight;
    float savedMaxHeight;
    public void ApplyToMaterial(Material material) {

        material.SetInt("baseColourCount", baseColours.Length);
        material.SetColorArray("baseColours", baseColours);
        material.SetFloatArray("baseStartHeights", baseStartHeights);

        /*
        "scripts compile before shaders do", so the meshheight values were being reset, so texture shaders weren't updating*/
        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);  // Meshheights were being overridden, so we can at least get them to show with the manual update

    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight) {
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;

        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }
}
