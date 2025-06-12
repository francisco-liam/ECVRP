using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

[System.Serializable]
public class Node
{
    public bool isDepot;                       // Tells whether this node represents a depot or not
    public int cour;                           // Node index
    public int position;                       // Position in the route
    public int whenLastTestedRI;               // "When" the RI moves for this node have been last tested
    public Node next;                     // Next node in the route order
    public Node prev;                     // Previous node in the route order
    public Route route;                       // Pointer towards the associated route
    public float cumulatedLoad;               // Cumulated load on this route until the customer (including itself)
    public float cumulatedTime;               // Cumulated time on this route until the customer (including itself)
    public float cumulatedReversalDistance;   // Difference of cost if the segment of route (0...cour) is reversed (useful for 2-opt moves with asymmetric problems)
    public float deltaRemoval;				// Difference of cost in the current route if the node is removed (used in SWAP*)
};

[System.Serializable]
// Structure containing a route
public class Route
{
    public int cour;                           // Route index
    public int nbCustomers;                    // Number of customers visited in the route
    public int whenLastModified;               // "When" this route has been last modified
    public int whenLastTestedSWAPStar;         // "When" the SWAP* moves for this route have been last tested
    public Node depot;                        // Pointer to the associated depot
    public float duration;                    // Total time on the route
    public float load;                        // Total load on the route
    public float reversalDistance;            // Difference of cost if the route is reversed
    public float penalty;                     // Current sum of load and duration penalties
    public float polarAngleBarycenter;        // Polar angle of the barycenter of the route
    public CircleSector sector;				// Circle sector associated to the set of customers

    public Route()
    {
        sector = new CircleSector();
    }
};

// Structured used to keep track of the best SWAP* move
public class SwapStarElement
{
    public float moveCost = 1e30f;
    public Node U = null;
    public Node bestPositionU = null;
    public Node V = null;
    public Node bestPositionV = null;
};

// Structure used in SWAP* to remember the three best insertion positions of a customer in a given route
public class ThreeBestInsert
{
    public int whenLastCalculated;
    public float[] bestCost = new float[3];
    public Node[] bestLocation = new Node[3];

    public void CompareAndAdd(float costInsert, Node placeInsert)
    {
        if (costInsert >= bestCost[2]) return;
        else if (costInsert >= bestCost[1])
        {
            bestCost[2] = costInsert; bestLocation[2] = placeInsert;
        }
        else if (costInsert >= bestCost[0])
        {
            bestCost[2] = bestCost[1]; bestLocation[2] = bestLocation[1];
            bestCost[1] = costInsert; bestLocation[1] = placeInsert;
        }
        else
        {
            bestCost[2] = bestCost[1]; bestLocation[2] = bestLocation[1];
            bestCost[1] = bestCost[0]; bestLocation[1] = bestLocation[0];
            bestCost[0] = costInsert; bestLocation[0] = placeInsert;
        }
    }

    // Resets the structure (no insertion calculated)
    public void Reset()
    {
        bestCost[0] = 1e30f; bestLocation[0] = null;
        bestCost[1] = 1e30f; bestLocation[1] = null;
        bestCost[2] = 1e30f; bestLocation[2] = null;
    }

    public ThreeBestInsert() { Reset(); }
};

public class LocalSearchMgr : MonoBehaviour
{
    public static LocalSearchMgr inst;

    bool searchCompleted;
    int nbMoves;				// Total number of moves (RI and SWAP*) applied during the local search. Attention: this is not only a simple counter, it is also used to avoid repeating move evaluations
    List<int> orderNodes;               // Randomized order for checking the nodes in the RI local search
    List<int> orderRoutes;			// Randomized order for checking the routes in the SWAP* local search
    public SortedSet<int> emptyRoutes;  // indices of all empty routes
    public int loopID;									// Current loop index

    public Node[] clients;              // Elements representing clients (clients[0] is a sentinel and should not be accessed)
    public Node[] depots;				// Elements representing depots
    public Node[] depotsEnd;                // Duplicate of the depots to mark the end of the routes
    public Route[] routes;				// Elements representing routes
    public ThreeBestInsert[,] bestInsertClient; // (SWAP*) For each route and node, storing the cheapest insertion cost 

    public List<int> movesPerformed = new List<int>();

    /* TEMPORARY VARIABLES USED IN THE LOCAL SEARCH LOOPS */
    // nodeUPrev . nodeU . nodeX . nodeXNext
    // nodeVPrev . nodeV . nodeY . nodeYNext
    Node nodeU;
    Node nodeX;
    Node nodeV;
    Node nodeY;
    Route routeU;
    Route routeV;
    int nodeUPrevIndex, nodeUIndex, nodeXIndex, nodeXNextIndex;
    int nodeVPrevIndex, nodeVIndex, nodeYIndex, nodeYNextIndex;
    float loadU, loadX, loadV, loadY;
    float serviceU, serviceX, serviceV, serviceY;
    float penaltyCapacityLS, penaltyDurationLS;
    bool intraRouteMove;

    float[,] matrix;

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
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (loopID > 1) // Allows at least two loops since some moves involving empty routes are not checked at the first loop
                searchCompleted = true;

            //CLASSICAL ROUTE IMPROVEMENT(RI) MOVES SUBJECT TO A PROXIMITY RESTRICTION
            for (int posU = 0; posU < CVRPMgr.inst.problem.customers; posU++)
            {
                nodeU = clients[orderNodes[posU]];
                int lastTestRINodeU = nodeU.whenLastTestedRI;
                nodeU.whenLastTestedRI = nbMoves;
                for (int posV = 0; posV < (int)CVRPMgr.inst.correlatedVertices[nodeU.cour].Count; posV++)
                {
                    nodeV = clients[CVRPMgr.inst.correlatedVertices[nodeU.cour][posV]];

                    if (loopID == 0 || Mathf.Max(nodeU.route.whenLastModified, nodeV.route.whenLastModified) > lastTestRINodeU) // only evaluate moves involving routes that have been modified since last move evaluations for nodeU
                    {
                        // Randomizing the order of the neighborhoods within this loop does not matter much as we are already randomizing the order of the node pairs (and it's not very common to find improving moves of different types for the same node pair)
                        SetLocalVariablesRouteU();
                        SetLocalVariablesRouteV();
                        if (Move1()) { PrintMove(1); continue; } // RELOCATE
                        if (Move2()) { PrintMove(2); continue; } // RELOCATE
                        if (Move3()) { PrintMove(3); continue; }// RELOCATE
                        if (nodeUIndex <= nodeVIndex && Move4()) { PrintMove(4); continue; } // SWAP
                        if (Move5()) { PrintMove(5); continue; }// SWAP
                        if (nodeUIndex <= nodeVIndex && Move6()) { PrintMove(6); continue; }// SWAP
                        if (intraRouteMove && Move7()) { PrintMove(7); continue; } // 2-OPT
                        if (!intraRouteMove && Move8()) { PrintMove(8); continue; } // 2-OPT*
                        if (!intraRouteMove && Move9()) { PrintMove(9); continue; } // 2-OPT*

                        // Trying moves that insert nodeU directly after the depot
                        if (nodeV.prev.isDepot)
                        {
                            nodeV = nodeV.prev;
                            SetLocalVariablesRouteV();
                            if (Move1()) { PrintMove(1); continue; } // RELOCATE
                            if (Move2()) { PrintMove(2); continue; }// RELOCATE
                            if (Move3()) { PrintMove(3); continue; }// RELOCATE
                            if (!intraRouteMove && Move8()) { PrintMove(8); continue; }// 2-OPT*
                            if (!intraRouteMove && Move9()) { PrintMove(9); continue; }// 2-OPT*
                        }
                    }
                }

                //MOVES INVOLVING AN EMPTY ROUTE-- NOT TESTED IN THE FIRST LOOP TO AVOID INCREASING TOO MUCH THE FLEET SIZE
                if (loopID > 0 && emptyRoutes.Count != 0)
                {
                    nodeV = routes[emptyRoutes.First()].depot;
                    SetLocalVariablesRouteU();
                    SetLocalVariablesRouteV();
                    if (Move1()) { PrintMove(1); continue; } // RELOCATE
                    if (Move2()) { PrintMove(2); continue; } // RELOCATE
                    if (Move3()) { PrintMove(3); continue; } // RELOCATE
                    if (Move9()) { PrintMove(9); continue; } // 2-OPT*
                }
            }

            if (CVRPMgr.inst.ap.useSwapStar == true)
            {
                //(SWAP*)MOVES LIMITED TO ROUTE PAIRS WHOSE CIRCLE SECTORS OVERLAP
                for (int rU = 0; rU < CVRPMgr.inst.problem.vehicles; rU++)
                {
                    routeU = routes[orderRoutes[rU]];
                    int lastTestSWAPStarRouteU = routeU.whenLastTestedSWAPStar;
                    routeU.whenLastTestedSWAPStar = nbMoves;
                    for (int rV = 0; rV < CVRPMgr.inst.problem.vehicles; rV++)
                    {
                        routeV = routes[orderRoutes[rV]];
                        if (routeU.nbCustomers > 0 && routeV.nbCustomers > 0 && routeU.cour < routeV.cour
                            && (loopID == 0 || Mathf.Max(routeU.whenLastModified, routeV.whenLastModified)
                                > lastTestSWAPStarRouteU))
                            if (CircleSector.overlap(routeU.sector, routeV.sector))
                                SwapStar();
                    }
                }
            }

            Debug.Log($"Loop {loopID}, Search Completed: {searchCompleted}");
            loopID++;

            Individual print = new Individual();
            ExportIndividual(print);
            foreach (List<int> route in print.chromR)
                Debug.Log(BasicsChecking.PrintList(route));
        }
    }

    void PrintMove(int moveNum)
    {
        Debug.Log($"Move {moveNum} performed between nodes {nodeU.cour} and {nodeV.cour}");
        Individual print = new Individual();
        ExportIndividual(print);
        foreach (List<int> route in print.chromR)
            Debug.Log(BasicsChecking.PrintList(route));
    }

    bool printInMove;
    public void Run(Individual indiv, float penaltyCapacityLS, float penaltyDurationLS)
    {
        InitValues();

        this.penaltyCapacityLS = penaltyCapacityLS;
        this.penaltyDurationLS = penaltyDurationLS;
        LoadIndividual(indiv);

        // Shuffling the order of the nodes explored by the LS to allow for more diversity in the search
        for (int i = orderNodes.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = orderNodes[i];
            orderNodes[i] = orderNodes[j];
            orderNodes[j] = temp;
        }
        for (int i = orderRoutes.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = orderRoutes[i];
            orderRoutes[i] = orderRoutes[j];
            orderRoutes[j] = temp;
        }
        for (int i = 1; i <= CVRPMgr.inst.problem.customers; i++)
        {
            if (Random.Range(0, CVRPMgr.inst.ap.nbGranular) == 0) // O(n/nbGranular) calls to the inner function on average, to achieve linear-time complexity overall
            {
                for (int k = CVRPMgr.inst.correlatedVertices[i].Count - 1; k > 0; k--)
                {
                    int j = Random.Range(0, k + 1);
                    int temp = CVRPMgr.inst.correlatedVertices[i][k];
                    CVRPMgr.inst.correlatedVertices[i][k] = CVRPMgr.inst.correlatedVertices[i][j];
                    CVRPMgr.inst.correlatedVertices[i][j] = temp;
                }
            }
        }

        searchCompleted = false;
        for (loopID = 0; !searchCompleted; loopID++)
        {   
            /*if(loopID <= 9)
            {*/
            printInMove = false;
            if (loopID > 1) // Allows at least two loops since some moves involving empty routes are not checked at the first loop
                searchCompleted = true;

            /* CLASSICAL ROUTE IMPROVEMENT (RI) MOVES SUBJECT TO A PROXIMITY RESTRICTION */
            for (int posU = 0; posU < CVRPMgr.inst.problem.customers; posU++)
            {
                nodeU = clients[orderNodes[posU]];
                int lastTestRINodeU = nodeU.whenLastTestedRI;
                nodeU.whenLastTestedRI = nbMoves;
                for (int posV = 0; posV < (int)CVRPMgr.inst.correlatedVertices[nodeU.cour].Count; posV++)
                {
                    nodeV = clients[CVRPMgr.inst.correlatedVertices[nodeU.cour][posV]];

                    if (loopID == 0 || Mathf.Max(nodeU.route.whenLastModified, nodeV.route.whenLastModified) > lastTestRINodeU) // only evaluate moves involving routes that have been modified since last move evaluations for nodeU
                    {
                        // Randomizing the order of the neighborhoods within this loop does not matter much as we are already randomizing the order of the node pairs (and it's not very common to find improving moves of different types for the same node pair)
                        SetLocalVariablesRouteU();
                        SetLocalVariablesRouteV();
                        if (Move1()) continue; // RELOCATE
                        if (Move2()) continue; // RELOCATE
                        if (Move3()) continue; // RELOCATE
                        if (nodeUIndex <= nodeVIndex && Move4()) continue; // SWAP
                        if (Move5()) continue; // SWAP
                        if (nodeUIndex <= nodeVIndex && Move6()) continue; // SWAP
                        if (intraRouteMove && Move7()) continue; // 2-OPT
                        if (!intraRouteMove && Move8()) continue; // 2-OPT*
                        if (!intraRouteMove && Move9()) continue; // 2-OPT*

                        // Trying moves that insert nodeU directly after the depot
                        if (nodeV.prev.isDepot)
                        {
                            nodeV = nodeV.prev;
                            SetLocalVariablesRouteV();
                            if (Move1()) continue; // RELOCATE
                            if (Move2()) continue; // RELOCATE
                            if (Move3()) continue; // RELOCATE
                            if (!intraRouteMove && Move8()) continue; // 2-OPT*
                            if (!intraRouteMove && Move9()) continue; // 2-OPT*
                        }
                    }
                }

                /* MOVES INVOLVING AN EMPTY ROUTE -- NOT TESTED IN THE FIRST LOOP TO AVOID INCREASING TOO MUCH THE FLEET SIZE */
                if (loopID > 0 && emptyRoutes.Count != 0)
                {
                    nodeV = routes[emptyRoutes.First()].depot;
                    SetLocalVariablesRouteU();
                    SetLocalVariablesRouteV();
                    if (Move1()) continue; // RELOCATE
                    if (Move2()) continue; // RELOCATE
                    if (Move3()) continue; // RELOCATE
                    if (Move9()) continue; // 2-OPT*
                }
            }

            if (CVRPMgr.inst.ap.useSwapStar == true)
            {
                /* (SWAP*) MOVES LIMITED TO ROUTE PAIRS WHOSE CIRCLE SECTORS OVERLAP */
                for (int rU = 0; rU < CVRPMgr.inst.problem.vehicles; rU++)
                {
                    routeU = routes[orderRoutes[rU]];
                    int lastTestSWAPStarRouteU = routeU.whenLastTestedSWAPStar;
                    routeU.whenLastTestedSWAPStar = nbMoves;
                    for (int rV = 0; rV < CVRPMgr.inst.problem.vehicles; rV++)
                    {
                        routeV = routes[orderRoutes[rV]];
                        if (routeU.nbCustomers > 0 && routeV.nbCustomers > 0 && routeU.cour < routeV.cour
                            && (loopID == 0 || Mathf.Max(routeU.whenLastModified, routeV.whenLastModified)
                                > lastTestSWAPStarRouteU))
                            if (CircleSector.overlap(routeU.sector, routeV.sector))
                                SwapStar();
                    }
                }
            }
            /*}
            else
            {
                printInMove = true;

                if (loopID > 1) // Allows at least two loops since some moves involving empty routes are not checked at the first loop
                    searchCompleted = true;

                //CLASSICAL ROUTE IMPROVEMENT(RI) MOVES SUBJECT TO A PROXIMITY RESTRICTION
                for (int posU = 0; posU < CVRPMgr.inst.problem.customers; posU++)
                {
                    nodeU = clients[orderNodes[posU]];
                    int lastTestRINodeU = nodeU.whenLastTestedRI;
                    nodeU.whenLastTestedRI = nbMoves;
                    for (int posV = 0; posV < (int)CVRPMgr.inst.correlatedVertices[nodeU.cour].Count; posV++)
                    {
                        nodeV = clients[CVRPMgr.inst.correlatedVertices[nodeU.cour][posV]];

                        if (loopID == 0 || Mathf.Max(nodeU.route.whenLastModified, nodeV.route.whenLastModified) > lastTestRINodeU) // only evaluate moves involving routes that have been modified since last move evaluations for nodeU
                        {
                            // Randomizing the order of the neighborhoods within this loop does not matter much as we are already randomizing the order of the node pairs (and it's not very common to find improving moves of different types for the same node pair)
                            SetLocalVariablesRouteU();
                            SetLocalVariablesRouteV();
                            if (Move1()) { PrintMove(1); continue; } // RELOCATE
                            if (Move2()) { PrintMove(2); continue; } // RELOCATE
                            if (Move3()) { PrintMove(3); continue; }// RELOCATE
                            if (nodeUIndex <= nodeVIndex && Move4()) { PrintMove(4); continue; } // SWAP
                            if (Move5()) { PrintMove(5); continue; }// SWAP
                            if (nodeUIndex <= nodeVIndex && Move6()) { PrintMove(6); continue; }// SWAP
                            if (intraRouteMove && Move7()) { PrintMove(7); continue; } // 2-OPT
                            if (!intraRouteMove && Move8()) { PrintMove(8); continue; } // 2-OPT*
                            if (!intraRouteMove && Move9()) { PrintMove(9); continue; } // 2-OPT*

                            // Trying moves that insert nodeU directly after the depot
                            if (nodeV.prev.isDepot)
                            {
                                nodeV = nodeV.prev;
                                SetLocalVariablesRouteV();
                                if (Move1()) { PrintMove(1); continue; } // RELOCATE
                                if (Move2()) { PrintMove(2); continue; }// RELOCATE
                                if (Move3()) { PrintMove(3); continue; }// RELOCATE
                                if (!intraRouteMove && Move8()) { PrintMove(8); continue; }// 2-OPT*
                                if (!intraRouteMove && Move9()) { PrintMove(9); continue; }// 2-OPT*
                            }
                        }
                    }

                    //MOVES INVOLVING AN EMPTY ROUTE-- NOT TESTED IN THE FIRST LOOP TO AVOID INCREASING TOO MUCH THE FLEET SIZE
                    if (loopID > 0 && emptyRoutes.Count != 0)
                    {
                        nodeV = routes[emptyRoutes.First()].depot;
                        SetLocalVariablesRouteU();
                        SetLocalVariablesRouteV();
                        if (Move1()) { PrintMove(1); continue; } // RELOCATE
                        if (Move2()) { PrintMove(2); continue; } // RELOCATE
                        if (Move3()) { PrintMove(3); continue; } // RELOCATE
                        if (Move9()) { PrintMove(9); continue; } // 2-OPT*
                    }
                }

                if (CVRPMgr.inst.ap.useSwapStar == true)
                {
                    //(SWAP*)MOVES LIMITED TO ROUTE PAIRS WHOSE CIRCLE SECTORS OVERLAP
                    for (int rU = 0; rU < CVRPMgr.inst.problem.vehicles; rU++)
                    {
                        routeU = routes[orderRoutes[rU]];
                        int lastTestSWAPStarRouteU = routeU.whenLastTestedSWAPStar;
                        routeU.whenLastTestedSWAPStar = nbMoves;
                        for (int rV = 0; rV < CVRPMgr.inst.problem.vehicles; rV++)
                        {
                            routeV = routes[orderRoutes[rV]];
                            if (routeU.nbCustomers > 0 && routeV.nbCustomers > 0 && routeU.cour < routeV.cour
                                && (loopID == 0 || Mathf.Max(routeU.whenLastModified, routeV.whenLastModified)
                                    > lastTestSWAPStarRouteU))
                                if (CircleSector.overlap(routeU.sector, routeV.sector))
                                    SwapStar();
                        }
                    }
                }

                Debug.Log($"Loop {loopID}, Search Completed: {searchCompleted}");
                loopID++;

                
            }*/    
        }

        ExportIndividual(indiv);
    }

    float PenaltyExcessDuration(float myDuration)
    {
        return Mathf.Max(0, myDuration - CVRPMgr.inst.durationLimit) * penaltyDurationLS;
    }

    float PenaltyExcessLoad(float myLoad)
    {
        return Mathf.Max(0, myLoad - CVRPMgr.inst.problem.capacity) * penaltyCapacityLS;
    }

    void SetLocalVariablesRouteU()
    {
        routeU = nodeU.route;
        nodeX = nodeU.next;
        nodeXNextIndex = nodeX.next.cour;
        nodeUIndex = nodeU.cour;
        nodeUPrevIndex = nodeU.prev.cour;
        nodeXIndex = nodeX.cour;
        loadU = CVRPMgr.inst.problem.nodes[nodeUIndex].demand;
        //serviceU = CVRPMgr.inst.problem.nodes[nodeUIndex].serviceDuration;
        loadX = CVRPMgr.inst.problem.nodes[nodeXIndex].demand;
        //serviceX = CVRPMgr.inst.problem.nodes[nodeXIndex].serviceDuration;
    }

    void SetLocalVariablesRouteV()
    {
        routeV = nodeV.route;
        nodeY = nodeV.next;
        nodeYNextIndex = nodeY.next.cour;
        nodeVIndex = nodeV.cour;
        nodeVPrevIndex = nodeV.prev.cour;
        nodeYIndex = nodeY.cour;
        loadV = CVRPMgr.inst.problem.nodes[nodeVIndex].demand;
        //serviceV = CVRPMgr.inst.problem.nodes[nodeVIndex].serviceDuration;
        loadY = CVRPMgr.inst.problem.nodes[nodeYIndex].demand;
        //serviceY = CVRPMgr.inst.problem.nodes[nodeYIndex].serviceDuration;
        intraRouteMove = (routeU == routeV);
    }

    //(M1) If u is a customer visit, remove u from r(u) and place it after v in r(v);
    bool Move1()
    {
        float costSuppU = matrix[nodeUPrevIndex, nodeXIndex] - matrix[nodeUPrevIndex, nodeUIndex] - matrix[nodeUIndex, nodeXIndex];
        float costSuppV = matrix[nodeVIndex, nodeUIndex] + matrix[nodeUIndex, nodeYIndex] - matrix[nodeVIndex, nodeYIndex];

        if (!intraRouteMove) //if r(u) != r(v)
        {
            // Early move pruning to save CPU time. Guarantees that this move cannot improve without checking additional (load, duration...) constraints
            if (costSuppU + costSuppV >= routeU.penalty + routeV.penalty)
                return false;

            costSuppU += PenaltyExcessDuration(routeU.duration + costSuppU - serviceU)
                + PenaltyExcessLoad(routeU.load - loadU)
                - routeU.penalty;

            costSuppV += PenaltyExcessDuration(routeV.duration + costSuppV + serviceU)
                + PenaltyExcessLoad(routeV.load + loadU)
                - routeV.penalty;
        }

        if (costSuppU + costSuppV > -0.00001f)
            return false;
        if (nodeUIndex == nodeYIndex)
            return false;

        InsertNode(nodeU, nodeV);
        nbMoves++;
        searchCompleted = false;
        UpdateRouteData(routeU);
        if (!intraRouteMove) UpdateRouteData(routeV);

        return true;
    }

    //(M2) If u and x are customer visits, remove them, then place u and x after v
    bool Move2()
    {
        float costSuppU = matrix[nodeUPrevIndex, nodeXNextIndex]
            - matrix[nodeUPrevIndex, nodeUIndex]
            - matrix[nodeXIndex, nodeXNextIndex];

        float costSuppV = matrix[nodeVIndex, nodeUIndex]
            + matrix[nodeXIndex, nodeYIndex]
            - matrix[nodeVIndex, nodeYIndex];

        if (!intraRouteMove) //if r(u) != r(v)
        {
            // Early move pruning to save CPU time. Guarantees that this move cannot improve without checking additional (load, duration...) constraints
            if (costSuppU + costSuppV >= routeU.penalty + routeV.penalty)
                return false;

            costSuppU += PenaltyExcessDuration(routeU.duration + costSuppU - matrix[nodeUIndex, nodeXIndex] - serviceU - serviceX)
                + PenaltyExcessLoad(routeU.load - loadU - loadX)
                - routeU.penalty;

            costSuppV += PenaltyExcessDuration(routeV.duration + costSuppV + matrix[nodeUIndex, nodeXIndex] + serviceU + serviceX)
                + PenaltyExcessLoad(routeV.load + loadU + loadX)
                - routeV.penalty;
        }

        if (costSuppU + costSuppV > -0.00001f)
            return false;
        if (nodeU == nodeY || nodeV == nodeX || nodeX.isDepot)
            return false;

        InsertNode(nodeU, nodeV);
        InsertNode(nodeX, nodeU);
        nbMoves++;
        searchCompleted = false;
        UpdateRouteData(routeU);
        if (!intraRouteMove) UpdateRouteData(routeV);

        return true;
    }

    //(M3) If u and x are customer visits, remove them, then place x and u after v
    bool Move3()
    {
        float costSuppU = matrix[nodeUPrevIndex, nodeXNextIndex]
            - matrix[nodeUPrevIndex, nodeUIndex]
            - matrix[nodeUIndex, nodeXIndex]
            - matrix[nodeXIndex, nodeXNextIndex];

        float costSuppV = matrix[nodeVIndex, nodeXIndex]
            + matrix[nodeXIndex, nodeUIndex]
            + matrix[nodeUIndex, nodeYIndex]
            - matrix[nodeVIndex, nodeYIndex];

        if (!intraRouteMove)
        {
            // Early move pruning to save CPU time. Guarantees that this move cannot improve without checking additional (load, duration...) constraints
            if (costSuppU + costSuppV >= routeU.penalty + routeV.penalty)
                return false;

            costSuppU += PenaltyExcessDuration(routeU.duration + costSuppU - serviceU - serviceX)
                + PenaltyExcessLoad(routeU.load - loadU - loadX)
                - routeU.penalty;

            costSuppV += PenaltyExcessDuration(routeV.duration + costSuppV + serviceU + serviceX)
                + PenaltyExcessLoad(routeV.load + loadU + loadX)
                - routeV.penalty;
        }

        if (costSuppU + costSuppV > -0.00001f)
            return false;
        if (nodeU == nodeY || nodeV == nodeX || nodeX.isDepot)
            return false;

        //Debug.Log(costSuppU + costSuppV);
        //Debug.Log(intraRouteMove);

        InsertNode(nodeX, nodeV);
        InsertNode(nodeU, nodeX);
        nbMoves++; // Increment move counter before updating route data
        searchCompleted = false;
        UpdateRouteData(routeU);
        if (!intraRouteMove) UpdateRouteData(routeV);

        return true;
    }

    //(M4) If u and v are customer visits, swap u and v
    bool Move4()
    {
        float costSuppU = matrix[nodeUPrevIndex, nodeVIndex]
            + matrix[nodeVIndex, nodeXIndex]
            - matrix[nodeUPrevIndex, nodeUIndex]
            - matrix[nodeUIndex, nodeXIndex];

        float costSuppV = matrix[nodeVPrevIndex, nodeUIndex]
            + matrix[nodeUIndex, nodeYIndex]
            - matrix[nodeVPrevIndex, nodeVIndex]
            - matrix[nodeVIndex, nodeYIndex];

        if (!intraRouteMove)
        {
            // Early move pruning to save CPU time. Guarantees that this move cannot improve without checking additional (load, duration...) constraints
            if (costSuppU + costSuppV >= routeU.penalty + routeV.penalty) return false;

            costSuppU += PenaltyExcessDuration(routeU.duration + costSuppU + serviceV - serviceU)
                + PenaltyExcessLoad(routeU.load + loadV - loadU)
                - routeU.penalty;

            costSuppV += PenaltyExcessDuration(routeV.duration + costSuppV - serviceV + serviceU)
                + PenaltyExcessLoad(routeV.load + loadU - loadV)
                - routeV.penalty;
        }

        if (costSuppU + costSuppV > -0.00001f)
            return false;
        if (nodeUIndex == nodeVPrevIndex || nodeUIndex == nodeYIndex ||
            nodeU.isDepot || nodeV.isDepot)
            return false;

        SwapNode(nodeU, nodeV);
        nbMoves++; // Increment move counter before updating route data
        searchCompleted = false;
        UpdateRouteData(routeU);
        if (!intraRouteMove) UpdateRouteData(routeV);

        return true;
    }

    //(M5) If u, x, and v are customer visits, swap u and x with v
    bool Move5()
    {
        float costSuppU = matrix[nodeUPrevIndex, nodeVIndex]
            + matrix[nodeVIndex, nodeXNextIndex]
            - matrix[nodeUPrevIndex, nodeUIndex]
            - matrix[nodeXIndex, nodeXNextIndex];

        float costSuppV = matrix[nodeVPrevIndex, nodeUIndex]
            + matrix[nodeXIndex, nodeYIndex]
            - matrix[nodeVPrevIndex, nodeVIndex]
            - matrix[nodeVIndex, nodeYIndex];

        if (!intraRouteMove)
        {
            // Early move pruning to save CPU time. Guarantees that this move cannot improve without checking additional (load, duration...) constraints
            if (costSuppU + costSuppV >= routeU.penalty + routeV.penalty)
                return false;

            costSuppU += PenaltyExcessDuration(routeU.duration + costSuppU - matrix[nodeUIndex, nodeXIndex] + serviceV - serviceU - serviceX)
                + PenaltyExcessLoad(routeU.load + loadV - loadU - loadX)
                - routeU.penalty;

            costSuppV += PenaltyExcessDuration(routeV.duration + costSuppV + matrix[nodeUIndex, nodeXIndex] - serviceV + serviceU + serviceX)
                + PenaltyExcessLoad(routeV.load + loadU + loadX - loadV)
                - routeV.penalty;
        }

        if (costSuppU + costSuppV > -0.00001f)
            return false;
        if (nodeU == nodeV.prev || nodeX == nodeV.prev || nodeU == nodeY || 
            nodeX.isDepot || nodeU.isDepot || nodeY.isDepot)
            return false;

        SwapNode(nodeU, nodeV);
        InsertNode(nodeX, nodeU);
        nbMoves++; // Increment move counter before updating route data
        searchCompleted = false;
        UpdateRouteData(routeU);
        if (!intraRouteMove) UpdateRouteData(routeV);

        return true;
    }

    //(M6) If u, x, v, and y are customer visits, swap u and x with v and y
    bool Move6()
    {
        float costSuppU = matrix[nodeUPrevIndex, nodeVIndex]
            + matrix[nodeYIndex, nodeXNextIndex]
            - matrix[nodeUPrevIndex, nodeUIndex]
            - matrix[nodeXIndex, nodeXNextIndex];

        float costSuppV = matrix[nodeVPrevIndex, nodeUIndex]
            + matrix[nodeXIndex, nodeYNextIndex]
            - matrix[nodeVPrevIndex, nodeVIndex]
            - matrix[nodeYIndex, nodeYNextIndex];

        if (!intraRouteMove)
        {
            // Early move pruning to save CPU time. Guarantees that this move cannot improve without checking additional (load, duration...) constraints
            if (costSuppU + costSuppV >= routeU.penalty + routeV.penalty) return false;

            costSuppU += PenaltyExcessDuration(routeU.duration + costSuppU - matrix[nodeUIndex, nodeXIndex] + matrix[nodeVIndex, nodeYIndex] + serviceV + serviceY - serviceU - serviceX)
                + PenaltyExcessLoad(routeU.load + loadV + loadY - loadU - loadX)
                - routeU.penalty;

            costSuppV += PenaltyExcessDuration(routeV.duration + costSuppV + matrix[nodeUIndex, nodeXIndex] - matrix[nodeVIndex, nodeYIndex] - serviceV - serviceY + serviceU + serviceX)
                + PenaltyExcessLoad(routeV.load + loadU + loadX - loadV - loadY)
                - routeV.penalty;
        }

        if (costSuppU + costSuppV > -0.00001f) return false;
        if (nodeX.isDepot || nodeY.isDepot || nodeU.isDepot || nodeV.isDepot
            || nodeY == nodeU.prev || nodeU == nodeY || nodeX == nodeV || nodeV == nodeX.next) return false;

        SwapNode(nodeU, nodeV);
        SwapNode(nodeX, nodeY);
        nbMoves++; // Increment move counter before updating route data
        searchCompleted = false;
        UpdateRouteData(routeU);
        if (!intraRouteMove) UpdateRouteData(routeV);

        return true;
    }

    //(M7) If r(u) = r(v), replace (u, x) and (v, y) by (u, v) and (x, y)
    bool Move7()
    {
        if (nodeU.position > nodeV.position) return false;
        float cost = matrix[nodeUIndex, nodeVIndex]
            + matrix[nodeXIndex, nodeYIndex]
            - matrix[nodeUIndex, nodeXIndex]
            - matrix[nodeVIndex, nodeYIndex]
            + nodeV.cumulatedReversalDistance
            - nodeX.cumulatedReversalDistance;

        if (cost > -0.00001f) return false;
        if (nodeU.next == nodeV) return false;

        Node nodeNum = nodeX.next;
        nodeX.prev = nodeNum;
        nodeX.next = nodeY;

        int counter = 0;

        while (nodeNum != nodeV && counter < 10000)
        {
            Node temp = nodeNum.next;
            nodeNum.next = nodeNum.prev;
            nodeNum.prev = temp;
            nodeNum = temp;
            counter++;
        }

        if (counter == 10000)
            Debug.Log("Check 1");

        nodeV.next = nodeV.prev;
        nodeV.prev = nodeU;
        nodeU.next = nodeV;
        nodeY.prev = nodeX;

        nbMoves++; // Increment move counter before updating route data
        searchCompleted = false;
        UpdateRouteData(routeU);

        return true;
    }

    //(M8) If r(u) != r(v), replace (u, x) and (v, y) by (u, v) and (x, y)
    bool Move8()
    {
        if (nodeX.isDepot && nodeV.isDepot)
        {
            return false;
        }
        
        float cost = matrix[nodeUIndex, nodeVIndex] + matrix[nodeXIndex, nodeYIndex]
            - matrix[nodeUIndex, nodeXIndex] - matrix[nodeVIndex, nodeYIndex]
            + nodeV.cumulatedReversalDistance + routeU.reversalDistance
            - nodeX.cumulatedReversalDistance - routeU.penalty - routeV.penalty;


        float prePrune = cost;

        // Early move pruning to save CPU time. Guarantees that this move cannot improve without checking additional (load, duration...) constraints
        if (cost >= 0) return false;

        cost += PenaltyExcessDuration(nodeU.cumulatedTime + nodeV.cumulatedTime + nodeV.cumulatedReversalDistance + matrix[nodeUIndex, nodeVIndex])
            + PenaltyExcessDuration(routeU.duration - nodeU.cumulatedTime - matrix[nodeUIndex, nodeXIndex] + routeU.reversalDistance
                - nodeX.cumulatedReversalDistance + routeV.duration - nodeV.cumulatedTime - matrix[nodeVIndex, nodeYIndex] + matrix[nodeXIndex, nodeYIndex])
            + PenaltyExcessLoad(nodeU.cumulatedLoad + nodeV.cumulatedLoad)
            + PenaltyExcessLoad(routeU.load + routeV.load - nodeU.cumulatedLoad - nodeV.cumulatedLoad);

        if (cost > -0.00001f) return false;

        if(printInMove)
            Debug.Log($"Pre-prune = {prePrune} Cost = {cost}");

        Node depotU = routeU.depot;
        Node depotV = routeV.depot;
        Node depotUFin = routeU.depot.prev;
        Node depotVFin = routeV.depot.prev;
        Node depotVSuiv = depotV.next;

        Node temp;
        Node xx = nodeX;
        Node vv = nodeV;

        int counter = 0;
        while (!xx.isDepot && counter < 10000)
        {
            temp = xx.next;
            xx.next = xx.prev;
            xx.prev = temp;
            xx.route = routeV;
            xx = temp;
            counter++;
        }
        if (counter == 10000)
            Debug.Log("Check 2");

        counter = 0;
        while (!vv.isDepot && counter < 10000)
        {
            temp = vv.prev;
            vv.prev = vv.next;
            vv.next = temp;
            vv.route = routeU;
            vv = temp;
            counter++;
        }
        if (counter == 10000)
            Debug.Log("Check 3");

        nodeU.next = nodeV;
        nodeV.prev = nodeU;
        nodeX.next = nodeY;
        nodeY.prev = nodeX;

        if (nodeX.isDepot)
        {
            depotUFin.next = depotU;
            depotUFin.prev = depotVSuiv;
            depotUFin.prev.next = depotUFin;
            depotV.next = nodeY;
            nodeY.prev = depotV;
        }
        else if (nodeV.isDepot)
        {
            depotV.next = depotUFin.prev;
            depotV.next.prev = depotV;
            depotV.prev = depotVFin;
            depotUFin.prev = nodeU;
            nodeU.next = depotUFin;
        }
        else
        {
            depotV.next = depotUFin.prev;
            depotV.next.prev = depotV;
            depotUFin.prev = depotVSuiv;
            depotUFin.prev.next = depotUFin;
        }

        nbMoves++; // Increment move counter before updating route data
        searchCompleted = false;
        UpdateRouteData(routeU);
        UpdateRouteData(routeV);

        return true;
    }

    //(M9) If r(u) != r(v), replace (u, x) and (v, y) by (u, y) and (x, v)
    bool Move9()
    {
        float cost = matrix[nodeUIndex, nodeYIndex] + matrix[nodeVIndex, nodeXIndex]
            - matrix[nodeUIndex, nodeXIndex] - matrix[nodeVIndex, nodeYIndex]
            - routeU.penalty - routeV.penalty;

        cost = matrix[nodeUIndex, nodeYIndex] + matrix[nodeVIndex, nodeXIndex] 
            - matrix[nodeUIndex, nodeXIndex] - matrix[nodeVIndex,nodeYIndex]
            - routeU.penalty - routeV.penalty;

        // Early move pruning to save CPU time. Guarantees that this move cannot improve without checking additional (load, duration...) constraints
        if (cost >= 0) return false;

        cost += PenaltyExcessDuration(nodeU.cumulatedTime + routeV.duration - nodeV.cumulatedTime - matrix[nodeVIndex, nodeYIndex] + matrix[nodeUIndex, nodeYIndex])
            + PenaltyExcessDuration(routeU.duration - nodeU.cumulatedTime - matrix[nodeUIndex, nodeXIndex] + nodeV.cumulatedTime + matrix[nodeVIndex, nodeXIndex])
            + PenaltyExcessLoad(nodeU.cumulatedLoad + routeV.load - nodeV.cumulatedLoad)
            + PenaltyExcessLoad(nodeV.cumulatedLoad + routeU.load - nodeU.cumulatedLoad);

        if (cost > -0.00001f) return false;

        Node depotU = routeU.depot;
        Node depotV = routeV.depot;
        Node depotUFin = depotU.prev;
        Node depotVFin = depotV.prev;
        Node depotUpred = depotUFin.prev;

        Node count = nodeY;

        int counter = 0;
        while (!count.isDepot && counter < 10000)
        {
            count.route = routeU;
            count = count.next;
            counter++;
        }
        if (counter == 10000)
            Debug.Log("Check 4");

        count = nodeX;
        counter = 0;
        while (!count.isDepot && counter < 10000)
        {
            count.route = routeV;
            count = count.next;
            counter++;
        }
        if (counter == 10000)
            Debug.Log("Check 5");

        nodeU.next = nodeY;
        nodeY.prev = nodeU;
        nodeV.next = nodeX;
        nodeX.prev = nodeV;

        if (nodeX.isDepot)
        {
            depotUFin.prev = depotVFin.prev;
            depotUFin.prev.next = depotUFin;
            nodeV.next = depotVFin;
            depotVFin.prev = nodeV;
        }
        else
        {
            depotUFin.prev = depotVFin.prev;
            depotUFin.prev.next = depotUFin;
            depotVFin.prev = depotUpred;
            depotVFin.prev.next = depotVFin;
        }

        nbMoves++; // Increment move counter before updating route data
        searchCompleted = false;
        UpdateRouteData(routeU);
        UpdateRouteData(routeV);

        return true;
    }

    bool SwapStar()
    {
        SwapStarElement myBestSwapStar = new SwapStarElement();

        // Preprocessing insertion costs
        PreprocessInsertions(routeU, routeV);
        PreprocessInsertions(routeV, routeU);

        // Evaluating the moves
        for (nodeU = routeU.depot.next; !nodeU.isDepot; nodeU = nodeU.next)
        {
            for (nodeV = routeV.depot.next; !nodeV.isDepot; nodeV = nodeV.next)
            {
                float deltaPenRouteU = PenaltyExcessLoad(routeU.load + CVRPMgr.inst.problem.nodes[nodeV.cour].demand - CVRPMgr.inst.problem.nodes[nodeU.cour].demand) - routeU.penalty;
                float deltaPenRouteV = PenaltyExcessLoad(routeV.load + CVRPMgr.inst.problem.nodes[nodeU.cour].demand - CVRPMgr.inst.problem.nodes[nodeV.cour].demand) - routeV.penalty;

                // Quick filter: possibly early elimination of many SWAP* due to the capacity constraints/penalties and bounds on insertion costs
                if (deltaPenRouteU + nodeU.deltaRemoval + deltaPenRouteV + nodeV.deltaRemoval <= 0)
                {
                    SwapStarElement mySwapStar = new SwapStarElement();
                    mySwapStar.U = nodeU;
                    mySwapStar.V = nodeV;

                    // Evaluate best reinsertion cost of U in the route of V where V has been removed
                    float extraV = GetCheapestInsertSimultRemoval(nodeU, nodeV, ref mySwapStar.bestPositionU);

                    // Evaluate best reinsertion cost of V in the route of U where U has been removed
                    float extraU = GetCheapestInsertSimultRemoval(nodeV, nodeU, ref mySwapStar.bestPositionV);

                    // Evaluating final cost
                    mySwapStar.moveCost = deltaPenRouteU + nodeU.deltaRemoval + extraU + deltaPenRouteV + nodeV.deltaRemoval + extraV
                        + PenaltyExcessDuration(routeU.duration + nodeU.deltaRemoval + extraU /*+ CVRPMgr.inst.problem.nodes[nodeV.cour].serviceDuration - CVRPMgr.inst.problem.nodes[nodeU.cour].serviceDuration*/)
                        + PenaltyExcessDuration(routeV.duration + nodeV.deltaRemoval + extraV /*- CVRPMgr.inst.problem.nodes[nodeV.cour].serviceDuration + CVRPMgr.inst.problem.nodes[nodeU.cour].serviceDuration*/);

                    if (mySwapStar.moveCost < myBestSwapStar.moveCost)
                        myBestSwapStar = mySwapStar;
                }
            }
        }

        // Including RELOCATE from nodeU towards routeV (costs nothing to include in the evaluation at this step since we already have the best insertion location)
        // Moreover, since the granularity criterion is different, this can lead to different improving moves
        for (nodeU = routeU.depot.next; !nodeU.isDepot; nodeU = nodeU.next)
        {
            SwapStarElement mySwapStar = new SwapStarElement();
            mySwapStar.U = nodeU;
            mySwapStar.bestPositionU = bestInsertClient[routeV.cour, nodeU.cour].bestLocation[0];
            float deltaDistRouteU = matrix[nodeU.prev.cour, nodeU.next.cour]
                - matrix[nodeU.prev.cour, nodeU.cour]
                - matrix[nodeU.cour, nodeU.next.cour];
            float deltaDistRouteV = bestInsertClient[routeV.cour, nodeU.cour].bestCost[0];
            mySwapStar.moveCost = deltaDistRouteU + deltaDistRouteV
                + PenaltyExcessLoad(routeU.load - CVRPMgr.inst.problem.nodes[nodeU.cour].demand) - routeU.penalty
                + PenaltyExcessLoad(routeV.load + CVRPMgr.inst.problem.nodes[nodeU.cour].demand) - routeV.penalty
                + PenaltyExcessDuration(routeU.duration + deltaDistRouteU /*- CVRPMgr.inst.problem.nodes[nodeU.cour].serviceDuration*/)
                + PenaltyExcessDuration(routeV.duration + deltaDistRouteV /*+ CVRPMgr.inst.problem.nodes[nodeU.cour].serviceDuration*/);

            if (mySwapStar.moveCost < myBestSwapStar.moveCost)
                myBestSwapStar = mySwapStar;
        }

        // Including RELOCATE from nodeV towards routeU
        for (nodeV = routeV.depot.next; !nodeV.isDepot; nodeV = nodeV.next)
        {
            SwapStarElement mySwapStar = new SwapStarElement();
            mySwapStar.V = nodeV;
            mySwapStar.bestPositionV = bestInsertClient[routeU.cour, nodeV.cour].bestLocation[0];
            float deltaDistRouteU = bestInsertClient[routeU.cour, nodeV.cour].bestCost[0];
            float deltaDistRouteV = matrix[nodeV.prev.cour, nodeV.next.cour]
                - matrix[nodeV.prev.cour, nodeV.cour]
                - matrix[nodeV.cour, nodeV.next.cour];
            mySwapStar.moveCost = deltaDistRouteU + deltaDistRouteV
                + PenaltyExcessLoad(routeU.load + CVRPMgr.inst.problem.nodes[nodeV.cour].demand) - routeU.penalty
                + PenaltyExcessLoad(routeV.load - CVRPMgr.inst.problem.nodes[nodeV.cour].demand) - routeV.penalty
                + PenaltyExcessDuration(routeU.duration + deltaDistRouteU /*+ CVRPMgr.inst.problem.nodes[nodeV.cour].serviceDuration*/)
                + PenaltyExcessDuration(routeV.duration + deltaDistRouteV /*- CVRPMgr.inst.problem.nodes[nodeV.cour].serviceDuration*/);

            if (mySwapStar.moveCost < myBestSwapStar.moveCost)
                myBestSwapStar = mySwapStar;
        }

        if (myBestSwapStar.moveCost > -0.00001f) return false;

        // Applying the best move in case of improvement
        if (myBestSwapStar.bestPositionU != null) InsertNode(myBestSwapStar.U, myBestSwapStar.bestPositionU);
        if (myBestSwapStar.bestPositionV != null) InsertNode(myBestSwapStar.V, myBestSwapStar.bestPositionV);
        nbMoves++; // Increment move counter before updating route data
        searchCompleted = false;
        UpdateRouteData(routeU);
        UpdateRouteData(routeV);

        return true;
    }

    float GetCheapestInsertSimultRemoval(Node U, Node V, ref Node bestPosition)
    {
        ThreeBestInsert myBestInsert = bestInsertClient[V.route.cour, U.cour];
        bool found = false;

        // Find best insertion in the route such that V is not next or pred (can only belong to the top three locations)
        bestPosition = myBestInsert.bestLocation[0];
        float bestCost = myBestInsert.bestCost[0];
        found = (bestPosition != V && bestPosition.next != V);
        if (!found && myBestInsert.bestLocation[1] != null)
        {
            bestPosition = myBestInsert.bestLocation[1];
            bestCost = myBestInsert.bestCost[1];
            found = (bestPosition != V && bestPosition.next != V);
            if (!found && myBestInsert.bestLocation[2] != null)
            {
                bestPosition = myBestInsert.bestLocation[2];
                bestCost = myBestInsert.bestCost[2];
                found = true;
            }
        }

        // Compute insertion in the place of V
        float deltaCost = matrix[V.prev.cour, U.cour]
            + matrix[U.cour, V.next.cour]
            - matrix[V.prev.cour, V.next.cour];
        if (!found || deltaCost < bestCost)
        {
            bestPosition = V.prev;
            bestCost = deltaCost;
        }

        return bestCost;
    }

    void PreprocessInsertions(Route R1, Route R2)
    {
        for (Node U = R1.depot.next; !U.isDepot; U = U.next)
        {
            U.deltaRemoval = matrix[U.prev.cour, U.next.cour]
                - matrix[U.prev.cour, U.cour]
                - matrix[U.cour, U.next.cour];

            if (R2.whenLastModified > bestInsertClient[R2.cour, U.cour].whenLastCalculated)
            {
                bestInsertClient[R2.cour, U.cour].Reset();
                bestInsertClient[R2.cour, U.cour].whenLastCalculated = nbMoves;
                bestInsertClient[R2.cour, U.cour].bestCost[0] = matrix[0, U.cour]
                    + matrix[U.cour, R2.depot.next.cour]
                    - matrix[0, R2.depot.next.cour];
                bestInsertClient[R2.cour, U.cour].bestLocation[0] = R2.depot;
                for (Node V = R2.depot.next; !V.isDepot; V = V.next)
                {
                    float deltaCost = matrix[V.cour, U.cour]
                        + matrix[U.cour, V.next.cour]
                        - matrix[V.cour, V.next.cour];
                    bestInsertClient[R2.cour, U.cour].CompareAndAdd(deltaCost, V);
                }
            }
        }
    }

    //Insert node U after node V
    void InsertNode(Node U, Node V)
    {
        U.prev.next = U.next;
        U.next.prev = U.prev;
        V.next.prev = U;
        U.prev = V;
        U.next = V.next;
        V.next = U;
        U.route = V.route;
    }

    //Swaps nodes U and V
    void SwapNode(Node U, Node V)
    {
        Node myVPred = V.prev;
        Node myVSuiv = V.next;
        Node myUPred = U.prev;
        Node myUSuiv = U.next;
        Route myRouteU = U.route;
        Route myRouteV = V.route;

        myUPred.next = V;
        myUSuiv.prev = V;
        myVPred.next = U;
        myVSuiv.prev = U;

        U.prev = myVPred;
        U.next = myVSuiv;
        V.prev = myUPred;
        V.next = myUSuiv;

        U.route = myRouteV;
        V.route = myRouteU;
    }

    void UpdateRouteData(Route myRoute)
    {
        int myplace = 0;
        float myload = 0;
        float mytime = 0;
        float myReversalDistance = 0;
        float cumulatedX = 0;
        float cumulatedY = 0;

        Node mynode = myRoute.depot;
        mynode.position = 0;
        mynode.cumulatedLoad = 0;
        mynode.cumulatedTime = 0;
        mynode.cumulatedReversalDistance = 0;

        bool firstIt = true;
        int counter = 0;
        while ((!mynode.isDepot || firstIt) && counter++ < 10000)
        {
            mynode = mynode.next;
            myplace++;
            mynode.position = myplace;
            myload += CVRPMgr.inst.problem.nodes[mynode.cour].demand;
            mytime += matrix[mynode.prev.cour, mynode.cour] + CVRPMgr.inst.problem.nodes[mynode.cour].serviceDuration;
            myReversalDistance += matrix[mynode.cour, mynode.prev.cour] - matrix[mynode.prev.cour, mynode.cour];
            mynode.cumulatedLoad = myload;
            mynode.cumulatedTime = mytime;
            mynode.cumulatedReversalDistance = myReversalDistance;
            if (!mynode.isDepot)
            {
                cumulatedX += CVRPMgr.inst.problem.nodes[mynode.cour].coordinate.x;
                cumulatedY += CVRPMgr.inst.problem.nodes[mynode.cour].coordinate.y;
                if (firstIt) myRoute.sector.initialize(CVRPMgr.inst.problem.nodes[mynode.cour].polarAngle);
                else myRoute.sector.extend(CVRPMgr.inst.problem.nodes[mynode.cour].polarAngle);
            }
            firstIt = false;
        }
        if (counter == 10000)
            Debug.Log("Check 6");

        myRoute.duration = mytime;
        myRoute.load = myload;
        myRoute.penalty = PenaltyExcessDuration(mytime) + PenaltyExcessLoad(myload);
        myRoute.nbCustomers = myplace - 1;
        myRoute.reversalDistance = myReversalDistance;
        // Remember "when" this route has been last modified (will be used to filter unnecessary move evaluations)
        myRoute.whenLastModified = nbMoves;

        if (myRoute.nbCustomers == 0)
        {
            myRoute.polarAngleBarycenter = 1e30f;
            emptyRoutes.Add(myRoute.cour);
        }
        else
        {
            myRoute.polarAngleBarycenter = Mathf.Atan2(cumulatedY / (float)myRoute.nbCustomers - CVRPMgr.inst.problem.nodes[0].coordinate.y, cumulatedX / (float)myRoute.nbCustomers - CVRPMgr.inst.problem.nodes[0].coordinate.x);
            emptyRoutes.Remove(myRoute.cour);
        }
    }

    void LoadIndividual(Individual indiv)
    {
        emptyRoutes.Clear();
        nbMoves = 0;

        for (int r = 0; r < CVRPMgr.inst.problem.vehicles; r++)
        {
            Node myDepot = depots[r];
            Node myDepotFin = depotsEnd[r];
            Route myRoute = routes[r];
            myDepot.prev = myDepotFin;
            myDepotFin.next = myDepot;

            if (indiv.chromR[r].Count != 0)
            {
                Node myClient = clients[indiv.chromR[r][0]];
                myClient.route = myRoute;
                myClient.prev = myDepot;
                myDepot.next = myClient;
                for (int i = 1; i < indiv.chromR[r].Count; i++)
                {
                    Node myClientPred = myClient;
                    myClient = clients[indiv.chromR[r][i]];
                    myClient.prev = myClientPred;
                    myClientPred.next = myClient;
                    myClient.route = myRoute;
                }
                myClient.next = myDepotFin;
                myDepotFin.prev = myClient;
            }
            else
            {
                myDepot.next = myDepotFin;
                myDepotFin.prev = myDepot;
            }
            UpdateRouteData(routes[r]);
            routes[r].whenLastTestedSWAPStar = -1;
            for (int i = 1; i <= CVRPMgr.inst.problem.customers; i++)
                bestInsertClient[r, i].whenLastCalculated = -1;
        }

        for (int i = 1; i <= CVRPMgr.inst.problem.customers; i++)
            clients[i].whenLastTestedRI = -1;
    }


    void ExportIndividual(Individual indiv)
    {
        List<Tuple<float, int>> routePolarAngles = new List<Tuple<float, int>>();
        for (int r = 0; r < CVRPMgr.inst.problem.vehicles; r++)
            routePolarAngles.Add(new Tuple<float, int>(routes[r].polarAngleBarycenter, r));
        routePolarAngles.Sort();


        int pos = 0;
        for (int r = 0; r < CVRPMgr.inst.problem.vehicles; r++)
        {
            indiv.chromR[r].Clear();
            Node node = depots[routePolarAngles[r].Item2].next;
            int counter = 0;
            while (!node.isDepot && counter < 10000)
            {
                //chromT[pos] = node.cour;
                indiv.chromR[r].Add(node.cour);
                node = node.next;
                pos++;
                counter++;
            }
            if (counter == 10000)
                Debug.Log("Check 7");
        }

        indiv.EvaluateCompleteCost();
    }

    public void InitValues()
    {
        matrix = CVRPMgr.inst.problem.adjacencyMatrix;

        penaltyCapacityLS = CVRPMgr.inst.penaltyCapacity;
        penaltyDurationLS = CVRPMgr.inst.penaltyDuration;
        clients = new Node[CVRPMgr.inst.problem.customers + 1];
        routes = new Route[CVRPMgr.inst.problem.vehicles];
        depots = new Node[CVRPMgr.inst.problem.vehicles];
        depotsEnd = new Node[CVRPMgr.inst.problem.vehicles];
        bestInsertClient = new ThreeBestInsert[CVRPMgr.inst.problem.vehicles, CVRPMgr.inst.problem.customers + 1];
        orderNodes = new List<int>();
        orderRoutes = new List<int>();
        emptyRoutes = new SortedSet<int>();

        for (int i = 0; i < CVRPMgr.inst.problem.vehicles; i++)
        {
            for (int j = 0; j < CVRPMgr.inst.problem.customers + 1; j++)
            {
                bestInsertClient[i, j] = new ThreeBestInsert();
            }
        }
        for (int i = 0; i <= CVRPMgr.inst.problem.customers; i++)
        {
            clients[i] = new Node();
            clients[i].cour = i;
            clients[i].isDepot = false;
        }
        for (int i = 0; i < CVRPMgr.inst.problem.vehicles; i++)
        {
            depots[i] = new Node();
            depotsEnd[i] = new Node();
            routes[i] = new Route();

            routes[i].cour = i;
            routes[i].depot = depots[i];

            depots[i].cour = 0;
            depots[i].isDepot = true;
            depots[i].route = routes[i];

            depotsEnd[i].cour = 0;
            depotsEnd[i].isDepot = true;
            depotsEnd[i].route = routes[i];

            // Circularize route
            depots[i].next = depotsEnd[i];
            depotsEnd[i].prev = depots[i];

            // Optional: connect tail to head if needed
            depotsEnd[i].next = depots[i];
            depots[i].prev = depotsEnd[i];

        }
        for (int i = 1; i <= CVRPMgr.inst.problem.customers; i++)
            orderNodes.Add(i);
        for (int r = 0; r < CVRPMgr.inst.problem.vehicles; r++)
            orderRoutes.Add(r);
    }

    List<List<int>> testRoutes;
    public void Init()
    {
        InitValues();

        Individual randomIndiv = new Individual();
        randomIndiv.chromR =  new List<List<int>>
        {
            new List<int> { 25, 23, 22, 26, 19, 20, 21, 9 },
            new List<int> { 8, 43, 44, 30, 41, 40, 39, 42, 38, 37, 2, 16, 1, 15 },
            new List<int> { 36, 34, 31, 32, 33, 28, 29, 27, 6, 5, 7, 35, 3, 4, 14, 13, 12, 11, 18, 17, 10 },
            new List<int> { 24 }
        };

        orderNodes = new List<int> { 34, 3, 25, 30, 27, 11, 38, 29, 42, 7, 24, 13, 28, 36, 23, 12, 40, 6, 16, 10, 9, 14, 33, 15, 17, 21, 1, 41, 20, 26, 2, 44, 18, 5, 32, 37, 35, 43, 31, 22, 19, 4, 39, 8 };
        orderRoutes = new List<int> { 2, 3, 1, 0 };
        /*
        CVRPMgr.inst.correlatedVertices = new List<List<int>>
        {
            new List<int> {40,16,42,24,14,21,36,18,10,11,39,2,9,44,43,15,22,13,41,20,17,8,23,26,19,38,25,37,12},
            new List<int> {38,22,42,26,16,23,10,18,40,15,44,36,8,14,25,12,1,11,20,17,9,43,13,24,21,39,19,37},
            new List<int> {12,39,32,13,29,36,27,38,42,33,31,4,14,30,43,40,35,41,5,6,7,37,34},
            new List<int> {27,29,40,37,31,30,3,13,42,34,33,39,7,41,5,43,38,12,36,32,35,6,14},
            new List<int> {35,33,34,38,3,28,31,29,32,36,42,37,6,40,43,27,39,30,41,4,7},
            new List<int> {41,35,27,4,43,34,30,38,29,7,31,32,42,3,5,36,39,40,28,37,33},
            new List<int> {32,29,6,35,33,36,34,27,43,39,42,41,30,37,31,5,4,38,3,28,40},
            new List<int> {22,44,2,26,19,39,43,38,20,9,25,16,37,28,42,10,15,41,24,1,21,30,17,36,18,23},
            new List<int> {41,22,26,21,24,38,30,40,10,43,15,42,25,11,1,23,12,39,37,14,36,19,2,8,17,18,16,20,44,13},
            new List<int> {13,1,21,19,23,37,8,36,14,9,38,15,12,24,20,2,11,26,22,16,25,18,17},
            new List<int> {36,38,37,19,15,1,20,18,12,17,25,10,24,2,42,14,9,16,21,13},
            new List<int> {36,17,1,24,37,4,21,16,11,9,2,18,20,10,3,42,38,13,15,14},
            new List<int> {12,42,4,14,2,3,38,11,18,16,36,10,20,15,24,9,37,1,21,17},
            new List<int> {4,38,13,24,21,10,36,17,16,20,11,37,12,15,2,1,3,18,42,9},
            new List<int> {44,9,38,17,2,21,20,25,14,10,19,42,11,39,12,36,23,37,24,22,8,13,18,16,43,26,1},
            new List<int> {15,8,22,12,19,17,43,25,18,37,11,20,26,38,42,23,13,24,9,44,14,36,2,1,21,10},
            new List<int> {1,15,20,24,38,18,26,25,11,2,36,22,12,19,21,10,37,23,8,9,14,16,13},
            new List<int> {13,36,12,20,10,17,19,25,1,14,15,11,22,21,23,2,37,16,38,26,24,8,9},
            new List<int> {26,9,10,21,44,16,15,43,22,17,20,18,38,11,25,2,23,24,1,8,37},
            new List<int> {37,43,17,12,2,26,38,8,44,14,21,16,15,11,19,25,10,23,9,18,1,22,36,13,42,24,39},
            new List<int> {36,11,2,9,22,16,17,20,38,23,8,43,19,13,26,15,24,44,39,37,18,25,42,12,10,14,1},
            new List<int> {15,23,8,25,37,2,20,17,26,38,19,21,16,18,44,24,43,10,9,1},
            new List<int> {17,43,37,15,1,16,19,44,18,22,10,8,26,2,38,20,9,21,25,24},
            new List<int> {18,12,26,2,42,14,37,21,20,16,1,8,39,22,11,44,15,13,25,10,9,38,17,19,23,43,36},
            new List<int> {19,10,8,11,44,20,2,23,1,16,17,37,9,15,38,24,26,43,21,22,18},
            new List<int> {44,1,20,15,2,18,19,38,17,37,24,43,25,23,21,22,9,8,10,16},
            new List<int> {41,28,34,7,5,42,30,35,43,3,6,38,32,39,37,33,40,36,31,4,29},
            new List<int> {33,6,43,35,27,7,32,38,29,42,34,40,39,30,5,36,8,41,31,44},
            new List<int> {41,5,39,6,36,3,35,30,4,43,34,7,44,28,27,38,32,42,33,31,40},
            new List<int> {5,36,8,38,9,37,28,41,34,40,44,42,31,27,33,43,32,3,7,39,4,6,29,35},
            new List<int> {4,3,30,5,27,42,29,35,37,32,36,38,33,43,28,6,41,34,7,39,44,40},
            new List<int> {30,36,27,7,28,37,35,5,44,42,33,6,34,39,29,31,4,38,41,3,43,40},
            new List<int> {39,42,5,32,27,43,6,4,3,34,36,7,29,40,30,31,44,35,38,28,41},
            new List<int> {30,43,37,6,29,39,33,5,38,7,41,28,32,3,27,35,4,31,40,42,44,36},
            new List<int> {27,36,6,30,40,41,39,28,34,38,31,33,4,37,5,43,32,7,42,3,29},
            new List<int> {29,16,38,42,8,6,3,40,21,32,35,11,4,28,9,37,15,17,31,27,41,44,33,43,39,12,20,7,13,30,34,10,2,1,14,24,5,18},
            new List<int> {13,34,21,9,22,14,41,42,40,20,38,19,12,27,1,44,18,5,26,2,35,6,25,7,4,10,3,23,36,8,15,24,11,31,16,39,17,30,43,32},
            new List<int> {41,40,14,29,11,30,44,13,27,17,25,24,2,35,37,34,20,32,36,15,26,7,16,18,3,22,39,31,1,19,23,8,10,28,5,43,6,4,21,12,9,42,33},
            new List<int> {42,33,38,4,5,34,20,21,40,35,29,24,30,37,31,8,36,7,3,27,44,28,6,41,1,15,32,2,9,43},
            new List<int> {37,9,5,38,32,34,4,3,29,33,2,27,6,42,30,44,43,28,7,35,31,1,36,41,39},
            new List<int> {31,38,42,6,30,8,34,33,5,1,44,40,7,28,29,39,36,32,27,9,3,35,43,4,37},
            new List<int> {39,38,35,32,20,41,33,1,4,9,31,36,40,2,16,34,30,7,11,5,27,24,43,3,28,37,21,44,13,12,6,14,8,29,15},
            new List<int> {40,31,44,5,38,33,30,22,25,26,32,7,27,24,15,29,23,41,16,9,1,42,6,34,35,36,28,19,4,8,37,21,39,2,20,3},
            new List<int> {34,2,20,1,24,37,16,19,38,39,40,8,15,29,26,22,9,33,25,41,21,42,32,23,43,36,30,28,31}
        };*/

        penaltyCapacityLS = CVRPMgr.inst.penaltyCapacity;
        penaltyDurationLS = CVRPMgr.inst.penaltyDuration;
        LoadIndividual(randomIndiv);

        /*// Shuffling the order of the nodes explored by the LS to allow for more diversity in the search
        for (int i = orderNodes.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = orderNodes[i];
            orderNodes[i] = orderNodes[j];
            orderNodes[j] = temp;
        }
        for (int i = orderRoutes.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = orderRoutes[i];
            orderRoutes[i] = orderRoutes[j];
            orderRoutes[j] = temp;
        }
        for (int i = 1; i <= CVRPMgr.inst.problem.customers; i++)
        {
            if (Random.Range(0, CVRPMgr.inst.ap.nbGranular) == 0) // O(n/nbGranular) calls to the inner function on average, to achieve linear-time complexity overall
            {
                for (int k = CVRPMgr.inst.correlatedVertices[i].Count - 1; k > 0; k--)
                {
                    int j = Random.Range(0, k + 1);
                    int temp = CVRPMgr.inst.correlatedVertices[i][k];
                    CVRPMgr.inst.correlatedVertices[i][k] = CVRPMgr.inst.correlatedVertices[i][j];
                    CVRPMgr.inst.correlatedVertices[i][j] = temp;
                }
            }
        }*/

        loopID = 0;

        searchCompleted = false;
    }

    void CheckRoutes()
    {
        /*
        HashSet<int> visited = new HashSet<int>();
        int routeNum = 0;
        foreach(Route route in routes)
        {
            routeNum++;
            Node node = route.depot;
            int counter = 0;
            while (node != null && counter++ < 10000)
            {
                node = node.next;
                if (node == null)
                {
                    Debug.LogError("Null next node found in route!");
                    return;
                }
                if (node.isDepot && counter > 1)
                {
                    Debug.Log($"Route {routeNum} correctly ends with depot.");
                    break;
                }
                if (visited.Contains(node.cour))
                {
                    Debug.LogError($"Duplicate found: Node {node.cour}");
                }
                visited.Add(node.cour);
            }
            
        }

        if(visited.Count != CVRPMgr.inst.problem.vehicles)
        {
            Debug.LogError($"Missing vertices, has count {visited.Count}");
        }

        Debug.Log("Route does not return to depot or has a cycle or has a duplicate.");
        */
    }

    string PrintCost()
    {
        float cost = 0;
        foreach (Route route in routes)
            cost += route.duration;
        string costPrint = ($"Total Cost: {cost}");
        return costPrint;
    }

    string PrintStats()
    {
        return $"Node U: {nodeUIndex}, Node V: {nodeVIndex}";
    }

    public int moveFunction;
    public int uIndex;
    public int vIndex;
    public bool prev;

    void DebugChecks()
    {
        nodeU = clients[uIndex];
        nodeV = clients[vIndex];
        if (prev)
            nodeV = nodeV.prev;

        SetLocalVariablesRouteU();
        SetLocalVariablesRouteV();

        if (moveFunction == 1)
        {
            Move1();
        }
        else if (moveFunction == 2)
        {
            Move2();
        }
        else if (moveFunction == 3)
        {
            Move3();
        }
        else if (moveFunction == 4)
        {
            Move4();
        }
        else if (moveFunction == 5)
        {
            Move5();
        }
        else if (moveFunction == 6)
        {
            Move6();
        }
        else if (moveFunction == 7)
        {
            Move7();
        }
        else if (moveFunction == 8)
        {
            Move8();
        }
        else if (moveFunction == 9)
        {
            Move9();
        }
        else
        {
            throw new ArgumentException("Invalid move type: " + moveFunction);
        }
    }

}
