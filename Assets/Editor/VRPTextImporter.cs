using UnityEditor;
using UnityEngine;
using UnityEditor.AssetImporters;

[ScriptedImporter(1, "vrp")]
public class VRPTextImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        // Load the file content as text
        string fileText = System.IO.File.ReadAllText(ctx.assetPath);

        // Create a TextAsset from that text
        TextAsset textAsset = new TextAsset(fileText);

        // Add the TextAsset as the main asset for this importer
        ctx.AddObjectToAsset("main obj", textAsset);
        ctx.SetMainObject(textAsset);
    }
}
