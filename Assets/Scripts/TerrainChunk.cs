using UnityEngine;

public class TerrainChunk {
        const float colliderGenerationDistanceThreshold = 5; // How close player has to be to edge of chunk to generate its collider
        public event System.Action<TerrainChunk, bool> onVisibilityChanged;

        public Vector2 coord;
        
        Vector2 sampleCentre;
        GameObject meshObject;
        Bounds bounds; // Bounds used to find point on the perimeter closest to another point

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        // For collision
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        int colliderLODIndex;

        HeightMap heightMap;
        bool heightMapReceived;
        int previousLODIndex = -1;
        bool hasSetCollider;
        float maxViewDst;

        HeightMapSettings heightMapSettings;
        MeshSettings meshSettings;
        Transform viewer;

        public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material) {
            this.coord = coord;
            this.detailLevels = detailLevels;
            this.colliderLODIndex = colliderLODIndex;
            this.heightMapSettings = heightMapSettings;
            this.meshSettings = meshSettings;
            this.viewer = viewer;

            sampleCentre = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
            Vector2 position = coord * meshSettings.meshWorldSize;
            bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>(); 
            meshFilter = meshObject.AddComponent<MeshFilter>(); // AddComponent returns component it adds, so we can do this assignment
            meshCollider = meshObject.AddComponent<MeshCollider>(); // gives collision
            meshRenderer.material = material;

            meshObject.transform.position = new Vector3(position.x, 0, position.y);
            meshObject.transform.parent = parent;
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++) {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod);
                lodMeshes[i].updateCallback += UpdateTerrainChunk;
                if (i == colliderLODIndex) {
                    lodMeshes[i].updateCallback += UpdateCollisionMesh;
                }
            }

            maxViewDst = detailLevels[detailLevels.Length-1].visibleDstThreshold;

            ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, sampleCentre), OnHeightMapReceived);
        }

        public void Load() {
            ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, sampleCentre), OnHeightMapReceived);
        
        }
        
        // Receive heightMap, use it to later generate mesh data
        void OnHeightMapReceived(object heightMapObject) {
            this.heightMap = (HeightMap)heightMapObject;
            heightMapReceived = true;

            UpdateTerrainChunk();
        }

        Vector2 viewerPosition {
            get {
                return new Vector2(viewer.position.x, viewer.position.z);
            }
        }

        // When a chunk updates itself
        public void UpdateTerrainChunk() {
            if (heightMapReceived) {
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
                            lodMesh.RequestMesh(heightMap, meshSettings);
                        }
                    }
                }

                // If chunk visibility changed
                if (wasVisible != visible) {
                    SetVisible(visible);
                    if (onVisibilityChanged != null) {
                        onVisibilityChanged(this, visible);
                    }
                }
            }
        }

        public void UpdateCollisionMesh() {
            if (!hasSetCollider) {
                float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

                if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDstThreshold) {
                    if (!lodMeshes[colliderLODIndex].hasRequestedMesh) {
                        lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);
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

        void OnMeshDataReceived(object meshDataObject) {
            mesh = ((MeshData)meshDataObject).CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings) {
            hasRequestedMesh = true;
            ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod), OnMeshDataReceived);
        }

    }