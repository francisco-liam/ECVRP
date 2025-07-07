using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RNGTest : MonoBehaviour
{
    public Object file1;
    public Object file2;
    // Start is called before the first frame update
    void Start()
    {
       MyRNG test = new MyRNG(8008);
        for (int i = 0; i < 5; i++)
            Debug.Log(test.Range(10, 20));

        string pathToInstance1 = AssetDatabase.GetAssetPath(file1);
        string pathToInstance2 = AssetDatabase.GetAssetPath(file2);
        Debug.Log(FileComparer.FindFirstDifference(pathToInstance1, pathToInstance2));

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
