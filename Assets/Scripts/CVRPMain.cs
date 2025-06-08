using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CVRPMain : MonoBehaviour
{
    public static CVRPMain inst;

    public int seed;
    public int run;
    public int maxRuns;

    private void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(seed);
        CVRPMgr.inst.Init();
        GraphMgr.inst.CreateGraph();
        StatsMgr.inst.InitValues();
        StatsMgr.inst.InitRun();
        LocalSearchMgr.inst.InitValues();
        SplitMgr.inst.InitValues();
        PopulationMgr.inst.InitValues();
        GeneticMgr.inst.Init();
        run = 0;
    }

    // Update is called once per frame
    bool ran = false;
    void Update()
    {
        if(Input.GetKeyUp(KeyCode.Alpha1))
        {
            foreach (List<int> route in PopulationMgr.inst.GetBestFeasible().chromR)
                Debug.Log(BasicsChecking.PrintList(route));
            
        }
    }
}
