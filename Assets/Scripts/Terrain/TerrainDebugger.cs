using UnityEngine;

public class TerrainDebugger : MonoBehaviour
{
    [Header("Terrain Analysis")]
    [SerializeField] private bool analyzeTerrain = true;
    [SerializeField] private bool showMeshInfo = true;
    [SerializeField] private bool showHeightMapInfo = true;
    [SerializeField] private bool showWaterRegions = true;
    
    [Header("Visual Debug")]
    [SerializeField] private bool showHeightMapGizmos = true;
    [SerializeField] private bool showWaterGizmos = true;
    [SerializeField] private bool showMeshVertices = false;
    [SerializeField] private Color waterColor = Color.blue;
    [SerializeField] private Color landColor = Color.green;
    [SerializeField] private Color meshVertexColor = Color.red;
    
    [Header("Mesh Analysis")]
    [SerializeField] private bool checkMeshIntegrity = true;
    [SerializeField] private bool checkForHoles = true;
    [SerializeField] private bool checkForOverlaps = true;
    
    private MapGenerator mapGenerator;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private TerrainColliderGenerator terrainCollider;
    
    private void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        meshFilter = FindObjectOfType<MeshFilter>();
        meshRenderer = FindObjectOfType<MeshRenderer>();
        terrainCollider = FindObjectOfType<TerrainColliderGenerator>();
        
        if (analyzeTerrain)
        {
            StartCoroutine(AnalyzeTerrain());
        }
    }
    
    private System.Collections.IEnumerator AnalyzeTerrain()
    {
        // Wait for map generation to complete
        while (mapGenerator == null || !mapGenerator.IsMapReady)
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.Log("=== TERRAIN ANALYSIS STARTED ===");
        
        if (showMeshInfo)
        {
            AnalyzeMesh();
        }
        
        if (showHeightMapInfo)
        {
            AnalyzeHeightMap();
        }
        
        if (showWaterRegions)
        {
            AnalyzeWaterRegions();
        }
        
        if (checkMeshIntegrity)
        {
            CheckMeshIntegrity();
        }
        
        Debug.Log("=== TERRAIN ANALYSIS COMPLETE ===");
    }
    
    private void AnalyzeMesh()
    {
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("No mesh found! This could be the source of boat positioning issues.");
            return;
        }
        
        Mesh mesh = meshFilter.sharedMesh;
        Debug.Log($"=== MESH ANALYSIS ===");
        Debug.Log($"Mesh Name: {mesh.name}");
        Debug.Log($"Vertices: {mesh.vertexCount}");
        Debug.Log($"Triangles: {mesh.triangles.Length / 3}");
        Debug.Log($"UVs: {mesh.uv.Length}");
        Debug.Log($"Normals: {mesh.normals.Length}");
        Debug.Log($"Bounds: {mesh.bounds}");
        Debug.Log($"Submesh Count: {mesh.subMeshCount}");
        
        // Check for potential issues
        if (mesh.vertexCount == 0)
        {
            Debug.LogError("❌ MESH HAS NO VERTICES! This will cause positioning issues.");
        }
        
        if (mesh.triangles.Length == 0)
        {
            Debug.LogError("❌ MESH HAS NO TRIANGLES! This will cause collision issues.");
        }
        
        if (mesh.bounds.size == Vector3.zero)
        {
            Debug.LogError("❌ MESH HAS ZERO BOUNDS! This will cause physics issues.");
        }
        
        // Check for NaN or infinite values
        bool hasInvalidVertices = false;
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            Vector3 vertex = mesh.vertices[i];
            if (float.IsNaN(vertex.x) || float.IsNaN(vertex.y) || float.IsNaN(vertex.z) ||
                float.IsInfinity(vertex.x) || float.IsInfinity(vertex.y) || float.IsInfinity(vertex.z))
            {
                hasInvalidVertices = true;
                Debug.LogError($"❌ Invalid vertex at index {i}: {vertex}");
            }
        }
        
        if (!hasInvalidVertices)
        {
            Debug.Log("✅ All mesh vertices are valid");
        }
    }
    
    private void AnalyzeHeightMap()
    {
        if (mapGenerator == null || mapGenerator.currentHeightMap == null)
        {
            Debug.LogError("No height map found!");
            return;
        }
        
        float[,] heightMap = mapGenerator.currentHeightMap;
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        Debug.Log($"=== HEIGHT MAP ANALYSIS ===");
        Debug.Log($"Dimensions: {width}x{height}");
        
        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;
        int waterPixels = 0;
        int landPixels = 0;
        int invalidPixels = 0;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float heightValue = heightMap[x, y];
                
                if (float.IsNaN(heightValue) || float.IsInfinity(heightValue))
                {
                    invalidPixels++;
                    continue;
                }
                
                minHeight = Mathf.Min(minHeight, heightValue);
                maxHeight = Mathf.Max(maxHeight, heightValue);
                
                if (mapGenerator.IsWater(new Vector3(x - width/2f, 0, y - height/2f)))
                {
                    waterPixels++;
                }
                else
                {
                    landPixels++;
                }
            }
        }
        
        Debug.Log($"Height Range: {minHeight:F3} to {maxHeight:F3}");
        Debug.Log($"Water Pixels: {waterPixels} ({waterPixels * 100f / (width * height):F1}%)");
        Debug.Log($"Land Pixels: {landPixels} ({landPixels * 100f / (width * height):F1}%)");
        Debug.Log($"Invalid Pixels: {invalidPixels}");
        
        if (invalidPixels > 0)
        {
            Debug.LogError($"❌ {invalidPixels} invalid pixels found in height map!");
        }
        
        if (waterPixels == 0)
        {
            Debug.LogError("❌ NO WATER REGIONS FOUND! This will cause boat spawning issues.");
        }
        
        if (landPixels == 0)
        {
            Debug.LogError("❌ NO LAND REGIONS FOUND! This will cause terrain issues.");
        }
    }
    
    private void AnalyzeWaterRegions()
    {
        if (mapGenerator == null || mapGenerator.currentHeightMap == null)
        {
            return;
        }
        
        Debug.Log($"=== WATER REGION ANALYSIS ===");
        
        float[,] heightMap = mapGenerator.currentHeightMap;
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        // Find water regions
        int waterRegions = 0;
        float totalWaterArea = 0f;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 worldPos = new Vector3(x - width/2f, 0, y - height/2f);
                if (mapGenerator.IsWater(worldPos))
                {
                    waterRegions++;
                    totalWaterArea += 1f; // Each pixel represents 1 unit area
                }
            }
        }
        
        Debug.Log($"Water Regions: {waterRegions}");
        Debug.Log($"Total Water Area: {totalWaterArea} square units");
        
        if (waterRegions < 100)
        {
            Debug.LogWarning($"⚠️ Very few water regions ({waterRegions}). This may cause boat spawning issues.");
        }
    }
    
    private void CheckMeshIntegrity()
    {
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return;
        }
        
        Mesh mesh = meshFilter.sharedMesh;
        Debug.Log($"=== MESH INTEGRITY CHECK ===");
        
        // Check for duplicate vertices
        Vector3[] vertices = mesh.vertices;
        int duplicateVertices = 0;
        
        for (int i = 0; i < vertices.Length; i++)
        {
            for (int j = i + 1; j < vertices.Length; j++)
            {
                if (Vector3.Distance(vertices[i], vertices[j]) < 0.001f)
                {
                    duplicateVertices++;
                }
            }
        }
        
        Debug.Log($"Duplicate Vertices: {duplicateVertices}");
        
        // Check for degenerate triangles
        int[] triangles = mesh.triangles;
        int degenerateTriangles = 0;
        
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];
            
            // Check if triangle has zero area
            float area = Vector3.Cross(v2 - v1, v3 - v1).magnitude * 0.5f;
            if (area < 0.001f)
            {
                degenerateTriangles++;
            }
        }
        
        Debug.Log($"Degenerate Triangles: {degenerateTriangles}");
        
        if (degenerateTriangles > 0)
        {
            Debug.LogError($"❌ {degenerateTriangles} degenerate triangles found! This can cause physics issues.");
        }
        
        // Check mesh bounds
        Bounds bounds = mesh.bounds;
        Debug.Log($"Mesh Bounds: {bounds}");
        Debug.Log($"Mesh Center: {bounds.center}");
        Debug.Log($"Mesh Size: {bounds.size}");
        
        if (bounds.size.x == 0 || bounds.size.z == 0)
        {
            Debug.LogError("❌ Mesh has zero size in X or Z direction! This will cause positioning issues.");
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showHeightMapGizmos || mapGenerator == null || mapGenerator.currentHeightMap == null)
            return;
        
        float[,] heightMap = mapGenerator.currentHeightMap;
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        // Draw height map as colored points
        for (int x = 0; x < width; x += 5) // Sample every 5th point for performance
        {
            for (int y = 0; y < height; y += 5)
            {
                Vector3 worldPos = new Vector3(x - width/2f, heightMap[x, y] * mapGenerator.meshHeightMultiplier, y - height/2f);
                
                if (mapGenerator.IsWater(new Vector3(x - width/2f, 0, y - height/2f)))
                {
                    Gizmos.color = waterColor;
                }
                else
                {
                    Gizmos.color = landColor;
                }
                
                Gizmos.DrawWireCube(worldPos, Vector3.one * 0.5f);
            }
        }
        
        // Draw mesh vertices if enabled
        if (showMeshVertices && meshFilter != null && meshFilter.sharedMesh != null)
        {
            Gizmos.color = meshVertexColor;
            Vector3[] vertices = meshFilter.sharedMesh.vertices;
            
            for (int i = 0; i < vertices.Length; i += 10) // Sample every 10th vertex
            {
                Vector3 worldPos = meshFilter.transform.TransformPoint(vertices[i]);
                Gizmos.DrawWireSphere(worldPos, 0.1f);
            }
        }
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 300, 10, 290, 300));
        GUILayout.Label("=== TERRAIN DEBUGGER ===");
        
        if (mapGenerator != null)
        {
            GUILayout.Label($"Map Generator: ✓");
            GUILayout.Label($"Map Ready: {mapGenerator.IsMapReady}");
            GUILayout.Label($"Seed: {mapGenerator.seed}");
        }
        else
        {
            GUILayout.Label("Map Generator: ✗");
        }
        
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            GUILayout.Label($"Mesh: ✓");
            GUILayout.Label($"Vertices: {meshFilter.sharedMesh.vertexCount}");
            GUILayout.Label($"Triangles: {meshFilter.sharedMesh.triangles.Length / 3}");
        }
        else
        {
            GUILayout.Label("Mesh: ✗");
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Re-analyze Terrain"))
        {
            StartCoroutine(AnalyzeTerrain());
        }
        
        GUILayout.Space(10);
        GUILayout.Label("=== VISUAL DEBUG ===");
        showHeightMapGizmos = GUILayout.Toggle(showHeightMapGizmos, "Show Height Map");
        showWaterGizmos = GUILayout.Toggle(showWaterGizmos, "Show Water Regions");
        showMeshVertices = GUILayout.Toggle(showMeshVertices, "Show Mesh Vertices");
        
        GUILayout.EndArea();
    }
}
