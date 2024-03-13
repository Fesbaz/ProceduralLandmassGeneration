using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour {
    
    public enum DrawMode {NoiseMap, Mesh, FalloffMap}; // Used to draw all the different things without deleting their code
    public DrawMode drawMode;

    public TerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;

    public Material terrainMaterial;

    public bool autoUpdate;
    float [,] falloffMap;


    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();


    void OnValuesUpdated() {
        if (!Application.isPlaying) {
            DrawMapInEditor();
        }
    }

    void OnTextureValuesUpdated() {
        textureData.ApplyToMaterial(terrainMaterial);
    }
    /* 
    about mapChunkSize
    Non-Flatshaded:
    Chunk vertices amount. Dimensions of mesh we generate will be 1 less.
    Unity has maximum vertices per mesh of 255^2 ~65k. 241 - 1 = 240, divisible by 1,2,3,4,5,6,8,10,12,..
    We add 2 to it below, so 239.

    Flatshaded:
    Flatshading generates more vertices, so we have to lower this to 96 (also divisible by all evens up to 12) from 239 to not go over the cap
    */
    public int mapChunkSize {
        get {
            if (terrainData.useFlatShading) { // Cant access instance variable useFlatShading (because static) without saying which instance of MapGenerator we're referring to
                return 95;
            } else {
                return 239;
            }
        }
    }

    [Range(0,6)]
    public int editorPreviewLOD;

    public void DrawMapInEditor() {
        MapData mapData = GenerateMapData(Vector2.zero);

        MapDisplay display = FindObjectOfType<MapDisplay> ();
        if (drawMode == DrawMode.NoiseMap) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        } else if (drawMode == DrawMode.Mesh) {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorPreviewLOD, terrainData.useFlatShading));
        } else if (drawMode == DrawMode.FalloffMap) {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
        }
    }

    public void RequestMapData(Vector2 centre, Action<MapData> callback) {
        ThreadStart threadStart = delegate {
            MapDataThread (centre, callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 centre, Action<MapData> callback) {
        MapData mapData = GenerateMapData(centre);
        lock (mapDataThreadInfoQueue) { // When 1 thread reaches this point, whilst executing this, no other thread can execute it, and has to wait
          mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
        ThreadStart threadStart = delegate {
            MeshDataThread (mapData, lod, callback);
        };

        new Thread (threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, lod, terrainData.useFlatShading);
        lock (meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    void Update() {
        if (mapDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        if (meshDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    // Generation of Map Colors (and height map?) based on noise
    public MapData GenerateMapData(Vector2 centre) {
        // Generate Noise Map with size of mapChunkSize + 2 to generate 1 extra noise value on left, right, top and bottom sides
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, centre + noiseData.offset, noiseData.normalizeMode);

        if (terrainData.useFalloff) {
            if (falloffMap == null) {
                falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);
            }
            // Loop through noise map
            for (int y = 0; y < mapChunkSize+2; y++) {
                for (int x = 0; x < mapChunkSize+2; x++) {
                    if (terrainData.useFalloff) {
                        noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                    }
                }
            }
        }

        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);

        return new MapData(noiseMap);
    }

    // void OnValidate is called when a script value is changed in inspector
    void OnValidate() {

        if (terrainData != null) {
            terrainData.OnValuesUpdated -= OnValuesUpdated; // Before we subscribe again we unsubscribe to keep sub count at 1. If already unsubbed nothing happens
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }
        if (noiseData != null) {
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null) {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
        
    }

    struct MapThreadInfo<T> {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }

}

public struct MapData {
    public readonly float[,] heightMap;

    public MapData (float[,] heightMap) {
        this.heightMap = heightMap;
    }
}