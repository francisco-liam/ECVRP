using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class StatsMgr : MonoBehaviour
{
    public static StatsMgr inst;
    public List<float> bestCosts;
    public List<int> speeds;
    public List<int> seeds;
    public List<List<float>> averageTotalPopulationFitness;
    public List<List<float>> averageFeasiblePopulationFitness;
    public List<List<float>> averageInfeasiblePopulationFitness;
    public List<List<float>> minTotalPopulationFitness;
    public List<List<float>> minFeasiblePopulationFitness;
    public List<List<float>> minInfeasiblePopulationFitness;
    public List<List<float>> maxTotalPopulationCost;
    public List<List<float>> maxFeasiblePopulationCost;
    public List<List<float>> maxInfeasiblePopulationCost;

    public string filename;

    private void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    float GetAverageTotalPopulationFitness()
    {
        if (PopulationMgr.inst.feasibleSubpop.Count + PopulationMgr.inst.infeasibleSubpop.Count == 0)
            return 0;

        float sumFitness = 0;

        foreach (Individual indiv in PopulationMgr.inst.feasibleSubpop)
            sumFitness += indiv.biasedFitness;
        foreach (Individual indiv in PopulationMgr.inst.infeasibleSubpop)
            sumFitness += indiv.biasedFitness;

        return sumFitness / (PopulationMgr.inst.feasibleSubpop.Count + PopulationMgr.inst.infeasibleSubpop.Count);
    }

    float GetAverageFeasiblePopulationFitness()
    {
        if (PopulationMgr.inst.feasibleSubpop.Count == 0)
            return 0;

        float sumFitness = 0;

        foreach (Individual indiv in PopulationMgr.inst.feasibleSubpop)
            sumFitness += indiv.biasedFitness;

        return sumFitness / PopulationMgr.inst.feasibleSubpop.Count;
    }

    float GetAverageInfeasiblePopulationFitness()
    {
        if (PopulationMgr.inst.infeasibleSubpop.Count == 0)
            return 0;

        float sumFitness = 0;

        foreach (Individual indiv in PopulationMgr.inst.infeasibleSubpop)
            sumFitness += indiv.biasedFitness;

        return sumFitness / PopulationMgr.inst.infeasibleSubpop.Count;
    }

    float GetMaxTotalPopulationFitness()
    {
        if (PopulationMgr.inst.feasibleSubpop.Count + PopulationMgr.inst.infeasibleSubpop.Count == 0)
            return 0;

        float maxFitness = 0;

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

    float GetMaxFeasiblePopulationFitness()
    {
        if (PopulationMgr.inst.feasibleSubpop.Count == 0)
            return 0;

        float maxFitness = 0;

        foreach (Individual indiv in PopulationMgr.inst.feasibleSubpop)
        {
            if (indiv.biasedFitness > maxFitness)
                maxFitness = indiv.biasedFitness;
        }

        return maxFitness;
    }

    float GetMaxInfeasiblePopulationFitness()
    {
        if (PopulationMgr.inst.infeasibleSubpop.Count == 0)
            return 0;

        float maxFitness = 0;

        foreach (Individual indiv in PopulationMgr.inst.infeasibleSubpop)
        {
            if (indiv.biasedFitness > maxFitness)
                maxFitness = indiv.biasedFitness;
        }

        return maxFitness;
    }

    float GetMinTotalPopulationCost()
    {
        if (PopulationMgr.inst.feasibleSubpop.Count > 0 && PopulationMgr.inst.infeasibleSubpop.Count > 0)
            return Mathf.Min(GetMinFeasiblePopulationCost(), GetMinInfeasiblePopulationCost());
        else if (PopulationMgr.inst.feasibleSubpop.Count > 0)
            return GetMinFeasiblePopulationCost();
        else
            return GetMinInfeasiblePopulationCost();
    }

    float GetMinFeasiblePopulationCost()
    {
        Individual indiv = PopulationMgr.inst.GetBestFeasible();
        if(indiv == null) return 0;
        else return indiv.eval.penalizedCost;
    }

    float GetMinInfeasiblePopulationCost()
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

    public List<float> CalculateGenerationAveragesOverRuns(List<List<float>> list)
    {
        List<float> result = new List<float>();

        for(int i = 0; i < CVRPMgr.inst.ap.nbIter; i++)
        {
            float sum = 0;
            foreach(List<float> run in list)
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
        averageTotalPopulationFitness.Add(new List<float>());
        averageFeasiblePopulationFitness.Add(new List<float>());
        averageInfeasiblePopulationFitness.Add(new List<float>());
        minTotalPopulationFitness.Add(new List<float>());
        minFeasiblePopulationFitness.Add(new List<float>());
        minInfeasiblePopulationFitness.Add(new List<float>());
        maxTotalPopulationCost.Add(new List<float>());
        maxFeasiblePopulationCost.Add(new List<float>());
        maxInfeasiblePopulationCost.Add(new List<float>());

    }

    public void InitValues()
    {
        seeds = new List<int>();
        bestCosts = new List<float>();
        speeds = new List<int>();
        averageTotalPopulationFitness = new List<List<float>>();
        averageFeasiblePopulationFitness = new List<List<float>>();
        averageInfeasiblePopulationFitness = new List<List<float>>();
        minTotalPopulationFitness = new List<List<float>>();
        minFeasiblePopulationFitness = new List<List<float>>();
        minInfeasiblePopulationFitness = new List<List<float>>();
        maxTotalPopulationCost = new List<List<float>>();
        maxFeasiblePopulationCost = new List<List<float>>();
        maxInfeasiblePopulationCost = new List<List<float>>();
    }
}
