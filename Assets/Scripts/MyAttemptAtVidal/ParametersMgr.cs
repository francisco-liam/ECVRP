using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AlgorithmParameters
{

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
    public bool useSetNbOfIter;

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
public class Client
{
    public double coordX;          // Coordinate X
    public double coordY;          // Coordinate Y
    public double serviceDuration; // Service duration
    public double demand;          // Demand
    public int polarAngle;			// Polar angle of the client around the depot, measured in degrees and truncated for convenience
}

public class ParametersMgr : MonoBehaviour
{
    public static ParametersMgr inst;

    /* PARAMETERS OF THE GENETIC ALGORITHM */
    public bool verbose;                       // Controls verbose level through the iterations
    public AlgorithmParameters ap = new AlgorithmParameters();             // Main parameters of the HGS algorithm

    /* ADAPTIVE PENALTY COEFFICIENTS */
    public double penaltyCapacity;             // Penalty for one unit of capacity excess (adapted through the search)
    public double penaltyDuration;             // Penalty for one unit of duration excess (adapted through the search)

    /* START TIME OF THE ALGORITHM */
    public float startTime;                  // Start time of the optimization (set when Params is constructed)

    /* RANDOM NUMBER GENERATOR */
    //std::minstd_rand ran;               // Using the fastest and simplest LCG. The quality of random numbers is not critical for the LS, but speed is

    /* DATA OF THE PROBLEM INSTANCE */
    public bool isDurationConstraint;                              // Indicates if the problem includes duration constraints
    public int nbClients;                                          // Number of clients (excluding the depot)
    public int nbVehicles = int.MaxValue;                                     // Number of vehicles
    public double durationLimit;                                   // Route duration limit
    public double vehicleCapacity;                                 // Capacity limit
    public double totalDemand;                                 // Total demand required by the clients
    public double maxDemand;                                       // Maximum demand of a client
    public double maxDist;                                         // Maximum distance between two clients
    public List<Client> cli;                                // Vector containing information on each client
    public double[][] timeCost;  // Distance matrix
    public List<List<int>> correlatedVertices;   // Neighborhood restrictions: For each client, list of nearby customers
    public bool areCoordinatesProvided;                            // Check if valid coordinates are provided

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

    public void Init()
    {
        List<double> x_coords = InstanceCVRPMgr.inst.x_coords;
        List<double> y_coords = InstanceCVRPMgr.inst.y_coords;
        List<double> service_time = InstanceCVRPMgr.inst.service_time;
        List<double> demands = InstanceCVRPMgr.inst.demands;

        isDurationConstraint = InstanceCVRPMgr.inst.isDurationConstraint;
        durationLimit = InstanceCVRPMgr.inst.durationLimit;
        timeCost = InstanceCVRPMgr.inst.dist_mtx;
        vehicleCapacity = InstanceCVRPMgr.inst.vehicleCapacity;

        startTime = Time.realtimeSinceStartup;

        nbClients = InstanceCVRPMgr.inst.demands.Count - 1;
        totalDemand = 0;
        maxDemand = 0;

        areCoordinatesProvided = (demands.Count == x_coords.Count) && 
            (demands.Count == y_coords.Count);

        cli = new List<Client>();
        for (int i = 0; i <= nbClients; i++)
        {
            cli.Add(new Client());
            // If useSwapStar==false, x_coords and y_coords may be empty.
            if (ap.useSwapStar == true && areCoordinatesProvided)
            {
                cli[i].coordX = x_coords[i];
                cli[i].coordY = y_coords[i];
                cli[i].polarAngle = CircleSector.positive_mod(
                    (int) (32768 * Math.Atan2(cli[i].coordY - cli[0].coordY, cli[i].coordX - cli[0].coordX) / Math.PI));
            }
            else
            {
                cli[i].coordX = 0.0;
                cli[i].coordY = 0.0;
                cli[i].polarAngle = 0;
            }

            cli[i].serviceDuration = service_time[i];
            cli[i].demand = demands[i];
            if (cli[i].demand > maxDemand) maxDemand = cli[i].demand;
            totalDemand += cli[i].demand;
        }

        if (verbose && !ap.useSwapStar && !areCoordinatesProvided)
            Debug.Log("----- NO COORDINATES HAVE BEEN PROVIDED, SWAP* NEIGHBORHOOD WILL BE DEACTIVATED BY DEFAULT");

        // Default initialization if the number of vehicles has not been provided by the user
        if (nbVehicles == int.MaxValue)
        {
            nbVehicles = (int)Math.Ceiling(1.3 * totalDemand / vehicleCapacity) + 3;  // Safety margin: 30% + 3 more vehicles than the trivial bin packing LB
            if (verbose)
                Debug.Log($"----- FLEET SIZE WAS NOT SPECIFIED: DEFAULT INITIALIZATION TO {nbVehicles} VEHICLES");
        }
        else
        {
            if (verbose)
                Debug.Log($"----- FLEET SIZE SPECIFIED: SET TO {nbVehicles} VEHICLES");
        }

        // Calculation of the maximum distance
        maxDist = 0;
        for (int i = 0; i <= nbClients; i++)
            for (int j = 0; j <= nbClients; j++)
                if (timeCost[i][j] > maxDist) maxDist = timeCost[i][j];

        // Calculation of the correlated vertices for each customer (for the granular restriction)
        correlatedVertices = new List<List<int>>();
        List<HashSet<int>> setCorrelatedVertices = new List<HashSet<int>>();
        List<Tuple<double, int>> orderProximity = new List<Tuple<double, int>>();
        for (int i = 0; i < nbClients + 1; i++)
        {
            correlatedVertices.Add(new List<int>());
            setCorrelatedVertices.Add(new HashSet<int>());
        }

        for (int i = 1; i <= nbClients; i++)
        {
            orderProximity.Clear();
            for (int j = 1; j <= nbClients; j++)
                if (i != j) orderProximity.Add(new Tuple<double, int>(timeCost[i][j], j));
            orderProximity.Sort();

            for (int j = 0; j < Math.Min(ap.nbGranular, nbClients - 1); j++)
            {
                setCorrelatedVertices[i].Add(orderProximity[j].Item2);
                setCorrelatedVertices[orderProximity[j].Item2].Add(i);
                
            }
        }

        // Filling the vector of correlated vertices
        for (int i = 1; i <= nbClients; i++)
            foreach (int x in setCorrelatedVertices[i])
                correlatedVertices[i].Add(x);

        // A reasonable scale for the initial values of the penalties
        penaltyDuration = 1;
        penaltyCapacity = Math.Max(0.1, Math.Min(1000, maxDist / maxDemand));

        if (verbose)
            Debug.Log($"----- INSTANCE SUCCESSFULLY LOADED WITH {nbClients} CLIENTS AND {nbVehicles} VEHICLES");

    }
}
