using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// What this do: Generating flat plain and setting height of vertices
// If youre reading this to find solution for UNEVEN CHUNK BORDERS, make sure youre using Global normalize mode in Default Noise.asset in Terrain Assets
// Borders are uneven between chucks of different LODs (use lower vertice count meshes) causing seams/clipping/uneven ground you see through
// Fix is to increase render distance until you wont notice it
public static class MeshGenerator {
    
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail, bool useFlatShading) {
        // heightCurve is used to make height map not affect water so much, configured in inspector
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys); // threading causes heightCurve to return wrong values, this fixes it

        int meshSimplificationIncrement = (levelOfDetail == 0)?1:levelOfDetail * 2; // If LOD 0, set to 1, otherwise * 2. ? = if
        
        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

        // To get the mesh centered at 0,0 we need to start from top left
        float topLeftX = (meshSizeUnsimplified - 1)/ -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        int verticesPerLine = (meshSize - 1)/meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, useFlatShading);
        
        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        for (int y = 0; y < borderedSize; y+= meshSimplificationIncrement) {
            for (int x = 0; x < borderedSize; x+= meshSimplificationIncrement) {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;

                if (isBorderVertex) {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < borderedSize; y+= meshSimplificationIncrement) {
            for (int x = 0; x < borderedSize; x+= meshSimplificationIncrement) {
                Vector2 percent = new Vector2((x-meshSimplificationIncrement) / (float)meshSize, (y-meshSimplificationIncrement) / (float)meshSize);
                float height = heightCurve.Evaluate(heightMap[x,y]) * heightMultiplier;
                int vertexIndex = vertexIndicesMap[x, y];
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);

                meshData.AddVertex(vertexPosition, percent, vertexIndex);

                if (x < borderedSize - 1 && y < borderedSize - 1) {
                    /*
                             A (x, y)
                                *-------* B (x+i, y)
                                |       |
                                |       |
                                |       |
                                *-------*
                            C (x, y+i)   D (x+i, y+i)
                    */
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];

                    // These 2 lines create the 2 triangles of the square
                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }

                vertexIndex++;
            }
        }

        meshData.ProcessMesh(); // so lighting works out nicely, this should be done in the seperate thread ("GenerateTerrainMesh") to cause less lag

        return meshData;

    }
}

public class MeshData {
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    Vector3[] bakedNormals;

    Vector3[] borderVertices;
    int[] borderTriangles;

    int triangleIndex;
    int borderTriangleIndex;

    bool useFlatShading;

    public MeshData(int verticesPerLine, bool useFlatShading) {
        this.useFlatShading = useFlatShading;

        vertices = new Vector3[verticesPerLine * verticesPerLine];         // How many vertices: w * h
        uvs = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine-1)*(verticesPerLine-1)*6];    // Length: how many squares vertices form (w-1 * h-1), each made up of 2 triangles of 3 vertices

        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[24 * verticesPerLine]; // Number of squares is 4*verticesPerLine in mesh, 6*4=24
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
        if (vertexIndex < 0) {
            borderVertices[-vertexIndex - 1] = vertexPosition; //-vertexIndex to start from 1 going upwards, -1 to start from 0
        } else {
            vertices[vertexIndex] = vertexPosition;
            uvs [vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c) {
        if (a < 0 || b < 0 || c < 0) {
            borderTriangles [borderTriangleIndex] = a;
            borderTriangles [borderTriangleIndex+1] = b;
            borderTriangles [borderTriangleIndex+2] = c;
            borderTriangleIndex += 3;
        } else {
            triangles [triangleIndex] = a;
            triangles [triangleIndex+1] = b;
            triangles [triangleIndex+2] = c;
            triangleIndex += 3;
        }
    }

    // Normals are calculated based on triangles surrounding it, vertices along edge of chunk dont have access to triangles in adjacent chunks,
    // causing wrong normals, and lighting seams.
    // We fix this by creating our own RecalculateNormals here
    Vector3[] CalculateNormals() {

        // Regular triangles
        Vector3[] vertexNormals = new Vector3[vertices.Length]; // normal count = vertice count
        int triangleCount = triangles.Length / 3; // triangles stores sets of 3 vertices
        for (int i = 0; i < triangleCount; i++) {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices (vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        // Border triangles
        int borderTriangleCount = borderTriangles.Length / 3; // triangles stores sets of 3 vertices
        for (int i = 0; i < borderTriangleCount; i++) {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices (vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0) {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if (vertexIndexB >= 0) {
                vertexNormals[vertexIndexB] += triangleNormal;
            } 
            if (vertexIndexC >= 0) {
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }

        // Normalize all values in vertexNormals
        for (int i = 0; i < vertexNormals.Length; i++) {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    // Cross product of vertices gives us their normal
    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
        Vector3 pointA = (indexA < 0)?borderVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0)?borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0)?borderVertices[-indexC - 1] : vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void ProcessMesh() {
        if (useFlatShading) {
            FlatShading();
        } else {
            BakeNormals();
        }
    }

    void BakeNormals() {
        bakedNormals = CalculateNormals();
    }

    // FlatShading requires all vertices (of the triangle) to have normals that point in same direction
    // This isnt possible with triangles that share vertices, because the normals will be a blend
    // Therefore, we give each triangle unique vertices not shared with others (6 vertices instead of 4)
    // For example, previously 0,1,2,2,1,3. Now 0,1,2,3,4,5
    void FlatShading() {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[triangles.Length];

        for (int i = 0; i < triangles.Length; i++) {
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUvs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }

        vertices = flatShadedVertices;
        uvs = flatShadedUvs;
    }

    // CreateMesh() is being called from main game thread, whilst the rest (GenerateTerrainMesh) is called from a seperate thread
    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        if (useFlatShading) {
            mesh.RecalculateNormals();
        } else {
            mesh.normals = bakedNormals;
        }
        return mesh;
    }
}