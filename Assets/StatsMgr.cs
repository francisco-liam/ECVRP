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

    public List<double> CalculateGenerationAveragesOverRuns(List<List<double>> list)
    {
        List<double> result = new List<double>();

        for(int i = 0; i < ParametersMgr.inst.ap.nbIter; i++)
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
        averageTotalPopulationFitness.Add(new List<double>());
        averageFeasiblePopulationFitness.Add(new List<double>());
        averageInfeasiblePopulationFitness.Add(new List<double>());
        maxTotalPopulationFitness.Add(new List<double>());
        maxFeasiblePopulationFitness.Add(new List<double>());
        maxInfeasiblePopulationFitness.Add(new List<double>());
        minTotalPopulationCost.Add(new List<double>());
        minFeasiblePopulationCost.Add(new List<double>());
        minInfeasiblePopulationCost.Add(new List<double>());

    }

    //ran each generation
    public void RecordRunData(int runNum)
    {
        averageTotalPopulationFitness[runNum].Add(GetAverageTotalPopulationFitness());
        averageFeasiblePopulationFitness[runNum].Add(GetAverageFeasiblePopulationFitness());
        averageInfeasiblePopulationFitness[runNum].Add(GetAverageInfeasiblePopulationFitness());
        maxTotalPopulationFitness[runNum].Add(GetMaxTotalPopulationFitness());
        maxFeasiblePopulationFitness[runNum].Add(GetMaxFeasiblePopulationFitness());
        maxInfeasiblePopulationFitness[runNum].Add(GetMaxInfeasiblePopulationFitness());
        minTotalPopulationCost[runNum].Add(GetMinTotalPopulationCost());
        minFeasiblePopulationCost[runNum].Add(GetMinFeasiblePopulationCost());
        minInfeasiblePopulationCost[runNum].Add(GetMinInfeasiblePopulationCost());
    }

    public void InitValues()
    {
        seeds = new List<int>();
        bestCosts = new List<double>();
        speeds = new List<int>();
        averageTotalPopulationFitness = new List<List<double>>();
        averageFeasiblePopulationFitness = new List<List<double>>();
        averageInfeasiblePopulationFitness = new List<List<double>>();
        maxTotalPopulationFitness = new List<List<double>>();
        maxFeasiblePopulationFitness = new List<List<double>>();
        maxInfeasiblePopulationFitness = new List<List<double>>();
        minTotalPopulationCost = new List<List<double>>();
        minFeasiblePopulationCost = new List<List<double>>();
        minInfeasiblePopulationCost = new List<List<double>>();
    }
}
