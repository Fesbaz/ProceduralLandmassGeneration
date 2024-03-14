using System;
using System.Collections;
using System.Collections.Generic; // to get our hands on Dictionary
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {

    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate; // Getting square distance is always faster than getting actual distnace when comparing distances
    const float colliderGenerationDistanceThreshold = 5; // How close player has to be to edge of chunk to generate its collider

    public int colliderLODIndex;
    public LODInfo[] detailLevels;
    public static float maxViewDst;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> visibleChunks = new List<TerrainChunk>();

    void Start() {
        mapGenerator = FindAnyObjectByType<MapGenerator>();

        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        chunkSize = mapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);

        UpdateVisibleChunks();
    }

    void Update() {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / mapGenerator.terrainData.uniformScale;

        if (viewerPosition != viewerPositionOld) {
            foreach (TerrainChunk chunk in visibleChunks) {
                chunk.UpdateCollisionMesh();
            }
        }

        if ((viewerPositionOld-viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    // Updating all visible chunks in next frame
    void UpdateVisibleChunks() {
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
        // Hide chunks out of vision by deactivating all visible chunks last frame
        for (int i = visibleChunks.Count - 1; i >= 0; i--) {
            alreadyUpdatedChunkCoords.Add(visibleChunks[i].coord);
            visibleChunks[i].UpdateTerrainChunk(); // updating a chunk might remove itself from the list, leading to index errors, so we start at end of list
        }

        // Get coordinates of chunk viewer is standing on and update chunks around player
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x/chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y/chunkSize);

        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord)) {
                    if (terrainChunkDictionary.ContainsKey(viewedChunkCoord)) {
                        terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    } else { // If not, instantiate new terrain chunk
                        terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, colliderLODIndex, transform, mapMaterial));
                    }
                }
            }
        }
    }

    public class TerrainChunk {

        public Vector2 coord;
        
        Vector2 position;
        GameObject meshObject;
        Bounds bounds; // Bounds used to find point on the perimeter closest to another point

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        // For collision
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        int colliderLODIndex;

        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;
        bool hasSetCollider;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Material material) {
            this.coord = coord;
            this.detailLevels = detailLevels;
            this.colliderLODIndex = colliderLODIndex;

            position = coord * size;
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);
            bounds = new Bounds(position, Vector2.one * size);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>(); 
            meshFilter = meshObject.AddComponent<MeshFilter>(); // AddComponent returns component it adds, so we can do this assignment
            meshCollider = meshObject.AddComponent<MeshCollider>(); // gives collision
            meshRenderer.material = material;

            meshObject.transform.position = positionV3 * mapGenerator.terrainData.uniformScale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++) {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod);
                lodMeshes[i].updateCallback += UpdateTerrainChunk;
                if (i == colliderLODIndex) {
                    lodMeshes[i].updateCallback += UpdateCollisionMesh;
                }
            }

            mapGenerator.RequestMapData(position,OnMapDataReceived); 
        }
        
        // Receive mapData, use it to later generate mesh data
        void OnMapDataReceived(MapData mapData) {
            this.mapData = mapData;
            mapDataReceived = true;

            UpdateTerrainChunk();
        }

        // When a chunk updates itself
        public void UpdateTerrainChunk() {
            if (mapDataReceived) {
                float viewDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

                bool wasVisible = isVisible();
                bool visible = viewDstFromNearestEdge <= maxViewDst;

                // if chunk finds itself visible then add it to the terrainChunksVisibleLastUpdate list
                if (visible) {
                    int lodIndex = 0;

                    for (int i = 0; i < detailLevels.Length - 1; i++) {
                        if (viewDstFromNearestEdge > detailLevels[i].visibleDstThreshold) {
                            lodIndex = i + 1;
                        } else {
                            break;
                        }
                    }

                    if (lodIndex != previousLODIndex) {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh) {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh) {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                }

                // If chunk visibility changed
                if (wasVisible != visible) {
                    if (visible) {
                        visibleChunks.Add(this);
                    } else {
                        visibleChunks.Remove(this);
                    }
                    SetVisible(visible);
                }
            }
        }

        public void UpdateCollisionMesh() {
            if (!hasSetCollider) {
                float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

                if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDstThreshold) {
                    if (!lodMeshes[colliderLODIndex].hasRequestedMesh) {
                        lodMeshes[colliderLODIndex].RequestMesh(mapData);
                    }
                }

                if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold) {
                    if (lodMeshes[colliderLODIndex].hasMesh) {
                        meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                        hasSetCollider = true;
                    }
                }
            }
        }

        public void SetVisible(bool visible) {
            meshObject.SetActive(visible);
        }

        public bool isVisible() {
            return meshObject.activeSelf;
        }

    }

    class LODMesh {
        
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        public event System.Action updateCallback;

        public LODMesh(int lod) {
            this.lod = lod;
        }

        void OnMeshDataReceived(MeshData meshData) {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData) {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }

    }

    [System.Serializable]
    public struct LODInfo {
        [Range(0, MeshGenerator.numSupportedLODs-1)]
        public int lod;
        public float visibleDstThreshold;

        public float sqrVisibleDstThreshold {
            get {
                return visibleDstThreshold * visibleDstThreshold;
            }
        }
    }
}
