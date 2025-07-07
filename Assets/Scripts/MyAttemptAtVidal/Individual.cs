using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class EvalIndiv
{
    public double penalizedCost = 0;       // Penalized cost of the solution
    public int nbRoutes = 0;               // Number of routes
    public double distance = 0;            // Total distance
    public double capacityExcess = 0;      // Sum of excess load in all routes
    public double durationExcess = 0;      // Sum of excess duration in all routes
    public bool isFeasible = false;		    // Feasibility status of the individual

    public EvalIndiv() 
    {
        penalizedCost = 0;       // Penalized cost of the solution
        nbRoutes = 0;               // Number of routes
        distance = 0;            // Total distance
        capacityExcess = 0;      // Sum of excess load in all routes
        durationExcess = 0;      // Sum of excess duration in all routes
        isFeasible = false;         // Feasibility status of the individual
    }

    public EvalIndiv(EvalIndiv other)
    {
        this.penalizedCost = other.penalizedCost;
        this.nbRoutes = other.nbRoutes;
        this.distance = other.distance;
        this.capacityExcess = other.capacityExcess;
        this.durationExcess = other.durationExcess;
        this.isFeasible = other.isFeasible;
    }
};

[System.Serializable]
public class Individual
{
    public EvalIndiv eval;                                                         // Solution cost parameters
    public int[] chromT;                                                // Giant tour representing the individual
    public List<List<int>> chromR;                               // For each vehicle, the associated sequence of deliveries (complete solution)
    public int[] successors;                                            // For each node, the successor in the solution (can be the depot 0)
    public int[] predecessors;                                      // For each node, the predecessor in the solution (can be the depot 0)
    public SortedList<double, List<Individual>> indivsPerProximity = new SortedList<double, List<Individual>>();   // The other individuals in the population, ordered by increasing proximity (the set container follows a natural ordering based on the first value of the pair)
    public double biasedFitness;														// Biased fitness of the solution

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EvaluateCompleteCost()
    {
        eval = new EvalIndiv();
        for (int r = 0; r < ParametersMgr.inst.nbVehicles; r++)
        {
            if (chromR[r].Count != 0)
            {
                double distance = ParametersMgr.inst.timeCost[0][chromR[r][0]];
                double load = ParametersMgr.inst.cli[chromR[r][0]].demand;
                //double service = params.cli[chromR[r][0]].serviceDuration;
                predecessors[chromR[r][0]] = 0;
                for(int i = 1; i < chromR[r].Count; i++)
                {
                    distance += ParametersMgr.inst.timeCost[chromR[r][i - 1]][chromR[r][i]];
                    load += ParametersMgr.inst.cli[chromR[r][i]].demand;
                    //service += params.cli[chromR[r][i]].serviceDuration;
                    predecessors[chromR[r][i]] = chromR[r][i - 1];
                    successors[chromR[r][i - 1]] = chromR[r][i];
                }
                successors[chromR[r][chromR[r].Count - 1]] = 0;
                distance += ParametersMgr.inst.timeCost[chromR[r][chromR[r].Count - 1]][0];
                eval.distance += distance;
                eval.nbRoutes++;
                if (load > ParametersMgr.inst.vehicleCapacity) eval.capacityExcess += load - ParametersMgr.inst.vehicleCapacity;
                if (distance /*+ service*/ > ParametersMgr.inst.durationLimit) eval.durationExcess += distance /*+ service*/ - ParametersMgr.inst.durationLimit;
            }
        }

        eval.penalizedCost = eval.distance + eval.capacityExcess * ParametersMgr.inst.penaltyCapacity + eval.durationExcess * ParametersMgr.inst.penaltyDuration;
        eval.isFeasible = (eval.capacityExcess < 0.00001 && eval.durationExcess < 0.00001);
    }

    public Individual()
    {
        eval = new EvalIndiv();
        successors = new int[ParametersMgr.inst.nbClients + 1];
        predecessors = new int[ParametersMgr.inst.nbClients + 1];
        chromR = new List<List<int>>();
        for(int i = 0; i < ParametersMgr.inst.nbVehicles; i++)
            chromR.Add(new List<int>());
        chromT = new int[ParametersMgr.inst.nbClients];
        for (int i = 0; i < ParametersMgr.inst.nbClients; i++)
            chromT[i] = i + 1;

        ParametersMgr.inst.ran.Shuffle(chromT);
        eval.penalizedCost = 1e30f;
        EvaluateCompleteCost();
    }

    public Individual(Individual other)
    {
        // Deep copy of eval
        eval = new EvalIndiv(other.eval); // Assumes EvalIndiv has its own copy constructor

        // Deep copy of chromT
        chromT = new int[other.chromT.Length];
        Array.Copy(other.chromT, chromT, other.chromT.Length);

        // Deep copy of chromR (List<List<int>>)
        chromR = new List<List<int>>(other.chromR.Count);
        foreach (var route in other.chromR)
        {
            chromR.Add(new List<int>(route));
        }

        // Deep copy of successors and predecessors
        successors = new int[other.successors.Length];
        Array.Copy(other.successors, successors, other.successors.Length);

        predecessors = new int[other.predecessors.Length];
        Array.Copy(other.predecessors, predecessors, other.predecessors.Length);

        // Deep copy of indivsPerProximity
        indivsPerProximity = new SortedList<double, List<Individual>>();
        foreach (var kvp in other.indivsPerProximity)
        {
            var listCopy = new List<Individual>(kvp.Value); // Shallow copy of list elements
            indivsPerProximity.Add(kvp.Key, listCopy);
        }

        // Copy of primitive
        biasedFitness = other.biasedFitness;
    }
}
