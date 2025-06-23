using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
