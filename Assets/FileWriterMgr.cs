using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class FileWriterMgr : MonoBehaviour
{
    public static FileWriterMgr inst;
    
    public string instanceName;
    string directoryPath;
    public List<string> fileTypes;
    public List<string> fileNames;

    void Awake()
    {
        inst = this;

        // Create Logs folder inside Assets
        directoryPath = Path.Combine(Application.dataPath, "Stats Files");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            Debug.Log("Created Logs folder at: " + directoryPath);
        }

        if (instanceName == "")
            instanceName = "Default";

        directoryPath = Path.Combine(directoryPath, instanceName);
        if (!Directory.Exists(directoryPath))
        {

            Directory.CreateDirectory(directoryPath);
            Debug.Log("Created Instance folder at: " + directoryPath);
        }


        foreach (string type in fileTypes)
        {
            string fileName = instanceName + type;
            fileNames.Add(fileName);
        }
    }

    public void WriteGraphCSV(string fileName, List<float> data)
    {
        string filePath = Path.Combine(directoryPath, fileName);

        File.WriteAllText(filePath, 0 + ", " + data[0] + "\n");

        for (int i = 1; i < data.Count; i++)
            File.AppendAllText(filePath, i + ", " + data[i]+ "\n");
    }

    public void AppendMetricCSV()
    {
        string filePath = Path.Combine(directoryPath, fileNames[0]);
        File.AppendAllText(filePath, CVRPMain.inst.run + ", " + CVRPMain.inst.seed
            + ", " + StatsMgr.inst.bestCosts[CVRPMain.inst.run]
            + ", " + StatsMgr.inst.speeds[CVRPMain.inst.run] + "\n");
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
