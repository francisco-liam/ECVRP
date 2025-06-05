using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CVRPMain : MonoBehaviour
{
    public int seed;

    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(seed);
        CVRPMgr.inst.Init();
        GraphMgr.inst.CreateGraph();
        LocalSearchMgr.inst.InitValues();
        SplitMgr.inst.InitValues();
        PopulationMgr.inst.InitValues();
        GeneticMgr.inst.Init();
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
