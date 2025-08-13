using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class StatsMgr : MonoBehaviour
{
    public static StatsMgr inst;
    public List<double> bestCosts;
    public List<int> speeds;
    public List<float> times;
    public List<int> seeds;
    public List<List<double>> averageTotalPopulationFitness;
    public List<List<double>> averageFeasiblePopulationFitness;
    public List<List<double>> averageInfeasiblePopulationFitness;
    public List<List<double>> maxTotalPopulationFitness;
    public List<List<double>> maxFeasiblePopulationFitness;
    public List<List<double>> maxInfeasiblePopulationFitness;
    public List<List<double>> minTotalPopulationCost;
    public List<List<double>> minFeasiblePopulationCost;
    public List<List<double>> minInfeasiblePopulationCost;
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
        if (bestCosts[runNum] > bestFeas.eval.penalizedCost)
        {
            bestCosts[runNum] = bestFeas.eval.penalizedCost;
            speeds[runNum] = GeneticMgr.inst.nbIter;
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

        for(int i = 0; i < list[0].Count; i++)
        {
            double sum = 0;
            foreach(List<double> run in list)
                sum += run[i];

            result.Add(sum / list.Count);
        }

        return result;
    }

    //assumes run # in main is initialized correctly
    public void InitRun()
    {
        seeds.Add(CVRPMain.inst.seed);
        bestCosts.Add(float.MaxValue);
        speeds.Add(int.MaxValue);
        times.Add(float.MaxValue);
        averageTotalPopulationFitness.Add(new List<double>());
        averageFeasiblePopulationFitness.Add(new List<double>());
        averageInfeasiblePopulationFitness.Add(new List<double>());
        maxTotalPopulationFitness.Add(new List<double>());
        maxFeasiblePopulationFitness.Add(new List<double>());
        maxInfeasiblePopulationFitness.Add(new List<double>());
        minTotalPopulationCost.Add(new List<double>());
        minFeasiblePopulationCost.Add(new List<double>());
        minInfeasiblePopulationCost.Add(new List<double>());
        avgFeasiblePopulationCost.Add(new List<double>());
    }

    //ran each generation
    public void RecordRunData(int runNum)
    {
        minFeasiblePopulationCost[runNum].Add(GetMinFeasiblePopulationCost());
        avgFeasiblePopulationCost[runNum].Add(GetAverageFeasiblePopulationCost());
    }

    public void InitValues()
    {
        seeds = new List<int>();
        bestCosts = new List<double>();
        speeds = new List<int>();
        times = new List<float>();
        averageTotalPopulationFitness = new List<List<double>>();
        averageFeasiblePopulationFitness = new List<List<double>>();
        averageInfeasiblePopulationFitness = new List<List<double>>();
        maxTotalPopulationFitness = new List<List<double>>();
        maxFeasiblePopulationFitness = new List<List<double>>();
        maxInfeasiblePopulationFitness = new List<List<double>>();
        minTotalPopulationCost = new List<List<double>>();
        minFeasiblePopulationCost = new List<List<double>>();
        minInfeasiblePopulationCost = new List<List<double>>();
        avgFeasiblePopulationCost = new List<List<double>>();
    }
}
