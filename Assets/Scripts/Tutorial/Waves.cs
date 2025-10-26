using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waves : MonoBehaviour
{
    // Public Properties
    public int Dimension = 10;
    public float UVScale = 2f;
    public Octave[] Octaves;
    public Material WaterMaterial;

    // Mesh Components
    protected MeshFilter meshFilter;
    protected MeshRenderer meshRenderer;
    protected Mesh waterMesh;

    // Cache for performance
    private Vector3[] baseVertices;
    private bool isInitialized = false;

    // Public property to access the mesh safely
    public Mesh Mesh { get { return waterMesh; } }

    void Start()
    {
        InitializeWaterMesh();
    }

    void InitializeWaterMesh()
    {
        // Create and configure mesh
        waterMesh = new Mesh();
        waterMesh.name = "WaterMesh_" + gameObject.name;

        // Generate mesh components
        baseVertices = GenerateVertices();
        waterMesh.vertices = baseVertices;
        waterMesh.triangles = GenerateTriangles();
        waterMesh.uv = GenerateUVs();

        // IMPORTANT: Calculate normals correctly for proper lighting
        waterMesh.RecalculateNormals();
        waterMesh.RecalculateBounds();

        // Set up mesh components
        meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = waterMesh;

        // Set material if provided
        if (WaterMaterial != null)
        {
            meshRenderer.material = WaterMaterial;
        }

        // Fix: Ensure mesh faces the correct direction
        FixMeshOrientation();

        isInitialized = true;
    }

    void FixMeshOrientation()
    {
        // If the mesh is upside down, we can flip the triangles
        if (IsMeshUpsideDown())
        {
            Debug.Log("Fixing upside down mesh orientation...");
            int[] triangles = waterMesh.triangles;

            // Reverse winding order for all triangles
            for (int i = 0; i < triangles.Length; i += 3)
            {
                // Swap vertices to change winding order
                int temp = triangles[i];
                triangles[i] = triangles[i + 1];
                triangles[i + 1] = temp;
            }

            waterMesh.triangles = triangles;
            waterMesh.RecalculateNormals();
        }
    }

    bool IsMeshUpsideDown()
    {
        // Check if the average normal is pointing down
        Vector3 averageNormal = Vector3.zero;
        Vector3[] normals = waterMesh.normals;

        for (int i = 0; i < normals.Length; i++)
        {
            averageNormal += normals[i];
        }

        averageNormal /= normals.Length;
        return averageNormal.y < 0;
    }

    public float GetHeight(Vector3 position)
    {
        if (!isInitialized || waterMesh == null || waterMesh.vertices == null || waterMesh.vertices.Length == 0)
        {
            Debug.LogWarning("Water mesh not initialized. Returning default height 0.");
            return 0f;
        }

        try
        {
            // Convert to local space considering scale and position
            Vector3 localScale = transform.lossyScale;
            Vector3 localPosition = transform.position;

            Vector3 scaleFactor = new Vector3(1f / localScale.x, 1f, 1f / localScale.z);
            Vector3 localPos = Vector3.Scale(position - localPosition, scaleFactor);

            // Adjust for mesh position offset (Y=1)
            localPos.y = 0; // We only care about XZ for grid lookup

            // Get the four surrounding grid points
            Vector3 p1 = new Vector3(Mathf.Floor(localPos.x), 0, Mathf.Floor(localPos.z));
            Vector3 p2 = new Vector3(Mathf.Floor(localPos.x), 0, Mathf.Ceil(localPos.z));
            Vector3 p3 = new Vector3(Mathf.Ceil(localPos.x), 0, Mathf.Floor(localPos.z));
            Vector3 p4 = new Vector3(Mathf.Ceil(localPos.x), 0, Mathf.Ceil(localPos.z));

            // Clamp points to valid range
            p1.x = Mathf.Clamp(p1.x, 0, Dimension - 1);
            p1.z = Mathf.Clamp(p1.z, 0, Dimension - 1);
            p2.x = Mathf.Clamp(p2.x, 0, Dimension - 1);
            p2.z = Mathf.Clamp(p2.z, 0, Dimension - 1);
            p3.x = Mathf.Clamp(p3.x, 0, Dimension - 1);
            p3.z = Mathf.Clamp(p3.z, 0, Dimension - 1);
            p4.x = Mathf.Clamp(p4.x, 0, Dimension - 1);
            p4.z = Mathf.Clamp(p4.z, 0, Dimension - 1);

            // Calculate distances for bilinear interpolation
            float d1 = Vector2.Distance(new Vector2(p1.x, p1.z), new Vector2(localPos.x, localPos.z));
            float d2 = Vector2.Distance(new Vector2(p2.x, p2.z), new Vector2(localPos.x, localPos.z));
            float d3 = Vector2.Distance(new Vector2(p3.x, p3.z), new Vector2(localPos.x, localPos.z));
            float d4 = Vector2.Distance(new Vector2(p4.x, p4.z), new Vector2(localPos.x, localPos.z));

            // Avoid division by zero
            float totalDistance = d1 + d2 + d3 + d4;
            if (totalDistance < Mathf.Epsilon)
            {
                // Exactly on a vertex
                int vertexIndex = Index((int)localPos.x, (int)localPos.z);
                if (vertexIndex >= 0 && vertexIndex < waterMesh.vertices.Length)
                {
                    return (waterMesh.vertices[vertexIndex].y + transform.position.y) * localScale.y;
                }
                return transform.position.y;
            }

            // Calculate weights (inverse distance weighting)
            float w1 = (1f / (d1 + 0.001f));
            float w2 = (1f / (d2 + 0.001f));
            float w3 = (1f / (d3 + 0.001f));
            float w4 = (1f / (d4 + 0.001f));
            float totalWeight = w1 + w2 + w3 + w4;

            // Get heights from mesh vertices (include transform position)
            float h1 = waterMesh.vertices[Index((int)p1.x, (int)p1.z)].y + transform.position.y;
            float h2 = waterMesh.vertices[Index((int)p2.x, (int)p2.z)].y + transform.position.y;
            float h3 = waterMesh.vertices[Index((int)p3.x, (int)p3.z)].y + transform.position.y;
            float h4 = waterMesh.vertices[Index((int)p4.x, (int)p4.z)].y + transform.position.y;

            // Calculate weighted height
            float weightedHeight = (h1 * w1 + h2 * w2 + h3 * w3 + h4 * w4) / totalWeight;

            // Apply scale and return
            return weightedHeight * localScale.y;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error calculating water height at position {position}: {e.Message}");
            return transform.position.y; // Fallback to water object position
        }
    }

    private Vector3[] GenerateVertices()
    {
        Vector3[] vertices = new Vector3[(Dimension + 1) * (Dimension + 1)];

        for (int x = 0; x <= Dimension; x++)
        {
            for (int z = 0; z <= Dimension; z++)
            {
                vertices[Index(x, z)] = new Vector3(x, 0, z);
            }
        }

        return vertices;
    }

    private int[] GenerateTriangles()
    {
        int[] triangles = new int[Dimension * Dimension * 6];

        int triIndex = 0;
        for (int x = 0; x < Dimension; x++)
        {
            for (int z = 0; z < Dimension; z++)
            {
                // First triangle - clockwise winding for correct facing
                triangles[triIndex++] = Index(x, z);
                triangles[triIndex++] = Index(x + 1, z);
                triangles[triIndex++] = Index(x, z + 1);

                // Second triangle - clockwise winding for correct facing
                triangles[triIndex++] = Index(x + 1, z);
                triangles[triIndex++] = Index(x + 1, z + 1);
                triangles[triIndex++] = Index(x, z + 1);
            }
        }

        return triangles;
    }

    private Vector2[] GenerateUVs()
    {
        Vector2[] uvs = new Vector2[(Dimension + 1) * (Dimension + 1)];

        for (int x = 0; x <= Dimension; x++)
        {
            for (int z = 0; z <= Dimension; z++)
            {
                Vector2 uv = new Vector2(
                    (x / (float)Dimension) * UVScale,
                    (z / (float)Dimension) * UVScale
                );
                uvs[Index(x, z)] = uv;
            }
        }

        return uvs;
    }

    private int Index(int x, int z)
    {
        return x * (Dimension + 1) + z;
    }

    void Update()
    {
        if (!isInitialized || waterMesh == null || Octaves == null || Octaves.Length == 0)
            return;

        Vector3[] vertices = new Vector3[baseVertices.Length];
        Array.Copy(baseVertices, vertices, baseVertices.Length);

        for (int x = 0; x <= Dimension; x++)
        {
            for (int z = 0; z <= Dimension; z++)
            {
                float y = CalculateWaveHeight(x, z);
                vertices[Index(x, z)] = new Vector3(x, y, z);
            }
        }

        waterMesh.vertices = vertices;
        waterMesh.RecalculateNormals();
    }

    private float CalculateWaveHeight(int x, int z)
    {
        float height = 0f;

        foreach (Octave octave in Octaves)
        {
            if (octave.alternate)
            {
                // Sine wave based pattern
                float perl = Mathf.PerlinNoise(
                    (x * octave.scale.x) / Dimension,
                    (z * octave.scale.y) / Dimension
                ) * Mathf.PI * 2f;

                height += Mathf.Cos(perl + octave.speed.magnitude * Time.time) * octave.height;
            }
            else
            {
                // Standard Perlin noise based pattern
                float perl = Mathf.PerlinNoise(
                    (x * octave.scale.x + Time.time * octave.speed.x) / Dimension,
                    (z * octave.scale.y + Time.time * octave.speed.y) / Dimension
                );

                height += (perl - 0.5f) * octave.height;
            }
        }

        return height;
    }

    void OnDestroy()
    {
        // Clean up mesh to prevent memory leaks
        if (waterMesh != null)
        {
            DestroyImmediate(waterMesh);
        }
    }

    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        if (!isInitialized) return;

        // Draw water surface bounds
        Gizmos.color = Color.cyan;
        Bounds bounds = waterMesh.bounds;
        Gizmos.DrawWireCube(transform.position + bounds.center, bounds.size);

        // Draw some sample height points
        Gizmos.color = Color.yellow;
        for (int x = 0; x <= Dimension; x += Dimension / 4)
        {
            for (int z = 0; z <= Dimension; z += Dimension / 4)
            {
                Vector3 worldPos = transform.TransformPoint(waterMesh.vertices[Index(x, z)]);
                Gizmos.DrawSphere(worldPos, 0.1f);
            }
        }
    }

    // Add this method to your Waves class
    public Vector3 GetWorldVertexPosition(int x, int z)
    {
        if (!isInitialized || waterMesh == null)
            return Vector3.zero;

        int index = Index(x, z);
        if (index >= 0 && index < waterMesh.vertices.Length)
        {
            return transform.TransformPoint(waterMesh.vertices[index]);
        }
        return Vector3.zero;
    }

    // Add this property to get all vertices in world space
    public Vector3[] WorldVertices
    {
        get
        {
            if (!isInitialized || waterMesh == null)
                return new Vector3[0];

            Vector3[] worldVerts = new Vector3[waterMesh.vertices.Length];
            for (int i = 0; i < waterMesh.vertices.Length; i++)
            {
                worldVerts[i] = transform.TransformPoint(waterMesh.vertices[i]);
            }
            return worldVerts;
        }
    }


    [Serializable]
    public struct Octave
    {
        public Vector2 speed;
        public Vector2 scale;
        public float height;
        public bool alternate;
    }
}