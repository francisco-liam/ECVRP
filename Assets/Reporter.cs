using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Reporter : MonoBehaviour
{
    public TextMeshProUGUI textDisplay;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        string output = "run: " + CVRPMain.inst.run +
            "\nnbIter: " + GeneticMgr.inst.nbIter +
            "\nnbIterNonProd:" + GeneticMgr.inst.nbIterNonProd;
        if(PopulationMgr.inst.GetBestFeasible() != null)
            output += "\nBest Cost: " + PopulationMgr.inst.GetBestFeasible().eval.penalizedCost;

        textDisplay.text = output;
    }
}
