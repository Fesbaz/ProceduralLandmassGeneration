using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// What this do: Generating flat plain and setting height of vertices
// If youre reading this to find solution for UNEVEN CHUNK BORDERS, make sure youre using Global normalize mode in Default Noise.asset in Terrain Assets
// Borders are uneven between chucks of different LODs (use lower vertice count meshes) causing seams/clipping/uneven ground you see through
// Fix is to increase render distance until you wont notice it
public static class MeshGenerator {
    public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail) {
        int skipIncrement = (levelOfDetail == 0)?1:levelOfDetail * 2; // If LOD 0, set to 1, otherwise * 2. ? = if
        int numVertsPerLine = meshSettings.numVertsPerLine;
        // To get the mesh centered at 0,0 we need to start from top left
        Vector2 topLeft = new Vector2 (-1, 1) * meshSettings.meshWorldSize / 2f;

        MeshData meshData = new MeshData(numVertsPerLine, skipIncrement, meshSettings.useFlatShading);
        
        int[,] vertexIndicesMap = new int[numVertsPerLine, numVertsPerLine];
        int meshVertexIndex = 0;
        int outOfMeshVertexIndex = -1;

        for (int y = 0; y < numVertsPerLine; y++) {
            for (int x = 0; x < numVertsPerLine; x++) {
                bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && ((x-2)%skipIncrement != 0 || (y - 2) % skipIncrement != 0);
                if (isOutOfMeshVertex) {
                    vertexIndicesMap[x, y] = outOfMeshVertexIndex;
                    outOfMeshVertexIndex--;
                } else if (!isSkippedVertex){
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }
        for (int y = 0; y < numVertsPerLine; y++) {
            for (int x = 0; x < numVertsPerLine; x++) {
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && ((x-2)%skipIncrement != 0 || (y - 2) % skipIncrement != 0);
                
                if (!isSkippedVertex) {
                    bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                    bool isMeshEdgeVertex = (y == 1 || y == numVertsPerLine - 2 || x == 1 || x == numVertsPerLine - 2) && !isOutOfMeshVertex;
                    bool isMainVertex = (x - 2) % skipIncrement == 0 && (y - 2) % skipIncrement == 0 && !isOutOfMeshVertex && !isMeshEdgeVertex;
                    bool isEdgeConnectionVertex = (y == 2 || y == numVertsPerLine - 3 || x == 2|| x == numVertsPerLine - 3) && !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;

                    int vertexIndex = vertexIndicesMap[x, y];
                    Vector2 percent = new Vector2(x - 1, y - 1)/(numVertsPerLine - 3);
                    Vector2 vertexPosition2D = topLeft + new Vector2(percent.x, -percent.y) * meshSettings.meshWorldSize;
                    float height = heightMap[x, y];

                    if (isEdgeConnectionVertex) {
                        bool isVertical = x == 2 || x == numVertsPerLine - 3;
                        int dstToMainVertexA = ((isVertical)?y - 2 : x - 2) % skipIncrement;
                        int dstToMainVertexB = skipIncrement - dstToMainVertexA;
                        float dstPercentFromAToB = dstToMainVertexA / (float)skipIncrement;

                        Coord coordA = new Coord ((isVertical) ? x : x - dstToMainVertexA, (isVertical) ? y - dstToMainVertexA : y);
                        Coord coordB = new Coord ((isVertical) ? x : x + dstToMainVertexB, (isVertical) ? y + dstToMainVertexB : y);

                        float heightMainVertexA = heightMap [coordA.x,coordA.y];
                        float heightMainVertexB = heightMap [coordB.x,coordB.y];

                        height = heightMainVertexA * (1 - dstPercentFromAToB) + heightMainVertexB * dstPercentFromAToB;

                        EdgeConnectionVertexData edgeConnectionVertexData = new EdgeConnectionVertexData (vertexIndex, vertexIndicesMap [coordA.x, coordA.y], vertexIndicesMap [coordB.x, coordB.y], dstPercentFromAToB);
                        meshData.DeclareEdgeConnectionVertex (edgeConnectionVertexData);
                    }

                    meshData.AddVertex(new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), percent, vertexIndex);

                    bool createTriangle = x < numVertsPerLine - 1 && y < numVertsPerLine - 1 && (!isEdgeConnectionVertex || (x != 2 && y != 2));

                    if (createTriangle) {
                        /*
                                A (x, y)
                                    *-------* B (x+i, y)
                                    |       |
                                    |       |
                                    |       |
                                    *-------*
                                C (x, y+i)   D (x+i, y+i)
                        */
                        int currentIncrement = (isMainVertex && x != numVertsPerLine - 3 && y != numVertsPerLine - 3) ? skipIncrement : 1;

                        int a = vertexIndicesMap[x, y];
                        int b = vertexIndicesMap[x + currentIncrement, y];
                        int c = vertexIndicesMap[x, y + currentIncrement];
                        int d = vertexIndicesMap[x + currentIncrement, y + currentIncrement];

                        // These 2 lines create the 2 triangles of the square
                        meshData.AddTriangle(a, d, c);
                        meshData.AddTriangle(d, a, b);
                    }
                }
            }
        }

        meshData.ProcessMesh(); // so lighting works out nicely, this should be done in the seperate thread ("GenerateTerrainMesh") to cause less lag

        return meshData;

    }

    public struct Coord {
        public readonly int x;
        public readonly int y;
 
        public Coord (int x, int y)
        {
            this.x = x;
            this.y = y;
        }
 
    }
}

public class EdgeConnectionVertexData {
    public int vertexIndex;
    public int mainVertexAIndex;
    public int mainVertexBIndex;
    public float dstPercentFromAToB;
 
    public EdgeConnectionVertexData (int vertexIndex, int mainVertexAIndex, int mainVertexBIndex, float dstPercentFromAToB)
    {
        this.vertexIndex = vertexIndex;
        this.mainVertexAIndex = mainVertexAIndex;
        this.mainVertexBIndex = mainVertexBIndex;
        this.dstPercentFromAToB = dstPercentFromAToB;
    }
}

public class MeshData {
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    Vector3[] bakedNormals;

    Vector3[] outOfMeshVertices;
    int[] outOfMeshTriangles;

    int triangleIndex;
    int outOfMeshTriangleIndex;

    EdgeConnectionVertexData[] edgeConnectionVertices;
    int edgeConnectionVertexIndex;

    bool useFlatShading;

    public MeshData(int numVertsPerLine, int skipIncrement, bool useFlatShading) {
        this.useFlatShading = useFlatShading;

        int numMeshEdgeVertices = (numVertsPerLine - 2) * 4 - 4;
        int numEdgeConnectionVertices = (skipIncrement - 1) * (numVertsPerLine - 5) / skipIncrement * 4;
        int numMainVerticesPerLine = (numVertsPerLine - 5) / skipIncrement + 1;
        int numMainVertices = numMainVerticesPerLine * numMainVerticesPerLine;

        vertices = new Vector3[numMeshEdgeVertices + numEdgeConnectionVertices + numMainVertices];         // How many vertices: w * h
        uvs = new Vector2[vertices.Length];
        edgeConnectionVertices = new EdgeConnectionVertexData[numEdgeConnectionVertices];

        int numMeshEdgeTriangles = 8 * (numVertsPerLine - 4);
        int numMainTriangles = (numMainVerticesPerLine - 1) * (numMainVerticesPerLine - 1) * 2;
        triangles = new int[(numMeshEdgeTriangles + numMainTriangles) * 3];    // Length: how many squares vertices form (w-1 * h-1), each made up of 2 triangles of 3 vertices

        outOfMeshVertices = new Vector3[numVertsPerLine * 4 - 4];
        outOfMeshTriangles = new int[24 * (numVertsPerLine - 2)]; // Number of squares is 4*verticesPerLine in mesh, 6*4=24
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
        if (vertexIndex < 0) {
            outOfMeshVertices[-vertexIndex - 1] = vertexPosition; //-vertexIndex to start from 1 going upwards, -1 to start from 0
        } else {
            vertices[vertexIndex] = vertexPosition;
            uvs [vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c) {
        if (a < 0 || b < 0 || c < 0) {
            outOfMeshTriangles [outOfMeshTriangleIndex] = a;
            outOfMeshTriangles [outOfMeshTriangleIndex+1] = b;
            outOfMeshTriangles [outOfMeshTriangleIndex+2] = c;
            outOfMeshTriangleIndex += 3;
        } else {
            triangles [triangleIndex] = a;
            triangles [triangleIndex+1] = b;
            triangles [triangleIndex+2] = c;
            triangleIndex += 3;
        }
    }

    public void DeclareEdgeConnectionVertex(EdgeConnectionVertexData edgeConnectionVertexData) {
        edgeConnectionVertices [edgeConnectionVertexIndex] = edgeConnectionVertexData;
        edgeConnectionVertexIndex++;
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
        int borderTriangleCount = outOfMeshTriangles.Length / 3; // triangles stores sets of 3 vertices
        for (int i = 0; i < borderTriangleCount; i++) {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = outOfMeshTriangles[normalTriangleIndex];
            int vertexIndexB = outOfMeshTriangles[normalTriangleIndex + 1];
            int vertexIndexC = outOfMeshTriangles[normalTriangleIndex + 2];

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

    void ProcessEdgeConnectionVertices() {
        foreach (EdgeConnectionVertexData e in edgeConnectionVertices) {
            bakedNormals [e.vertexIndex] = bakedNormals [e.mainVertexAIndex] * (1 - e.dstPercentFromAToB) + bakedNormals [e.mainVertexBIndex] * e.dstPercentFromAToB;
        }
    }

    // Cross product of vertices gives us their normal
    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
        Vector3 pointA = (indexA < 0)?outOfMeshVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0)?outOfMeshVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0)?outOfMeshVertices[-indexC - 1] : vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void ProcessMesh() {
        if (useFlatShading) {
            FlatShading();
        } else {
            BakeNormals();
            ProcessEdgeConnectionVertices();
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