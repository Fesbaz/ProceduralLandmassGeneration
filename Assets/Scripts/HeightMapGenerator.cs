using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator {
    
    public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCentre) {
        float[,] values = Noise.GenerateNoiseMap(width, height, settings.noiseSettings, sampleCentre);
        
        float[,] falloffMap = new float[width, height];

        if (settings.noiseSettings.useFalloff) {
            falloffMap = FalloffGenerator.GenerateFalloffMap(width);
        }

        // heightCurve is used to make height map not affect water so much, configured in inspector
        // threading causes heightCurve to return wrong values, this fixes it
        AnimationCurve heightCurve_threadsafe = new AnimationCurve(settings.heightCurve.keys); // accessing GenerateHeightMap simultaneously from different threads has issues with animationcurve

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;
        
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (settings.noiseSettings.useFalloff) {
                    values[i,j] = values[i,j] - falloffMap[i,j];
                }
                values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j]) * settings.heightMultiplier;

                if (values[i, j] > maxValue) {
                    maxValue = values[i, j];
                }
                if (values[i, j] < minValue) {
                    minValue = values[i, j];
                }
            }
        }

        return new HeightMap(values, minValue, maxValue);
    }
    
}

public struct HeightMap {
    public readonly float[,] values;
    public readonly float minValue;
    public readonly float maxValue;

    public HeightMap (float[,] values, float minValue, float maxValue) {
        this.values = values;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
}