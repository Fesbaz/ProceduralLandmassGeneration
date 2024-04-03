using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

public static class Noise {

    public enum NormalizeMode {Local, Global};

    // For intro on persistance, lacunarity, etc. https://www.youtube.com/watch?v=wbpMiKiSKm8&list=PLFt_AvWsXl0eBW2EiBtl_sxmDtSgZBxB3&index=2
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCentre) {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        // Pseudo Random Number Generator, each octave sampled from different location
        System.Random prng = new System.Random(settings.seed);
        Vector2[] octaveOffsets = new Vector2[settings.octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < settings.octaves; i++){
            float offsetX = prng.Next(-100000, 100000) + settings.offset.x + sampleCentre.x; // Mathf.PerlinNoise returns same value over values >100k
            float offsetY = prng.Next(-100000, 100000) - settings.offset.y - sampleCentre.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= settings.persistance;
        }

        // For normalization of noise values, need to keep track of min and max values in noise map
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        // TO make noise map zoom to the center when changing scale, otherwise it zooms to the top right
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        // Noise map generation using Unity's PerlinNoise
        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth; x++) {
                
                // initial values
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < settings.octaves; i++) {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / settings.scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / settings.scale * frequency;

                    float perlinValue = Mathf.PerlinNoise (sampleX, sampleY) * 2 - 1; // PerlinNoise normally 0-1, *2-1 makes it between -1-1
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= settings.persistance; // persistance 0-1
                    frequency *= settings.lacunarity; // lacunarity >1
                }

                if (noiseHeight > maxLocalNoiseHeight) {
                    maxLocalNoiseHeight = noiseHeight;
                }
                if (noiseHeight < minLocalNoiseHeight) {
                    minLocalNoiseHeight = noiseHeight;
                }
                noiseMap [x, y] = noiseHeight;

                if (settings.normalizeMode == NormalizeMode.Global) {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight/0.9f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }
        // Normalize noiseMap
        if (settings.normalizeMode == NormalizeMode.Local) {
            for (int y = 0; y < mapHeight; y++) {
                for (int x = 0; x < mapWidth; x++) {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap  [x, y]);
                }
            }
        }

        return noiseMap;
    }
}

[System.Serializable]
public class NoiseSettings {
    public Noise.NormalizeMode normalizeMode; // !!!!!!! README !!!!!!! using local normalize mode causes uneven chunk borders, use Global to fix

    public bool useFalloff = false;
    public float scale = 50;

    public int octaves = 6;
    [Range(0,1)] // This makes persistance 0-1
    public float persistance = 0.5f;
    
    public float lacunarity = 2;

    public int seed;
    public Vector2 offset;

    public void ValidateValues() {
        scale = Mathf.Max(scale, 0.01f);
        octaves = Mathf.Max(octaves, 1);
        lacunarity = Mathf.Max(lacunarity, 1);
        persistance = Mathf.Clamp01(persistance);
    }
}
