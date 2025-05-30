using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
    int[] orderNodes;               // Randomized order for checking the nodes in the RI local search
    int[] orderRoutes;			// Randomized order for checking the routes in the SWAP* local search
    public SortedSet<int> emptyRoutes;  // indices of all empty routes
    int loopID;									// Current loop index

    public Node[] clients;              // Elements representing clients (clients[0] is a sentinel and should not be accessed)
    public Node[] depots;				// Elements representing depots
    public Node[] depotsEnd;                // Duplicate of the depots to mark the end of the routes
    public Route[] routes;				// Elements representing routes
    public ThreeBestInsert[,] bestInsertClient; // (SWAP*) For each route and node, storing the cheapest insertion cost 

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

    }

    public void Run(Individual indiv, float penaltyCapacityLS, float penaltyDurationLS)
    {
        InitValues();

        this.penaltyCapacityLS = penaltyCapacityLS;
        this.penaltyDurationLS = penaltyDurationLS;
        LoadIndividual(indiv);

        // Shuffling the order of the nodes explored by the LS to allow for more diversity in the search
        for (int i = orderNodes.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = orderNodes[i];
            orderNodes[i] = orderNodes[j];
            orderNodes[j] = temp;
        }
        for (int i = orderRoutes.Length - 1; i > 0; i--)
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
                        //if (intraRouteMove && Move7()) continue; // 2-OPT
                        //if (!intraRouteMove && Move8()) continue; // 2-OPT*
                        //if (!intraRouteMove && Move9()) continue; // 2-OPT*

                        // Trying moves that insert nodeU directly after the depot
                        if (nodeV.prev.isDepot)
                        {
                            nodeV = nodeV.prev;
                            SetLocalVariablesRouteV();
                            if (Move1()) continue; // RELOCATE
                            if (Move2()) continue; // RELOCATE
                            if (Move3()) continue; // RELOCATE
                            //if (!intraRouteMove && Move8()) continue; // 2-OPT*
                            //if (!intraRouteMove && Move9()) continue; // 2-OPT*
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
                    //if (Move9()) continue; // 2-OPT*
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
        float costSuppU = matrix[nodeUPrevIndex, nodeXIndex]
            - matrix[nodeUPrevIndex, nodeUIndex]
            - matrix[nodeUIndex, nodeXIndex];

        float costSuppV = matrix[nodeVIndex, nodeUIndex]
            + matrix[nodeUIndex, nodeYIndex]
            - matrix[nodeVIndex, nodeYIndex];

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

        if (costSuppU + costSuppV > -float.Epsilon)
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

        if (costSuppU + costSuppV > -float.Epsilon)
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

        float costSuppV = matrix[nodeVIndex, nodeUIndex]
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

        if (costSuppU + costSuppV > -float.Epsilon)
            return false;
        if (nodeU == nodeY || nodeV == nodeX || nodeX.isDepot)
            return false;

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

        if (costSuppU + costSuppV > -float.Epsilon)
            return false;
        if (nodeUIndex == nodeVPrevIndex || nodeUIndex == nodeYIndex)
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

        if (costSuppU + costSuppV > -float.Epsilon)
            return false;
        if (nodeU == nodeV.prev || nodeX == nodeV.prev || nodeU == nodeY || nodeX.isDepot)
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

        if (costSuppU + costSuppV > -float.Epsilon) return false;
        if (nodeX.isDepot || nodeY.isDepot || nodeY == nodeU.prev || nodeU == nodeY || nodeX == nodeV || nodeV == nodeX.next) return false;

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
        double cost = matrix[nodeUIndex, nodeVIndex]
            + matrix[nodeXIndex, nodeYIndex]
            - matrix[nodeUIndex, nodeXIndex]
            - matrix[nodeVIndex, nodeYIndex]
            + nodeV.cumulatedReversalDistance
            - nodeX.cumulatedReversalDistance;

        if (cost > -float.Epsilon) return false;
        if (nodeU.next == nodeV) return false;

        Node nodeNum = nodeX.next;
        nodeX.prev = nodeNum;
        nodeX.next = nodeY;

        while (nodeNum != nodeV)
        {
            Node temp = nodeNum.next;
            nodeNum.next = nodeNum.prev;
            nodeNum.prev = temp;
            nodeNum = temp;
        }

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
        float cost = matrix[nodeUIndex, nodeVIndex] + matrix[nodeXIndex, nodeYIndex]
            - matrix[nodeUIndex, nodeXIndex] - matrix[nodeVIndex, nodeYIndex]
            + nodeV.cumulatedReversalDistance + routeU.reversalDistance
            - nodeX.cumulatedReversalDistance - routeU.penalty - routeV.penalty;

        // Early move pruning to save CPU time. Guarantees that this move cannot improve without checking additional (load, duration...) constraints
        if (cost >= 0) return false;

        cost += PenaltyExcessDuration(nodeU.cumulatedTime + nodeV.cumulatedTime + nodeV.cumulatedReversalDistance + matrix[nodeUIndex, nodeVIndex])
            + PenaltyExcessDuration(routeU.duration - nodeU.cumulatedTime - matrix[nodeUIndex, nodeXIndex] + routeU.reversalDistance
                - nodeX.cumulatedReversalDistance + routeV.duration - nodeV.cumulatedTime - matrix[nodeVIndex, nodeYIndex] + matrix[nodeXIndex, nodeYIndex])
            + PenaltyExcessLoad(nodeU.cumulatedLoad + nodeV.cumulatedLoad)
            + PenaltyExcessLoad(routeU.load + routeV.load - nodeU.cumulatedLoad - nodeV.cumulatedLoad);

        if (cost > float.Epsilon) return false;

        Node depotU = routeU.depot;
        Node depotV = routeV.depot;
        Node depotUFin = routeU.depot.prev;
        Node depotVFin = routeV.depot.prev;
        Node depotVSuiv = depotV.next;

        Node temp;
        Node xx = nodeX;
        Node vv = nodeV;

        while (!xx.isDepot)
        {
            temp = xx.next;
            xx.next = xx.prev;
            xx.prev = temp;
            xx.route = routeV;
            xx = temp;
        }

        while (!vv.isDepot)
        {
            temp = vv.prev;
            vv.prev = vv.next;
            vv.next = temp;
            vv.route = routeU;
            vv = temp;
        }

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

        // Early move pruning to save CPU time. Guarantees that this move cannot improve without checking additional (load, duration...) constraints
        if (cost >= 0) return false;

        cost += PenaltyExcessDuration(nodeU.cumulatedTime + routeV.duration - nodeV.cumulatedTime - matrix[nodeVIndex, nodeYIndex] + matrix[nodeUIndex, nodeYIndex])
            + PenaltyExcessDuration(routeU.duration - nodeU.cumulatedTime - matrix[nodeUIndex, nodeXIndex] + nodeV.cumulatedTime + matrix[nodeVIndex, nodeXIndex])
            + PenaltyExcessLoad(nodeU.cumulatedLoad + routeV.load - nodeV.cumulatedLoad)
            + PenaltyExcessLoad(nodeV.cumulatedLoad + routeU.load - nodeU.cumulatedLoad);

        if (cost > -float.Epsilon) return false;

        Node depotU = routeU.depot;
        Node depotV = routeV.depot;
        Node depotUFin = depotU.prev;
        Node depotVFin = depotV.prev;
        Node depotUpred = depotUFin.prev;

        Node count = nodeY;
        while (!count.isDepot)
        {
            count.route = routeU;
            count = count.next;
        }

        count = nodeX;
        while (!count.isDepot)
        {
            count.route = routeV;
            count = count.next;
        }

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

        if (myBestSwapStar.moveCost > -float.Epsilon) return false;

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
        int myPlace = 0;
        float myLoad = 0;
        float myTime = 0;
        float myReversalDistance = 0;
        float cumulatedX = 0;
        float cumulatedY = 0;

        Node myNode = myRoute.depot;
        myNode.position = 0;
        myNode.cumulatedLoad = 0;
        myNode.cumulatedTime = 0;
        myNode.cumulatedReversalDistance = 0;

        bool firstIt = true;

        while (!myNode.isDepot || firstIt)
        {
            myNode = myNode.next;
            myPlace++;
            myNode.position = myPlace;
            myLoad += CVRPMgr.inst.problem.nodes[myNode.cour].demand;
            myTime += matrix[myNode.prev.cour, myNode.cour]; // + CVRPMgr.inst.problem.nodes[mynode.cour].serviceDuration;
            myReversalDistance += matrix[myNode.cour, myNode.prev.cour]
                - matrix[myNode.prev.cour, myNode.cour];
            myNode.cumulatedLoad = myLoad;
            myNode.cumulatedTime = myTime;
            myNode.cumulatedReversalDistance = myReversalDistance;
            if (!myNode.isDepot)
            {
                cumulatedX += CVRPMgr.inst.problem.nodes[myNode.cour].coordinate.x;
                cumulatedY += CVRPMgr.inst.problem.nodes[myNode.cour].coordinate.y;
                if (firstIt)
                    myRoute.sector.initialize(CVRPMgr.inst.problem.nodes[myNode.cour].polarAngle);
                else
                    myRoute.sector.extend(CVRPMgr.inst.problem.nodes[myNode.cour].polarAngle);
            }
            firstIt = false;
        }

        myRoute.duration = myTime;
        myRoute.load = myLoad;
        myRoute.penalty = PenaltyExcessDuration(myTime) + PenaltyExcessLoad(myLoad);
        myRoute.nbCustomers = myPlace - 1;
        myRoute.reversalDistance = myReversalDistance;
        // Remember "when" this route has been last modified (will be used to filter unnecessary move evaluations)
        myRoute.whenLastModified = nbMoves;

        if (myRoute.nbCustomers == 0)
        {
            myRoute.polarAngleBarycenter = 0;
            emptyRoutes.Add(myRoute.cour);
        }
        else
        {
            myRoute.polarAngleBarycenter = Mathf.Atan2(
                cumulatedY / myRoute.nbCustomers - CVRPMgr.inst.problem.nodes[0].coordinate.y,
                cumulatedX / myRoute.nbCustomers - CVRPMgr.inst.problem.nodes[0].coordinate.x);
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
            while (!node.isDepot)
            {
                //chromT[pos] = node.cour;
                indiv.chromR[r].Add(node.cour);
                node = node.next;
                pos++;
            }
        }

        indiv.EvaluateCompleteCost();
    }

    public void InitValues()
    {
        matrix = CVRPMgr.inst.problem.adjacencyMatrix;

        penaltyCapacityLS = CVRPMgr.inst.penaltyCapacity;
        clients = new Node[CVRPMgr.inst.problem.customers + 1];
        routes = new Route[CVRPMgr.inst.problem.vehicles];
        depots = new Node[CVRPMgr.inst.problem.vehicles];
        depotsEnd = new Node[CVRPMgr.inst.problem.vehicles];
        bestInsertClient = new ThreeBestInsert[CVRPMgr.inst.problem.vehicles, CVRPMgr.inst.problem.customers + 1];
        orderNodes = new int[CVRPMgr.inst.problem.customers];
        orderRoutes = new int[CVRPMgr.inst.problem.vehicles];
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
        }
        for (int i = 1; i <= CVRPMgr.inst.problem.customers; i++)
            orderNodes[i - 1] = i;
        for (int r = 0; r < CVRPMgr.inst.problem.vehicles; r++)
            orderRoutes[r] = r;
    }

    List<List<int>> testRoutes;
    public void Init()
    {
        InitValues();

        /*
        testRoutes = new List<List<int>> {
            new List<int> { 32, 31, 35 },
            new List<int> { 53, 73, 95, 24, 46},
            new List<int> { 75, 93, 33 },
            new List<int> { 15, 22, 41, 20 },
            new List<int> { 1, 70, 54 },
            new List<int> { 92, 9, 86 },
            new List<int> { 68, 90, 84, 66 },
            new List<int> { 76, 55, 16, 69 },
            new List<int> { 4, 13, 74 },
            new List<int> { 58, 12, 5 },
            new List<int> { 18, 10, 39 },
            new List<int> { 25, 65, 78, 42, 28 },
            new List<int> { 7, 2, 45, 43, 29, 36, 72, 57 },
            new List<int> { 87, 37, 6, 49, 14 },
            new List<int> { 3, 77, 63 },
            new List<int> { 44, 67, 88, 40 },
            new List<int> { 82, 60, 59 },
            new List<int> { 8, 17 },
            new List<int> { 34, 64, 96, 48, 26, 47, 38 },
            new List<int> { 80, 94, 56, 21 },
            new List<int> { 71, 62, 99, 98, 89 },
            new List<int> { 100, 61, 23 },
            new List<int> { 19, 97, 27 },
            new List<int> { 81, 51, 83 },
            new List<int> { 50, 91, 52 },
            new List<int> { 30, 85, 11, 79 }
        };
        */
        
        /*
        List<int> route1 = new List<int> { 17, 21, 18, 13, 11, 12 };
        List<int> route2 = new List<int> { 16, 19, 20, 14 };
        List<int> route3 = new List<int> { 15, 4, 2, 5, 10 };
        List<int> route4 = new List<int> { 9, 7, 3, 1, 6, 8 };
        */

        List<int> route1 = new List<int> { 16, 17, 4, 5, 7, 3 };
        List<int> route2 = new List<int> { 15, 11, 10, 13, 20 };
        List<int> route3 = new List<int> { 2, 9, 21, 19, 18, 6, 8 };
        List<int> route4 = new List<int> { 14, 1, 12 };

        Individual indiv = new Individual();
        indiv.chromR = new List<List<int>> { route1, route2, route3, route4 };

        LoadIndividual(indiv);

        //Run(indiv, CVRPMgr.inst.penaltyCapacity, CVRPMgr.inst.penaltyDuration);

        foreach (List<int> route in indiv.chromR)
            Debug.Log(BasicsChecking.PrintList(route));
        //Debug.Log(BasicsChecking.PrintList(routePolarAngles));
    }
}
