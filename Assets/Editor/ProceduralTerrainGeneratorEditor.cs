using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ProceduralTerrainGenerator))]
public class ProceduralTerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();

        ProceduralTerrainGenerator generator = (ProceduralTerrainGenerator)target;

        // Add spacing
        GUILayout.Space(10);

        // Draw button
        if (GUILayout.Button("Generate Terrain"))
        {
            generator.GenerateTerrain();
        }
    }
}
