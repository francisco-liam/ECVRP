using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AlgorithmParameters {

    public int nbGranular;         // Granular search parameter, limits the number of moves in the RI local search
    public int mu;                 // Minimum population size
    public int lambda;             // Number of solutions created before reaching the maximum population size (i.e., generation size)
    public int nbElite;            // Number of elite individuals
    public int nbClose;            // Number of closest solutions/individuals considered when calculating diversity contribution

    public int nbIterPenaltyManagement;  // Number of iterations between penalty updates
    public float targetFeasible;        // Reference proportion for the number of feasible individuals, used for the adaptation of the penalty parameters
    public float penaltyDecrease;       // Multiplier used to decrease penalty parameters if there are sufficient feasible individuals
    public float penaltyIncrease;       // Multiplier used to increase penalty parameters if there are insufficient feasible individuals

    public int seed;               // Random seed. Default value: 0
    public int nbIter;             // Nb iterations without improvement until termination (or restart if a time limit is specified). Default value: 20,000 iterations
    public int nbIterTraces;       // Number of iterations between traces display during HGS execution
    public float timeLimit;       // CPU time limit until termination in seconds. Default value: 0 (i.e., inactive)
    public bool useSwapStar;		// Use SWAP* local search or not. Default value: 1. Only available when coordinates are provided.

    public AlgorithmParameters()
    {
        nbGranular = 20;
        mu = 25;
        lambda = 40;
        nbElite = 4;
        nbClose = 5;

        nbIterPenaltyManagement = 100;
        targetFeasible = 0.2f;
        penaltyDecrease = 0.85f;
        penaltyIncrease = 1.2f;

        seed = 0;
        nbIter = 20000;
        nbIterTraces = 500;
        timeLimit = 0;
        useSwapStar = true;
    }
}

[System.Serializable]
public class CVRPNode
{
    public int nodeNumber;
    public Vector2 coordinate;
    public int polarAngle;
    public float demand;
    public bool isDepot;
    public float serviceDuration = 0;
}

[System.Serializable]
public class CVRPProblem
{
    public List<CVRPNode> nodes;
    public List<CVRPNode> customerNodes;
    public float[,] adjacencyMatrix;
    public int vehicles;
    public int customers;
    public float capacity;
    
    public void CreateAdjacencyMatrix()
    {
        adjacencyMatrix = new float[nodes.Count, nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = 0; j < i; j++)
            {
                adjacencyMatrix[i, j] = Mathf.Round(Vector2.Distance(nodes[i].coordinate, nodes[j].coordinate));
                adjacencyMatrix[j, i] = adjacencyMatrix[i, j];
            }
        }

        string log = "";
        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = 0; j < nodes.Count; j++)
            {
                log += adjacencyMatrix[i, j] + " ";
            }
            log += "\n";
        }
        Debug.Log(log);
    }
}

public class CVRPMgr : MonoBehaviour
{
    public static CVRPMgr inst;

    /* PARAMETERS OF THE GENETIC ALGORITHM */
    public AlgorithmParameters ap = new AlgorithmParameters(); // Main parameters of the HGS algorithm

    /* ADAPTIVE PENALTY COEFFICIENTS */
    public float penaltyCapacity;   // Penalty for one unit of capacity excess (adapted through the search)
    public float penaltyDuration;   // Penalty for one unit of duration excess (adapted through the search)

    /* START TIME OF THE ALGORITHM */
    public float startTime; // Start time of the optimization (set when Params is constructed)

    /* DATA OF THE PROBLEM INSTANCE */
    public CVRPProblem problem;
    public float durationLimit = 1e30f; // Route duration limit
    public float totalDemand; // Total demand required by the clients
    public float maxDist;   // Maximum distance between two clients
    public float maxDemand; // Maximum demand of a client
    public List<List<int>> correlatedVertices;	// Neighborhood restrictions: For each client, list of nearby customers

    // Start is called before the first frame update
    void Awake()
    {
        inst = this;

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Init()
    {
        startTime = Time.realtimeSinceStartup;

        problem = DataMgr.inst.ReadCVRPFile();
        maxDist = 0;
        for (int i = 0; i <= problem.customers; i++)
            for (int j = 0; j <= problem.customers; j++)
                if (problem.adjacencyMatrix[i, j] > maxDist) maxDist = problem.adjacencyMatrix[i, j];
        maxDemand = 0;
        foreach(CVRPNode customer in problem.nodes)
        {
            if(customer.demand > maxDemand) maxDemand = customer.demand;
            totalDemand += customer.demand;

        }

        penaltyDuration = 1;
        penaltyCapacity = Mathf.Max(0.1f, Mathf.Min(1000, maxDist / maxDemand));

        // Calculation of the correlated vertices for each customer (for the granular restriction)
        correlatedVertices = new List<List<int>>();
        List<HashSet<int>> setCorrelatedVertices = new List<HashSet<int>>();
        List<Tuple<float, int>> orderProximity = new List<Tuple<float, int>>();
        for (int i = 0; i < problem.customers + 1; i++)
        {
            correlatedVertices.Add(new List<int>());
            setCorrelatedVertices.Add(new HashSet<int>());
        }

        for (int i = 1;  i <= problem.customers; i++)
        {
            orderProximity.Clear();
            for(int j = 1; j <= problem.customers; j++)
                if(i != j) orderProximity.Add(new Tuple<float, int>(problem.adjacencyMatrix[i, j], j));
            orderProximity.Sort();

            for(int j = 0; j < Mathf.Min(ap.nbGranular, problem.customers - 1); j++)
            {
                setCorrelatedVertices[i].Add(orderProximity[j].Item2);
                setCorrelatedVertices[orderProximity[j].Item2].Add(i);
            }
        }

        // Filling the vector of correlated vertices
        for (int i = 1; i <= problem.customers; i++)
            foreach (int x in setCorrelatedVertices[i])
                correlatedVertices[i].Add(x);
    }
}
