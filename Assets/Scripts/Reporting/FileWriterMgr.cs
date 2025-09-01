using System;
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
    public bool addToDatabase;

    void Awake()
    {
        if (inst == null)
        {
            inst = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    public void Init()
    {
        fileNames = new List<string>();

        // Create Logs folder inside Assets
        directoryPath = Path.Combine(Application.persistentDataPath, "Stats Files");
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

    public void WriteGraphCSV(string fileName, List<double> data)
    {
        string filePath = Path.Combine(directoryPath, fileName);

        File.WriteAllText(filePath, 0 + ", " + data[0] + "\n");

        for (int i = 1; i < data.Count; i++)
            File.AppendAllText(filePath, i + ", " + data[i]+ "\n");
    }

    public void AppendMetricCSV()
    {
        string filePath = Path.Combine(directoryPath, fileNames[0]);
        if (CVRPMain.inst.run == 0)
            File.WriteAllText(filePath, CVRPMain.inst.run + ", " + ParametersMgr.inst.ap.seed
                + ", " + StatsMgr.inst.bestCosts
                + ", " + StatsMgr.inst.speeds 
                + ", " + (Time.realtimeSinceStartup - ParametersMgr.inst.startTime)
                + ", " + GeneticMgr.inst.nbIter + "\n");
        else
            File.AppendAllText(filePath, CVRPMain.inst.run + ", " + ParametersMgr.inst.ap.seed
                + ", " + StatsMgr.inst.bestCosts
                + ", " + StatsMgr.inst.speeds
                + ", " + (Time.realtimeSinceStartup - ParametersMgr.inst.startTime)
                + ", " + GeneticMgr.inst.nbIter + "\n");
    }

    public void ReadMetricFile()
    {
        string filePath = Path.Combine(directoryPath, fileNames[0]);
        if (!File.Exists(filePath))
        {
            CVRPMain.inst.run = 0;
            ParametersMgr.inst.ap.seed = 1;
        }
        else
        {
            string lastLine = null;

            // Read the last line efficiently
            using (var reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    lastLine = reader.ReadLine();
                }
            }

            if (string.IsNullOrWhiteSpace(lastLine))
            {
                throw new InvalidOperationException("CSV file is empty or only contains whitespace.");
            }

            // Split by comma
            string[] parts = lastLine.Split(',');

            if (parts.Length < 2)
            {
                throw new InvalidOperationException("CSV does not have at least two columns.");
            }

            // Parse first two columns as ints
            CVRPMain.inst.run = int.Parse(parts[0]) + 1;
            ParametersMgr.inst.ap.seed = (uint) int.Parse(parts[1]) + 1;
        }
    }

    public void AppendMoveDataCSV(string oldRoute, string newRoute, int nodeU, int nodeV, int move, double cost)
    {
        if (addToDatabase)
        {
            string filePath = Path.Combine(directoryPath, fileNames[3]);
            File.AppendAllText(filePath, oldRoute
                    + ", " + newRoute
                    + ", " + nodeU
                    + ", " + nodeV
                    + ", " + move
                    + ", " + cost + "\n");
        }
    }

    public List<double> GetPreviousAverages(string fileName)
    {
        var values = new List<double>();

        string filePath = Path.Combine(directoryPath, fileName);

        using (var reader = new StreamReader(filePath))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] parts = line.Split(',');

                if (parts.Length < 2)
                    continue; // skip if there's no second column

                if (double.TryParse(parts[1], out double val))
                {
                    values.Add(val);
                }
                else
                {
                    throw new FormatException($"Could not parse '{parts[1]}' as an double.");
                }
            }
        }

        return values;
    }

    public bool CheckIfFileExists(string fileName)
    {
        Debug.Log(Path.Combine(directoryPath, fileName));
        return File.Exists(Path.Combine(directoryPath, fileName));
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
