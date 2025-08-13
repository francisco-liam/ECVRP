using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Image;

public class CVRPRunner
{
    public FileWriterMgr FileWriterMgr;
    public InstanceCVRPMgr InstanceCVRPMgr;
    public ParametersMgr ParametersMgr;
    public GraphMgr GraphMgr;
    public StatsMgr StatsMgr;
    public LocalSearchMgr LocalSearchMgr;
    public SplitMgr SplitMgr;
    public PopulationMgr PopulationMgr;
    public GeneticMgr GeneticMgr;

    public CVRPRunner()
    {
        FileWriterMgr = new FileWriterMgr();
        InstanceCVRPMgr = new InstanceCVRPMgr();
        ParametersMgr = new ParametersMgr();
        GraphMgr = new GraphMgr();
        StatsMgr = new StatsMgr();
        LocalSearchMgr = new LocalSearchMgr();
        SplitMgr = new SplitMgr();
        PopulationMgr = new PopulationMgr();
        GeneticMgr = new GeneticMgr();
    }
}

public class CVRPMain : MonoBehaviour
{
    public static CVRPMain inst;

    public int seed;
    public int run;
    public int maxRuns;
    public bool graph;
    public TMP_Dropdown dropdown;
    public GameObject menu;

    public UnityEngine.Object moves;

    private void Awake()
    {
        inst = this;
    }

    public List<TextAsset> assets;

    // Start is called before the first frame update
    void Start()
    {
        QualitySettings.vSyncCount = 0; // Disable VSync
        assets = new List<TextAsset>();
        assets.Add(null);
        assets.Add(null);
        assets.Add(null);
        assets.Add(null);
        assets.Add(null);
        //InstanceCVRPMgr.inst.file = assets[0];
        //FileWriterMgr.inst.instanceName = assets[0].name;
        //StartGA();
    }

    public void StartGA()
    {
        FileWriterMgr.inst.instanceName = assets[instance].name;
        FileWriterMgr.inst.Init();
        InstanceCVRPMgr.inst.ReadInstance();
        ParametersMgr.inst.Init();
        if(graph)
            GraphMgr.inst.CreateGraph();
        StatsMgr.inst.InitValues();
        StatsMgr.inst.InitRun();
        LocalSearchMgr.inst.InitValues();
        SplitMgr.inst.InitValues();
        PopulationMgr.inst.InitValues();
        GeneticMgr.inst.Init();
        run = 0;
        maxRuns = 10;
        GeneticMgr.inst.running = true;
        menu.SetActive(false);
    }

    // Update is called once per frame
    public int instance = 0;
    bool ran = false;
    void Update()
    {
        if (GeneticMgr.inst.done && instance < assets.Count - 1)
        {
            instance++;
            run = 0;
            GeneticMgr.inst.done = false;
            InstanceCVRPMgr.inst.file = assets[instance];
            ParametersMgr.inst.ap.seed = 0;
            StartGA();
        }
    }
}
