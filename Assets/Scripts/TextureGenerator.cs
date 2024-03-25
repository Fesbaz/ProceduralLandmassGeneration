using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class TextureGenerator {
    
    public static Texture2D TextureFromColorMap(Color[] colourMap, int width, int height) {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point; // Fix blurriness, default .Bilinear
        texture.wrapMode = TextureWrapMode.Clamp; // Fix Wrapping, other side of map seeping through to other side
        texture.SetPixels(colourMap);
        texture.Apply();
        return texture;
    }

    public static Texture2D TextureFromHeightMap(HeightMap heightMap) {
        // Dimensions
        int width = heightMap.values.GetLength(0);
        int height = heightMap.values.GetLength(1);

        // Color
        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                // ! colourMap is 1D, whereas noiseMap is 2D !
                // Row we're on is y * width, column is x
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, Mathf.InverseLerp(heightMap.minValue, heightMap.maxValue, heightMap.values [x, y]));
            }
        }

        return TextureFromColorMap (colourMap, width, height);
    }

}
