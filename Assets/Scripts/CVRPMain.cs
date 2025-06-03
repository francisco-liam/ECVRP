using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CVRPMain : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        CVRPMgr.inst.Init();
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
            if (!ran)
            {
                //ran = true;
                //SplitMgr.inst.Init();
                //LocalSearchMgr.inst.Init();
                //PopulationMgr.inst.Init();

            }
        }
    }
}
