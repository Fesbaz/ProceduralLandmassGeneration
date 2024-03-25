using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Take noise map, turn into texture and apply to a plane in scene
public class MapPreview : MonoBehaviour {
    
     public Renderer textureRender;
     public MeshFilter meshFilter;
     public MeshRenderer meshRenderer;

     public enum DrawMode {NoiseMap, Mesh, FalloffMap}; // Used to draw all the different things without deleting their code
     public DrawMode drawMode;

     public MeshSettings meshSettings;
     public HeightMapSettings heightMapSettings;
     public TextureData textureData;

     public Material terrainMaterial;


     [Range(0,MeshSettings.numSupportedLODs-1)]
     public int editorPreviewLOD;
     public bool autoUpdate;


    public void DrawMapInEditor() {
     textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero);

        if (drawMode == DrawMode.NoiseMap) {
          DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
        } else if (drawMode == DrawMode.Mesh) {
          DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorPreviewLOD));
        } else if (drawMode == DrawMode.FalloffMap) {
          DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVertsPerLine), 0 , 1)));
        }
     }

     public void DrawTexture(Texture2D texture) {
          // Apply texture to texture renderer
          textureRender.sharedMaterial.mainTexture = texture; // Preview map inside editor. Can't use textureRender.material because instantiated at runtime
          textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f; // Nice to have size of plane same as size of texture map

          textureRender.gameObject.SetActive(true);
          meshFilter.gameObject.SetActive(false);

     }

     public void DrawMesh(MeshData meshData) {
          meshFilter.sharedMesh = meshData.CreateMesh(); // Has to be shared because we might be generating the mesh outside of gamemode
          
          textureRender.gameObject.SetActive(false);
          meshFilter.gameObject.SetActive(true);
     }

     void OnValuesUpdated() {
        if (!Application.isPlaying) {
            DrawMapInEditor();
        }
     }

    void OnTextureValuesUpdated() {
        textureData.ApplyToMaterial(terrainMaterial);
     }

    // void OnValidate is called when a script value is changed in inspector
    void OnValidate() {

        if (meshSettings != null) {
            meshSettings.OnValuesUpdated -= OnValuesUpdated; // Before we subscribe again we unsubscribe to keep sub count at 1. If already unsubbed nothing happens
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (heightMapSettings != null) {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null) {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
     }
}
