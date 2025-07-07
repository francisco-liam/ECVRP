using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CVRPMain : MonoBehaviour
{
    public static CVRPMain inst;

    public int seed;
    public int run;
    public int maxRuns;

    public UnityEngine.Object moves;

    private void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        InstanceCVRPMgr.inst.ReadInstance();
        ParametersMgr.inst.Init();
        GraphMgr.inst.CreateGraph();
        StatsMgr.inst.InitValues();
        StatsMgr.inst.InitRun();
        LocalSearchMgr.inst.InitValues();
        SplitMgr.inst.InitValues();
        PopulationMgr.inst.InitValues();
        GeneticMgr.inst.Init();
        LocalSearchMgr.inst.Init();

        /*Individual randomIndiv = new Individual();
        SplitMgr.inst.GeneralSplit(randomIndiv, ParametersMgr.inst.nbVehicles);
        LocalSearchMgr.inst.Run(randomIndiv, ParametersMgr.inst.penaltyCapacity, ParametersMgr.inst.penaltyDuration);
        
        /string path = AssetDatabase.GetAssetPath(moves);
        List<Tuple<int, int, int>> tuples = ReadTuplesFromFile(path, 154);

        LocalSearchMgr.inst.penaltyCapacityLS = ParametersMgr.inst.penaltyCapacity;
        LocalSearchMgr.inst.penaltyDurationLS = ParametersMgr.inst.penaltyDuration;
        LocalSearchMgr.inst.LoadIndividual(randomIndiv);

        foreach(Tuple<int, int, int> tuple in tuples)
            LocalSearchMgr.inst.PerformMove(tuple.Item1, tuple.Item2, tuple.Item3);

        Debug.Log(LocalSearchMgr.inst.PerformMove(7, 74, 5));

        LocalSearchMgr.inst.ExportIndividual(randomIndiv);

        Debug.Log(BasicsChecking.PrintList(randomIndiv.chromT));

        foreach (List<int> route in randomIndiv.chromR)
            Debug.Log(BasicsChecking.PrintList(route));*/
        run = 0;
    }

    public static List<Tuple<int, int, int>> ReadTuplesFromFile(string path, int x)
    {
        var tuples = new List<Tuple<int, int, int>>();

        using (var reader = new StreamReader(path))
        {
            int lineCount = 0;
            while (!reader.EndOfStream && lineCount < x)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                    throw new FormatException($"Line {lineCount + 1} does not contain 3 numbers: \"{line}\"");

                if (int.TryParse(parts[0], out int a) &&
                    int.TryParse(parts[1], out int b) &&
                    int.TryParse(parts[2], out int c))
                {
                    tuples.Add(Tuple.Create(a, b, c));
                }
                else
                {
                    throw new FormatException($"Line {lineCount + 1} has invalid integer format: \"{line}\"");
                }

                lineCount++;
            }
        }

        return tuples;
    }

    // Update is called once per frame
    bool ran = false;
    void Update()
    { 
        /*if(Input.GetKeyUp(KeyCode.Alpha1))
        {
            foreach (List<int> route in PopulationMgr.inst.GetBestFeasible().chromR)
                Debug.Log(BasicsChecking.PrintList(route));
            
        }*/
    }
}
