using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ArrayVisualizer))]
public class ArrayVisualizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ArrayVisualizer script = (ArrayVisualizer)target;
        int[,] array = script.myArray;

        if (array == null)
        {
            EditorGUILayout.LabelField("Array is null.");
            return;
        }

        int rows = array.GetLength(0);
        int cols = array.GetLength(1);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2D Array Contents:");

        for (int r = 0; r < rows; r++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < cols; c++)
            {
                EditorGUILayout.LabelField(array[r, c].ToString(), GUILayout.Width(40));
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
