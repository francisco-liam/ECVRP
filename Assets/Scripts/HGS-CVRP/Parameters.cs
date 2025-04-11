using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public int useSwapStar;		// Use SWAP* local search or not. Default value: 1. Only available when coordinates are provided.
};

public class Client
{
    public float coordX;          // Coordinate X
    public float coordY;          // Coordinate Y
    public float serviceDuration; // Service duration
    public float demand;          // Demand
    public int polarAngle;			// Polar angle of the client around the depot, measured in degrees and truncated for convenience
};

public class Parameters : MonoBehaviour
{
    public static Parameters inst;

    /* PARAMETERS OF THE GENETIC ALGORITHM */
    public bool verbose;                       // Controls verbose level through the iterations
    public AlgorithmParameters ap;             // Main parameters of the HGS algorithm

    /* ADAPTIVE PENALTY COEFFICIENTS */
    public float penaltyCapacity;             // Penalty for one unit of capacity excess (adapted through the search)
    public float penaltyDuration;             // Penalty for one unit of duration excess (adapted through the search)

    /* START TIME OF THE ALGORITHM */
    //clock_t startTime;                  // Start time of the optimization (set when Parameters.inst is constructed)

    /* DATA OF THE PROBLEM INSTANCE */
    public bool isDurationConstraint;                              // Indicates if the problem includes duration constraints
    public int nbClients;                                          // Number of clients (excluding the depot)
    public int nbVehicles;                                     // Number of vehicles
    public float durationLimit;                                   // Route duration limit
    public float vehicleCapacity;                                 // Capacity limit
    public float totalDemand;                                 // Total demand required by the clients
    public float maxDemand;                                       // Maximum demand of a client
    public float maxDist;                                         // Maximum distance between two clients
    public List<Client> cli;                                // Vector containing information on each client
    public List<List<float>> timeCost;	// Distance matrix
	public List<List<int>> correlatedVertices;   // Neighborhood restrictions: For each client, list of nearby customers
    public  bool areCoordinatesProvided;                            // Check if valid coordinates are provided

    // Initialization from a given data set
    Parameters(List<float> x_coords,
		List<float> y_coords,
		float[,] dist_mtx,
		List<float> service_time,
		List<float> demands,
        float vehicleCapacity,
        float durationLimit,
        int nbVeh,
        bool isDurationConstraint,
        bool verbose){
        
        
    }
}
