using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Take noise map, turn into texture and apply to a plane in scene
public class MapDisplay : MonoBehaviour {
    
     public Renderer textureRender;
     public MeshFilter meshFilter;
     public MeshRenderer meshRenderer;

     public void DrawTexture(Texture2D texture) {
          // Apply texture to texture renderer
          textureRender.sharedMaterial.mainTexture = texture; // Preview map inside editor. Can't use textureRender.material because instantiated at runtime
          textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height); // Nice to have size of plane same as size of texture map
     }

     public void DrawMesh(MeshData meshData, Texture2D texture) {
          meshFilter.sharedMesh = meshData.CreateMesh(); // Has to be shared because we might be generating the mesh outside of gamemode 
          meshRenderer.sharedMaterial.mainTexture = texture;
     }

}
