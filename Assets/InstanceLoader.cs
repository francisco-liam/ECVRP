using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.UI;
using UnityEditor;
using System.Linq;

[System.Serializable]
public class FileInfoObject
{
    public string fullPath;
    public string fileName;
    public string relativePath; // relative path inside Resources (no extension)

    public FileInfoObject(string fullPath, string fileName, string relativePath)
    {
        this.fullPath = fullPath;
        this.fileName = fileName;
        this.relativePath = relativePath;
    }
}

public class InstanceLoader : MonoBehaviour
{
    public static InstanceLoader inst;

    public string resourceFolder = "CVRPs"; // inside Resources
    public string fileExtension = ".vrp";

    public List<FileInfoObject> fileList = new List<FileInfoObject>();
    public List<TextAsset> textAssets = new List<TextAsset>();

    public List<TMP_Dropdown> instanceDropdowns;
    public bool inProgress;

    public void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        LoadFiles();
        GenerateDropdown();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void LoadFiles()
    {
        // Load all TextAssets in Resources/CVRPs with the given extension
        textAssets = Resources.LoadAll<TextAsset>("CVRPs/Vrp-Set-X").ToList<TextAsset>();
        Debug.Log($"Loaded {textAssets.Count} {fileExtension} file(s) from Resources/CVRPs/Vrp-Set-X");
    }



    void GenerateDropdown()
    {
        List<string> instanceNames = new List<string>();
        instanceNames.Add("Choose Instance");
        foreach (TextAsset obj in textAssets)
        {
            instanceNames.Add(obj.name);
        }

        foreach (TMP_Dropdown instanceDropdown in instanceDropdowns)
        {
            instanceDropdown.ClearOptions();
            instanceDropdown.AddOptions(instanceNames);
        }
    }

    public void RunStarter()
    {
        inProgress = true;
    }
}
