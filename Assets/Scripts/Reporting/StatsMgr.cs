using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using System.IO;

public class StatsMgr : MonoBehaviour
{
    public static StatsMgr inst;
    public double bestCosts;
    public int speeds;
    public float times;
    public uint seeds;
    public List<List<double>> minFeasiblePopulationCost;
    public List<List<double>> avgFeasiblePopulationCost;

    private void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(Application.dataPath);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    double GetAverageTotalPopulationFitness()
    {
        if (PopulationMgr.inst.feasibleSubpop.Count + PopulationMgr.inst.infeasibleSubpop.Count == 0)
            return 0;

        double sumFitness = 0;

        foreach (Individual indiv in PopulationMgr.inst.feasibleSubpop)
            sumFitness += indiv.biasedFitness;
        foreach (Individual indiv in PopulationMgr.inst.infeasibleSubpop)
            sumFitness += indiv.biasedFitness;

        return sumFitness / (PopulationMgr.inst.feasibleSubpop.Count + PopulationMgr.inst.infeasibleSubpop.Count);
    }

    double GetAverageFeasiblePopulationFitness()
    {
        if (PopulationMgr.inst.feasibleSubpop.Count == 0)
            return 0;

        double sumFitness = 0;

        foreach (Individual indiv in PopulationMgr.inst.feasibleSubpop)
            sumFitness += indiv.biasedFitness;

        return sumFitness / PopulationMgr.inst.feasibleSubpop.Count;
    }

    double GetAverageInfeasiblePopulationFitness()
    {
        if (PopulationMgr.inst.infeasibleSubpop.Count == 0)
            return 0;

        double sumFitness = 0;

        foreach (Individual indiv in PopulationMgr.inst.infeasibleSubpop)
            sumFitness += indiv.biasedFitness;

        return sumFitness / PopulationMgr.inst.infeasibleSubpop.Count;
    }

    double GetMaxTotalPopulationFitness()
    {
        if (PopulationMgr.inst.feasibleSubpop.Count + PopulationMgr.inst.infeasibleSubpop.Count == 0)
            return 0;

        double maxFitness = 0;

        foreach (Individual indiv in PopulationMgr.inst.feasibleSubpop)
        {
            if(indiv.biasedFitness > maxFitness)
                maxFitness = indiv.biasedFitness;
        }
        foreach (Individual indiv in PopulationMgr.inst.infeasibleSubpop)
        {
            if (indiv.biasedFitness > maxFitness)
                maxFitness = indiv.biasedFitness;
        }

        return maxFitness;
    }

    double GetMaxFeasiblePopulationFitness()
    {
        if (PopulationMgr.inst.feasibleSubpop.Count == 0)
            return 0;

        double maxFitness = 0;

        foreach (Individual indiv in PopulationMgr.inst.feasibleSubpop)
        {
            if (indiv.biasedFitness > maxFitness)
                maxFitness = indiv.biasedFitness;
        }

        return maxFitness;
    }

    double GetMaxInfeasiblePopulationFitness()
    {
        if (PopulationMgr.inst.infeasibleSubpop.Count == 0)
            return 0;

        double maxFitness = 0;

        foreach (Individual indiv in PopulationMgr.inst.infeasibleSubpop)
        {
            if (indiv.biasedFitness > maxFitness)
                maxFitness = indiv.biasedFitness;
        }

        return maxFitness;
    }

    double GetMinTotalPopulationCost()
    {
        if (PopulationMgr.inst.feasibleSubpop.Count > 0 && PopulationMgr.inst.infeasibleSubpop.Count > 0)
            return Math.Min(GetMinFeasiblePopulationCost(), GetMinInfeasiblePopulationCost());
        else if (PopulationMgr.inst.feasibleSubpop.Count > 0)
            return GetMinFeasiblePopulationCost();
        else
            return GetMinInfeasiblePopulationCost();
    }

    double GetMinFeasiblePopulationCost()
    {
        Individual indiv = PopulationMgr.inst.GetBestFeasible();
        if(indiv == null) return 0;
        else return indiv.eval.penalizedCost;
    }

    double GetMinInfeasiblePopulationCost()
    {
        Individual indiv = PopulationMgr.inst.GetBestInfeasible();
        if (indiv == null) return 0;
        else return indiv.eval.penalizedCost;
    }

    double GetAverageFeasiblePopulationCost()
    {
        if (PopulationMgr.inst.feasibleSubpop.Count == 0)
            return 0;

        double sumFitness = 0;

        foreach (Individual indiv in PopulationMgr.inst.feasibleSubpop)
            sumFitness += indiv.eval.penalizedCost;

        return sumFitness / PopulationMgr.inst.feasibleSubpop.Count;
    }

    public void UpdateBestCostAndSpeedForRun(int runNum)
    {
        Individual bestFeas = PopulationMgr.inst.GetBestFeasible();
        if (bestFeas == null) return;
        if (bestCosts > bestFeas.eval.penalizedCost)
        {
            bestCosts = bestFeas.eval.penalizedCost;
            speeds = GeneticMgr.inst.nbIter;
        }
    }

    public static void ExtendLists(List<List<double>> lists)
    {
        if (lists == null || lists.Count == 0)
            return;

        // Step 1: Find the maximum list length
        int maxLength = 0;
        foreach (var list in lists)
        {
            if (list.Count > maxLength)
                maxLength = list.Count;
        }

        // Step 2: Extend each list to match maxLength
        foreach (var list in lists)
        {
            if (list.Count == 0)
                continue; // skip empty lists

            double lastValue = list[list.Count - 1];
            while (list.Count < maxLength)
            {
                list.Add(lastValue);
            }
        }
    }

    public List<double> CalculateGenerationAveragesOverRuns(List<List<double>> list)
    {
        ExtendLists(list);

        List<double> result = new List<double>();

        if (list.Count == 1)
            return list[0];

        for(int i = 0; i < list[0].Count; i++)
        {
            double sum = 0;
            sum += CVRPMain.inst.run * list[0][i];
            sum += list[1][i];

            result.Add(sum / (CVRPMain.inst.run + 1));
        }

        return result;
    }

    //assumes run # in main is initialized correctly
    public void InitRun()
    {
        seeds = ParametersMgr.inst.ap.seed;
        bestCosts = float.MaxValue;
        speeds = int.MaxValue;
        times = float.MaxValue;

        if (FileWriterMgr.inst.CheckIfFileExists(FileWriterMgr.inst.fileNames[1]))
        {
            minFeasiblePopulationCost.Add(FileWriterMgr.inst.GetPreviousAverages(FileWriterMgr.inst.fileNames[1]));
            Debug.Log("found");
        }      
        if (FileWriterMgr.inst.CheckIfFileExists(FileWriterMgr.inst.fileNames[2]))
            avgFeasiblePopulationCost.Add(FileWriterMgr.inst.GetPreviousAverages(FileWriterMgr.inst.fileNames[2]));

        minFeasiblePopulationCost.Add(new List<double>());
        avgFeasiblePopulationCost.Add(new List<double>());
    }

    //ran each generation
    public void RecordRunData()
    {
        minFeasiblePopulationCost[minFeasiblePopulationCost.Count-1].Add(GetMinFeasiblePopulationCost());
        avgFeasiblePopulationCost[avgFeasiblePopulationCost.Count-1].Add(GetAverageFeasiblePopulationCost());
    }

    public void InitValues()
    {
        minFeasiblePopulationCost = new List<List<double>>();
        avgFeasiblePopulationCost = new List<List<double>>();
    }
}
