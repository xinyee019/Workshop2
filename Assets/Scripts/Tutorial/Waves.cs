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

    // Performance optimization
    [Header("Performance Settings")]
    public bool useSimplifiedPhysics = true; // Use fast approximation for boat physics
    public int physicsUpdateSkip = 2; // Update physics every N frames

    // Mesh Components
    protected MeshFilter meshFilter;
    protected MeshRenderer meshRenderer;
    protected Mesh waterMesh;

    // Cache for performance
    private Vector3[] baseVertices;
    private bool isInitialized = false;
    private int frameCount = 0;

    // Height cache for physics calculations
    private Dictionary<Vector2Int, CachedHeight> heightCache;
    private const float CACHE_LIFETIME = 0.1f; // Cache height for 0.1 seconds

    private struct CachedHeight
    {
        public float height;
        public float timestamp;
    }

    // Public property to access the mesh safely
    public Mesh Mesh { get { return waterMesh; } }

    void Start()
    {
        InitializeWaterMesh();
        heightCache = new Dictionary<Vector2Int, CachedHeight>();
    }

    void InitializeWaterMesh()
    {
        waterMesh = new Mesh();
        waterMesh.name = "WaterMesh_" + gameObject.name;

        baseVertices = GenerateVertices();
        waterMesh.vertices = baseVertices;
        waterMesh.triangles = GenerateTriangles();
        waterMesh.uv = GenerateUVs();

        waterMesh.RecalculateNormals();
        waterMesh.RecalculateBounds();

        meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = waterMesh;

        if (WaterMaterial != null)
        {
            meshRenderer.material = WaterMaterial;
        }

        FixMeshOrientation();
        isInitialized = true;
    }

    void FixMeshOrientation()
    {
        if (IsMeshUpsideDown())
        {
            int[] triangles = waterMesh.triangles;

            for (int i = 0; i < triangles.Length; i += 3)
            {
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
        Vector3 averageNormal = Vector3.zero;
        Vector3[] normals = waterMesh.normals;

        for (int i = 0; i < normals.Length; i++)
        {
            averageNormal += normals[i];
        }

        averageNormal /= normals.Length;
        return averageNormal.y < 0;
    }

    // OPTIMIZED: Fast height calculation for physics
    public float GetHeight(Vector3 position)
    {
        if (!isInitialized || Octaves == null || Octaves.Length == 0)
        {
            return transform.position.y;
        }

        if (useSimplifiedPhysics)
        {
            return GetHeightFast(position);
        }

        // Use cached mesh-based calculation
        return GetHeightFromMesh(position);
    }

    // NEW: Ultra-fast approximation using Perlin noise directly
    private float GetHeightFast(Vector3 position)
    {
        Vector3 localScale = transform.lossyScale;
        Vector3 localPosition = transform.position;

        // Convert to local space
        float localX = (position.x - localPosition.x) / localScale.x;
        float localZ = (position.z - localPosition.z) / localScale.z;

        float height = 0f;

        // Calculate wave height using same octave system
        foreach (Octave octave in Octaves)
        {
            if (octave.alternate)
            {
                float perl = Mathf.PerlinNoise(
                    (localX * octave.scale.x) / Dimension,
                    (localZ * octave.scale.y) / Dimension
                ) * Mathf.PI * 2f;

                height += Mathf.Cos(perl + octave.speed.magnitude * Time.time) * octave.height;
            }
            else
            {
                float perl = Mathf.PerlinNoise(
                    (localX * octave.scale.x + Time.time * octave.speed.x) / Dimension,
                    (localZ * octave.scale.y + Time.time * octave.speed.y) / Dimension
                );

                height += (perl - 0.5f) * octave.height;
            }
        }

        return (height + transform.position.y) * localScale.y;
    }

    // Original mesh-based calculation with caching
    private float GetHeightFromMesh(Vector3 position)
    {
        if (waterMesh == null || waterMesh.vertices == null || waterMesh.vertices.Length == 0)
        {
            return transform.position.y;
        }

        try
        {
            Vector3 localScale = transform.lossyScale;
            Vector3 localPosition = transform.position;

            Vector3 scaleFactor = new Vector3(1f / localScale.x, 1f, 1f / localScale.z);
            Vector3 localPos = Vector3.Scale(position - localPosition, scaleFactor);

            localPos.y = 0;

            // Check cache
            Vector2Int cacheKey = new Vector2Int(
                Mathf.RoundToInt(localPos.x * 10),
                Mathf.RoundToInt(localPos.z * 10)
            );

            if (heightCache.ContainsKey(cacheKey))
            {
                CachedHeight cached = heightCache[cacheKey];
                if (Time.time - cached.timestamp < CACHE_LIFETIME)
                {
                    return cached.height;
                }
            }

            // Get the four surrounding grid points
            Vector3 p1 = new Vector3(Mathf.Floor(localPos.x), 0, Mathf.Floor(localPos.z));
            Vector3 p2 = new Vector3(Mathf.Floor(localPos.x), 0, Mathf.Ceil(localPos.z));
            Vector3 p3 = new Vector3(Mathf.Ceil(localPos.x), 0, Mathf.Floor(localPos.z));
            Vector3 p4 = new Vector3(Mathf.Ceil(localPos.x), 0, Mathf.Ceil(localPos.z));

            p1.x = Mathf.Clamp(p1.x, 0, Dimension - 1);
            p1.z = Mathf.Clamp(p1.z, 0, Dimension - 1);
            p2.x = Mathf.Clamp(p2.x, 0, Dimension - 1);
            p2.z = Mathf.Clamp(p2.z, 0, Dimension - 1);
            p3.x = Mathf.Clamp(p3.x, 0, Dimension - 1);
            p3.z = Mathf.Clamp(p3.z, 0, Dimension - 1);
            p4.x = Mathf.Clamp(p4.x, 0, Dimension - 1);
            p4.z = Mathf.Clamp(p4.z, 0, Dimension - 1);

            float d1 = Vector2.Distance(new Vector2(p1.x, p1.z), new Vector2(localPos.x, localPos.z));
            float d2 = Vector2.Distance(new Vector2(p2.x, p2.z), new Vector2(localPos.x, localPos.z));
            float d3 = Vector2.Distance(new Vector2(p3.x, p3.z), new Vector2(localPos.x, localPos.z));
            float d4 = Vector2.Distance(new Vector2(p4.x, p4.z), new Vector2(localPos.x, localPos.z));

            float totalDistance = d1 + d2 + d3 + d4;
            if (totalDistance < Mathf.Epsilon)
            {
                int vertexIndex = Index((int)localPos.x, (int)localPos.z);
                if (vertexIndex >= 0 && vertexIndex < waterMesh.vertices.Length)
                {
                    return (waterMesh.vertices[vertexIndex].y + transform.position.y) * localScale.y;
                }
                return transform.position.y;
            }

            float w1 = (1f / (d1 + 0.001f));
            float w2 = (1f / (d2 + 0.001f));
            float w3 = (1f / (d3 + 0.001f));
            float w4 = (1f / (d4 + 0.001f));
            float totalWeight = w1 + w2 + w3 + w4;

            float h1 = waterMesh.vertices[Index((int)p1.x, (int)p1.z)].y + transform.position.y;
            float h2 = waterMesh.vertices[Index((int)p2.x, (int)p2.z)].y + transform.position.y;
            float h3 = waterMesh.vertices[Index((int)p3.x, (int)p3.z)].y + transform.position.y;
            float h4 = waterMesh.vertices[Index((int)p4.x, (int)p4.z)].y + transform.position.y;

            float weightedHeight = (h1 * w1 + h2 * w2 + h3 * w3 + h4 * w4) / totalWeight;
            float finalHeight = weightedHeight * localScale.y;

            // Cache the result
            heightCache[cacheKey] = new CachedHeight { height = finalHeight, timestamp = Time.time };

            return finalHeight;
        }
        catch (System.Exception)
        {
            return transform.position.y;
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
                triangles[triIndex++] = Index(x, z);
                triangles[triIndex++] = Index(x + 1, z);
                triangles[triIndex++] = Index(x, z + 1);

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

        // Skip frames for performance
        frameCount++;
        if (frameCount % physicsUpdateSkip != 0)
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

        // Clear old cache entries
        if (frameCount % 60 == 0)
        {
            ClearOldCache();
        }
    }

    private void ClearOldCache()
    {
        List<Vector2Int> keysToRemove = new List<Vector2Int>();
        foreach (var kvp in heightCache)
        {
            if (Time.time - kvp.Value.timestamp > CACHE_LIFETIME)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            heightCache.Remove(key);
        }
    }

    private float CalculateWaveHeight(int x, int z)
    {
        float height = 0f;

        foreach (Octave octave in Octaves)
        {
            if (octave.alternate)
            {
                float perl = Mathf.PerlinNoise(
                    (x * octave.scale.x) / Dimension,
                    (z * octave.scale.y) / Dimension
                ) * Mathf.PI * 2f;

                height += Mathf.Cos(perl + octave.speed.magnitude * Time.time) * octave.height;
            }
            else
            {
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
        if (waterMesh != null)
        {
            DestroyImmediate(waterMesh);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!isInitialized) return;

        Gizmos.color = Color.cyan;
        Bounds bounds = waterMesh.bounds;
        Gizmos.DrawWireCube(transform.position + bounds.center, bounds.size);

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