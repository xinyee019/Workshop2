using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ProceduralTerrainGeneratorV2))]
public class ProceduralTerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();

        ProceduralTerrainGeneratorV2 generator = (ProceduralTerrainGeneratorV2)target;

        // Add spacing
        GUILayout.Space(10);

        // Draw button
        if (GUILayout.Button("Generate Terrain"))
        {
            generator.GenerateTerrain();
        }
    }
}
