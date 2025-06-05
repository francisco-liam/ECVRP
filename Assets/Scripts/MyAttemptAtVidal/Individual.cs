using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class EvalIndiv
{
    public float penalizedCost = 0;       // Penalized cost of the solution
    public int nbRoutes = 0;               // Number of routes
    public float distance = 0;            // Total distance
    public float capacityExcess = 0;      // Sum of excess load in all routes
    public float durationExcess = 0;      // Sum of excess duration in all routes
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
    public SortedList<float, List<Individual>> indivsPerProximity = new SortedList<float, List<Individual>>();   // The other individuals in the population, ordered by increasing proximity (the set container follows a natural ordering based on the first value of the pair)
    public float biasedFitness;														// Biased fitness of the solution

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
        for (int r = 0; r < CVRPMgr.inst.problem.vehicles; r++)
        {
            if (chromR[r].Count != 0)
            {
                float distance = CVRPMgr.inst.problem.adjacencyMatrix[0, chromR[r][0]];
                float load = CVRPMgr.inst.problem.nodes[chromR[r][0]].demand;
                //double service = params.cli[chromR[r][0]].serviceDuration;
                predecessors[chromR[r][0]] = 0;
                for(int i = 1; i < chromR[r].Count; i++)
                {
                    distance += CVRPMgr.inst.problem.adjacencyMatrix[chromR[r][i - 1], chromR[r][i]];
                    load += CVRPMgr.inst.problem.nodes[chromR[r][i]].demand;
                    //service += params.cli[chromR[r][i]].serviceDuration;
                    predecessors[chromR[r][i]] = chromR[r][i - 1];
                    successors[chromR[r][i - 1]] = chromR[r][i];
                }
                successors[chromR[r][chromR[r].Count - 1]] = 0;
                distance += CVRPMgr.inst.problem.adjacencyMatrix[chromR[r][chromR[r].Count - 1], 0];
                eval.distance += distance;
                eval.distance = Mathf.Round(eval.distance);
                eval.nbRoutes++;
                if (load > CVRPMgr.inst.problem.capacity) eval.capacityExcess += load - CVRPMgr.inst.problem.capacity;
                if (distance /*+ service*/ > CVRPMgr.inst.durationLimit) eval.durationExcess += distance /*+ service*/ - CVRPMgr.inst.durationLimit;
            }
        }

        eval.penalizedCost = eval.distance + eval.capacityExcess * CVRPMgr.inst.penaltyCapacity + eval.durationExcess * CVRPMgr.inst.penaltyDuration;
        eval.isFeasible = (eval.capacityExcess < float.Epsilon && eval.durationExcess < float.Epsilon);
    }

    public Individual()
    {
        eval = new EvalIndiv();
        successors = new int[CVRPMgr.inst.problem.customers + 1];
        predecessors = new int[CVRPMgr.inst.problem.customers + 1];
        chromR = new List<List<int>>();
        for(int i = 0; i < CVRPMgr.inst.problem.vehicles; i++)
            chromR.Add(new List<int>());
        chromT = new int[CVRPMgr.inst.problem.customers];
        for (int i = 0; i < CVRPMgr.inst.problem.customers; i++)
            chromT[i] = i + 1;

        // Shuffle using Fisher–Yates algorithm with Unity's Random
        for (int i = chromT.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = chromT[i];
            chromT[i] = chromT[j];
            chromT[j] = temp;
        }
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
        indivsPerProximity = new SortedList<float, List<Individual>>();
        foreach (var kvp in other.indivsPerProximity)
        {
            var listCopy = new List<Individual>(kvp.Value); // Shallow copy of list elements
            indivsPerProximity.Add(kvp.Key, listCopy);
        }

        // Copy of primitive
        biasedFitness = other.biasedFitness;
    }
}
