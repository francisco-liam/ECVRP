using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class CircleSector
{
    public int start;
    public int end;

    // Positive modulo 65536
    public static int positive_mod(int i)
    {
        // 1) Using the formula positive_mod(n,x) = (n % x + x) % x
        // 2) Moreover, remark that "n % 65536" should be automatically compiled in an optimized form as "n & 0xffff" for faster calculations
        return (i % 65536 + 65536) % 65536;
    }

    // Initialize a circle sector from a single point
    public void initialize(int point)
    {
        start = point;
        end = point;
    }

    // Tests if a point is enclosed in the circle sector
    public bool isEnclosed(int point)
    {
        return (positive_mod(point - start) <= positive_mod(end - start));
    }

    // Tests overlap of two circle sectors
    public static bool overlap(CircleSector sector1, CircleSector sector2)
	{
		return ((positive_mod(sector2.start - sector1.start) <= positive_mod(sector1.end - sector1.start))
			|| (positive_mod(sector1.start - sector2.start) <= positive_mod(sector2.end - sector2.start)));
	}

    // Extends the circle sector to include an additional point 
    // Done in a "greedy" way, such that the resulting circle sector is the smallest
    public void extend(int point)
    {
        if (!isEnclosed(point))
        {
            if (positive_mod(point - end) <= positive_mod(start - point))
                end = point;
            else
                start = point;
        }
    }
};

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
};

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

// Structure used in SWAP* to remember the three best insertion positions of a customer in a given route
public class ThreeBestInsert
{
    public int whenLastCalculated;
    public float[] bestCost = new float[3];
    public Node[] bestLocation = new Node[3];

    public void compareAndAdd(float costInsert, Node placeInsert)
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
    public void reset()
    {
        bestCost[0] = 1e30f; bestLocation[0] = null;
        bestCost[1] = 1e30f; bestLocation[1] = null;
        bestCost[2] = 1e30f; bestLocation[2] = null;
    }

    public ThreeBestInsert() { reset(); }
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


public class LocalSearch : MonoBehaviour
{
    public static LocalSearch inst;

    bool searchCompleted;                       // Tells whether all moves have been evaluated without success
    int nbMoves;                                // Total number of moves (RI and SWAP*) applied during the local search. Attention: this is not only a simple counter, it is also used to avoid repeating move evaluations
    List<int> orderNodes;                // Randomized order for checking the nodes in the RI local search
    List<int> orderRoutes;           // Randomized order for checking the routes in the SWAP* local search
    SortedSet<int> emptyRoutes;              // indices of all empty routes
    int loopID;                                 // Current loop index

    /* THE SOLUTION IS REPRESENTED AS A LINKED LIST OF ELEMENTS */
    List<Node> clients;              // Elements representing clients (clients[0] is a sentinel and should not be accessed)
    List<Node> depots;               // Elements representing depots
    List<Node> depotsEnd;                // Duplicate of the depots to mark the end of the routes
    List<Route> routes;              // Elements representing routes
    List<List<ThreeBestInsert>> bestInsertClient;   // (SWAP*) For each route and node, storing the cheapest insertion cost 

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

    float penaltyExcessDuration(float myDuration) 
    { 
        return Mathf.Max(0, myDuration - Parameters.inst.durationLimit) * penaltyDurationLS; 
    }
    float penaltyExcessLoad(float myLoad) 
    { 
        return Mathf.Max(0, myLoad - Parameters.inst.vehicleCapacity) * penaltyCapacityLS;
    }

    void run(HGSCVRPIndividual indiv, float penaltyCapacityLS, float penaltyDurationLS)
    {
        this.penaltyCapacityLS = penaltyCapacityLS;
        this.penaltyDurationLS = penaltyDurationLS;
        loadIndividual(indiv);

        // Shuffling the order of the nodes explored by the LS to allow for more diversity in the search
        for (int i = orderNodes.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (orderNodes[i], orderNodes[j]) = (orderNodes[j], orderNodes[i]);
        }
        for (int i = orderRoutes.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (orderRoutes[i], orderRoutes[j]) = (orderRoutes[j], orderRoutes[i]);
        }
        for (int i = 1; i <= Parameters.inst.nbClients; i++)
        {
            if (Random.Range(0, 2) % Parameters.inst.ap.nbGranular == 0)  // O(n/nbGranular) calls to the inner function on average, to achieve linear-time complexity overall
            {
                for (int k = orderRoutes.Count - 1; k > 0; k--)
                {
                    int j = Random.Range(0, i + 1);
                    (Parameters.inst.correlatedVertices[i][k], Parameters.inst.correlatedVertices[i][j]) = (Parameters.inst.correlatedVertices[i][j], Parameters.inst.correlatedVertices[i][k]);
                }
            }
        }

        searchCompleted = false;
        for (loopID = 0; !searchCompleted; loopID++)
        {
            if (loopID > 1) // Allows at least two loops since some moves involving empty routes are not checked at the first loop
                searchCompleted = true;

            /* CLASSICAL ROUTE IMPROVEMENT (RI) MOVES SUBJECT TO A PROXIMITY RESTRICTION */
            for (int posU = 0; posU < Parameters.inst.nbClients; posU++)
		    {
                nodeU = clients[orderNodes[posU]];
                int lastTestRINodeU = nodeU.whenLastTestedRI;
                nodeU.whenLastTestedRI = nbMoves;
                for (int posV = 0; posV < Parameters.inst.correlatedVertices[nodeU.cour].Count; posV++)
			    {
                    nodeV = clients[Parameters.inst.correlatedVertices[nodeU.cour][posV]];
                    if (loopID == 0 || Mathf.Max(nodeU.route.whenLastModified, nodeV.route.whenLastModified) > lastTestRINodeU) // only evaluate moves involving routes that have been modified since last move evaluations for nodeU
                    {
                        // Randomizing the order of the neighborhoods within this loop does not matter much as we are already randomizing the order of the node pairs (and it's not very common to find improving moves of different types for the same node pair)
                        setLocalVariablesRouteU();
                        setLocalVariablesRouteV();
                        if (move1()) continue; // RELOCATE
                        if (move2()) continue; // RELOCATE
                        if (move3()) continue; // RELOCATE
                        if (nodeUIndex <= nodeVIndex && move4()) continue; // SWAP
                        if (move5()) continue; // SWAP
                        if (nodeUIndex <= nodeVIndex && move6()) continue; // SWAP
                        if (intraRouteMove && move7()) continue; // 2-OPT
                        if (!intraRouteMove && move8()) continue; // 2-OPT*
                        if (!intraRouteMove && move9()) continue; // 2-OPT*

                        // Trying moves that insert nodeU directly after the depot
                        if (nodeV.prev.isDepot)
                        {
                            nodeV = nodeV.prev;
                            setLocalVariablesRouteV();
                            if (move1()) continue; // RELOCATE
                            if (move2()) continue; // RELOCATE
                            if (move3()) continue; // RELOCATE
                            if (!intraRouteMove && move8()) continue; // 2-OPT*
                            if (!intraRouteMove && move9()) continue; // 2-OPT*
                        }
                    }
                }

                /* MOVES INVOLVING AN EMPTY ROUTE -- NOT TESTED IN THE FIRST LOOP TO AVOID INCREASING TOO MUCH THE FLEET SIZE */
                if (loopID > 0 && emptyRoutes.Count != 0)
                {
                    nodeV = routes[emptyRoutes.Min].depot;
                    setLocalVariablesRouteU();
                    setLocalVariablesRouteV();
                    if (move1()) continue; // RELOCATE
                    if (move2()) continue; // RELOCATE
                    if (move3()) continue; // RELOCATE
                    if (move9()) continue; // 2-OPT*
                }
            }

            if (Parameters.inst.ap.useSwapStar == 1 && Parameters.inst.areCoordinatesProvided)
		    {
                /* (SWAP*) MOVES LIMITED TO ROUTE PAIRS WHOSE CIRCLE SECTORS OVERLAP */
                for (int rU = 0; rU < Parameters.inst.nbVehicles; rU++)
			    {
                    routeU = routes[orderRoutes[rU]];
                    int lastTestSWAPStarRouteU = routeU.whenLastTestedSWAPStar;
                    routeU.whenLastTestedSWAPStar = nbMoves;
                    for (int rV = 0; rV < Parameters.inst.nbVehicles; rV++)
				    {
                        routeV = routes[orderRoutes[rV]];
                        if (routeU.nbCustomers > 0 && routeV.nbCustomers > 0 && routeU.cour < routeV.cour
                            && (loopID == 0 || Mathf.Max(routeU.whenLastModified, routeV.whenLastModified)
                                > lastTestSWAPStarRouteU))
                            if (CircleSector.overlap(routeU.sector, routeV.sector))
                                swapStar();
                    }
                }
            }
        }

        // Register the solution produced by the LS in the individual
        exportIndividual(indiv);
    }

    void setLocalVariablesRouteU()
    {
        routeU = nodeU.route;
        nodeX = nodeU.next;
        nodeXNextIndex = nodeX.next.cour;
        nodeUIndex = nodeU.cour;
        nodeUPrevIndex = nodeU.prev.cour;
        nodeXIndex = nodeX.cour;
        loadU = Parameters.inst.cli[nodeUIndex].demand;
        serviceU = Parameters.inst.cli[nodeUIndex].serviceDuration;
        loadX = Parameters.inst.cli[nodeXIndex].demand;
        serviceX = Parameters.inst.cli[nodeXIndex].serviceDuration;
    }

    void setLocalVariablesRouteV()
    {
        routeV = nodeV.route;
        nodeY = nodeV.next;
        nodeYNextIndex = nodeY.next.cour;
        nodeVIndex = nodeV.cour;
        nodeVPrevIndex = nodeV.prev.cour;
        nodeYIndex = nodeY.cour;
        loadV = Parameters.inst.cli[nodeVIndex].demand;
        serviceV = Parameters.inst.cli[nodeVIndex].serviceDuration;
        loadY = Parameters.inst.cli[nodeYIndex].demand;
        serviceY = Parameters.inst.cli[nodeYIndex].serviceDuration;
        intraRouteMove = (routeU == routeV);
    }

    bool move1()
    {
        float costSuppU = Parameters.inst.timeCost[nodeUPrevIndex][nodeXIndex] - Parameters.inst.timeCost[nodeUPrevIndex][nodeUIndex] - Parameters.inst.timeCost[nodeUIndex][nodeXIndex];
        float costSuppV = Parameters.inst.timeCost[nodeVIndex][nodeUIndex] + Parameters.inst.timeCost[nodeUIndex][nodeYIndex] - Parameters.inst.timeCost[nodeVIndex][nodeYIndex];

        if (!intraRouteMove)
        {
            // Early move pruning to save CPU time. Guarantees that this move cannot improve without checking additional (load, duration...) constraints
            if (costSuppU + costSuppV >= routeU.penalty + routeV.penalty) return false;

            costSuppU += penaltyExcessDuration(routeU.duration + costSuppU - serviceU)
                + penaltyExcessLoad(routeU.load - loadU)
                - routeU.penalty;

            costSuppV += penaltyExcessDuration(routeV.duration + costSuppV + serviceU)
                + penaltyExcessLoad(routeV.load + loadU)
                - routeV.penalty;
        }

        if (costSuppU + costSuppV > - 0.00001) return false;
        if (nodeUIndex == nodeYIndex) return false;

        insertNode(nodeU, nodeV);
        nbMoves++; // Increment move counter before updating route data
        searchCompleted = false;
        updateRouteData(routeU);
        if (!intraRouteMove) updateRouteData(routeV);
        return true;
    }

    bool move2()
    {
        float costSuppU = Parameters.inst.timeCost[nodeUPrevIndex][nodeXNextIndex] - Parameters.inst.timeCost[nodeUPrevIndex][nodeUIndex] - Parameters.inst.timeCost[nodeXIndex][nodeXNextIndex];
        float costSuppV = Parameters.inst.timeCost[nodeVIndex][nodeUIndex] + Parameters.inst.timeCost[nodeXIndex][nodeYIndex] - Parameters.inst.timeCost[nodeVIndex][nodeYIndex];

        if (!intraRouteMove)
        {
            // Early move pruning to save CPU time. Guarantees that this move cannot improve without checking additional (load, duration...) constraints
            if (costSuppU + costSuppV >= routeU.penalty + routeV.penalty) return false;

            costSuppU += penaltyExcessDuration(routeU.duration + costSuppU - Parameters.inst.timeCost[nodeUIndex][nodeXIndex] - serviceU - serviceX)
                + penaltyExcessLoad(routeU.load - loadU - loadX)
                - routeU.penalty;

            costSuppV += penaltyExcessDuration(routeV.duration + costSuppV + Parameters.inst.timeCost[nodeUIndex][nodeXIndex] + serviceU + serviceX)
                + penaltyExcessLoad(routeV.load + loadU + loadX)
                - routeV.penalty;
        }

        if (costSuppU + costSuppV > -0.00001) return false;
        if (nodeU == nodeY || nodeV == nodeX || nodeX.isDepot) return false;

        insertNode(nodeU, nodeV);
        insertNode(nodeX, nodeU);
        nbMoves++; // Increment move counter before updating route data
        searchCompleted = false;
        updateRouteData(routeU);
        if (!intraRouteMove) updateRouteData(routeV);
        return true;
    }

    bool move3()
    {
        float costSuppU = Parameters.inst.timeCost[nodeUPrevIndex][nodeXNextIndex] - Parameters.inst.timeCost[nodeUPrevIndex][nodeUIndex] - Parameters.inst.timeCost[nodeUIndex][nodeXIndex] - Parameters.inst.timeCost[nodeXIndex][nodeXNextIndex];
        float costSuppV = Parameters.inst.timeCost[nodeVIndex][nodeXIndex] + Parameters.inst.timeCost[nodeXIndex][nodeUIndex] + Parameters.inst.timeCost[nodeUIndex][nodeYIndex] - Parameters.inst.timeCost[nodeVIndex][nodeYIndex];

        if (!intraRouteMove)
        {
            // Early move pruning to save CPU time. Guarantees that this move cannot improve without checking additional (load, duration...) constraints
            if (costSuppU + costSuppV >= routeU.penalty + routeV.penalty) return false;

            costSuppU += penaltyExcessDuration(routeU.duration + costSuppU - serviceU - serviceX)
                + penaltyExcessLoad(routeU.load - loadU - loadX)
                - routeU.penalty;

            costSuppV += penaltyExcessDuration(routeV.duration + costSuppV + serviceU + serviceX)
                + penaltyExcessLoad(routeV.load + loadU + loadX)
                - routeV.penalty;
        }

        if (costSuppU + costSuppV > -0.00001) return false;
        if (nodeU == nodeY || nodeX == nodeV || nodeX.isDepot) return false;

        insertNode(nodeX, nodeV);
        insertNode(nodeU, nodeX);
        nbMoves++; // Increment move counter before updating route data
        searchCompleted = false;
        updateRouteData(routeU);
        if (!intraRouteMove) updateRouteData(routeV);
        return true;
    }

    bool move4()
    {
        float costSuppU = Parameters.inst.timeCost[nodeUPrevIndex][nodeVIndex] + Parameters.inst.timeCost[nodeVIndex][nodeXIndex] - Parameters.inst.timeCost[nodeUPrevIndex][nodeUIndex] - Parameters.inst.timeCost[nodeUIndex][nodeXIndex];
        float costSuppV = Parameters.inst.timeCost[nodeVPrevIndex][nodeUIndex] + Parameters.inst.timeCost[nodeUIndex][nodeYIndex] - Parameters.inst.timeCost[nodeVPrevIndex][nodeVIndex] - Parameters.inst.timeCost[nodeVIndex][nodeYIndex];

        if (!intraRouteMove)
        {
            // Early move pruning to save CPU time. Guarantees that this move cannot improve without checking additional (load, duration...) constraints
            if (costSuppU + costSuppV >= routeU.penalty + routeV.penalty) return false;

            costSuppU += penaltyExcessDuration(routeU.duration + costSuppU + serviceV - serviceU)
                + penaltyExcessLoad(routeU.load + loadV - loadU)
                - routeU.penalty;

            costSuppV += penaltyExcessDuration(routeV.duration + costSuppV - serviceV + serviceU)
                + penaltyExcessLoad(routeV.load + loadU - loadV)
                - routeV.penalty;
        }

        if (costSuppU + costSuppV > -0.00001) return false;
        if (nodeUIndex == nodeVPrevIndex || nodeUIndex == nodeYIndex) return false;

        swapNode(nodeU, nodeV);
        nbMoves++; // Increment move counter before updating route data
        searchCompleted = false;
        updateRouteData(routeU);
        if (!intraRouteMove) updateRouteData(routeV);
        return true;
    }

    bool move5()
    {
        float costSuppU = Parameters.inst.timeCost[nodeUPrevIndex][nodeVIndex] + Parameters.inst.timeCost[nodeVIndex][nodeXNextIndex] - Parameters.inst.timeCost[nodeUPrevIndex][nodeUIndex] - Parameters.inst.timeCost[nodeXIndex][nodeXNextIndex];
        float costSuppV = Parameters.inst.timeCost[nodeVPrevIndex][nodeUIndex] + Parameters.inst.timeCost[nodeXIndex][nodeYIndex] - Parameters.inst.timeCost[nodeVPrevIndex][nodeVIndex] - Parameters.inst.timeCost[nodeVIndex][nodeYIndex];

        if (!intraRouteMove)
        {
            // Early move pruning to save CPU time. Guarantees that this move cannot improve without checking additional (load, duration...) constraints
            if (costSuppU + costSuppV >= routeU.penalty + routeV.penalty) return false;

            costSuppU += penaltyExcessDuration(routeU.duration + costSuppU - Parameters.inst.timeCost[nodeUIndex][nodeXIndex] + serviceV - serviceU - serviceX)
                + penaltyExcessLoad(routeU.load + loadV - loadU - loadX)
                - routeU.penalty;

            costSuppV += penaltyExcessDuration(routeV.duration + costSuppV + Parameters.inst.timeCost[nodeUIndex][nodeXIndex] - serviceV + serviceU + serviceX)
                + penaltyExcessLoad(routeV.load + loadU + loadX - loadV)
                - routeV.penalty;
        }

        if (costSuppU + costSuppV > -0.00001) return false;
        if (nodeU == nodeV.prev || nodeX == nodeV.prev || nodeU == nodeY || nodeX.isDepot) return false;

        swapNode(nodeU, nodeV);
        insertNode(nodeX, nodeU);
        nbMoves++; // Increment move counter before updating route data
        searchCompleted = false;
        updateRouteData(routeU);
        if (!intraRouteMove) updateRouteData(routeV);
        return true;
    }

    bool move6()
    {
        float costSuppU = Parameters.inst.timeCost[nodeUPrevIndex][nodeVIndex] + Parameters.inst.timeCost[nodeYIndex][nodeXNextIndex] - Parameters.inst.timeCost[nodeUPrevIndex][nodeUIndex] - Parameters.inst.timeCost[nodeXIndex][nodeXNextIndex];
        float costSuppV = Parameters.inst.timeCost[nodeVPrevIndex][nodeUIndex] + Parameters.inst.timeCost[nodeXIndex][nodeYNextIndex] - Parameters.inst.timeCost[nodeVPrevIndex][nodeVIndex] - Parameters.inst.timeCost[nodeYIndex][nodeYNextIndex];

        if (!intraRouteMove)
        {
            // Early move pruning to save CPU time. Guarantees that this move cannot improve without checking additional (load, duration...) constraints
            if (costSuppU + costSuppV >= routeU.penalty + routeV.penalty) return false;

            costSuppU += penaltyExcessDuration(routeU.duration + costSuppU - Parameters.inst.timeCost[nodeUIndex][nodeXIndex] + Parameters.inst.timeCost[nodeVIndex][nodeYIndex] + serviceV + serviceY - serviceU - serviceX)
                + penaltyExcessLoad(routeU.load + loadV + loadY - loadU - loadX)
                - routeU.penalty;

            costSuppV += penaltyExcessDuration(routeV.duration + costSuppV + Parameters.inst.timeCost[nodeUIndex][nodeXIndex] - Parameters.inst.timeCost[nodeVIndex][nodeYIndex] - serviceV - serviceY + serviceU + serviceX)
                + penaltyExcessLoad(routeV.load + loadU + loadX - loadV - loadY)
                - routeV.penalty;
        }

        if (costSuppU + costSuppV > -0.00001) return false;
        if (nodeX.isDepot || nodeY.isDepot || nodeY == nodeU.prev || nodeU == nodeY || nodeX == nodeV || nodeV == nodeX.next) return false;

        swapNode(nodeU, nodeV);
        swapNode(nodeX, nodeY);
        nbMoves++; // Increment move counter before updating route data
        searchCompleted = false;
        updateRouteData(routeU);
        if (!intraRouteMove) updateRouteData(routeV);
        return true;
    }

    bool move7()
    {
        if (nodeU.position > nodeV.position) return false;

        float cost = Parameters.inst.timeCost[nodeUIndex][nodeVIndex] + Parameters.inst.timeCost[nodeXIndex][nodeYIndex] - Parameters.inst.timeCost[nodeUIndex][nodeXIndex] - Parameters.inst.timeCost[nodeVIndex][nodeYIndex] + nodeV.cumulatedReversalDistance - nodeX.cumulatedReversalDistance;

        if (cost > -0.00001) return false;
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
        updateRouteData(routeU);
        return true;
    }

    bool move8()
    {
        float cost = Parameters.inst.timeCost[nodeUIndex][nodeVIndex] + Parameters.inst.timeCost[nodeXIndex][nodeYIndex] - Parameters.inst.timeCost[nodeUIndex][nodeXIndex] - Parameters.inst.timeCost[nodeVIndex][nodeYIndex]
            + nodeV.cumulatedReversalDistance + routeU.reversalDistance - nodeX.cumulatedReversalDistance
            - routeU.penalty - routeV.penalty;

        // Early move pruning to save CPU time. Guarantees that this move cannot improve without checking additional (load, duration...) constraints
        if (cost >= 0) return false;

        cost += penaltyExcessDuration(nodeU.cumulatedTime + nodeV.cumulatedTime + nodeV.cumulatedReversalDistance + Parameters.inst.timeCost[nodeUIndex][nodeVIndex])
            + penaltyExcessDuration(routeU.duration - nodeU.cumulatedTime - Parameters.inst.timeCost[nodeUIndex][nodeXIndex] + routeU.reversalDistance - nodeX.cumulatedReversalDistance + routeV.duration - nodeV.cumulatedTime - Parameters.inst.timeCost[nodeVIndex][nodeYIndex] + Parameters.inst.timeCost[nodeXIndex][nodeYIndex])
            + penaltyExcessLoad(nodeU.cumulatedLoad + nodeV.cumulatedLoad)
            + penaltyExcessLoad(routeU.load + routeV.load - nodeU.cumulatedLoad - nodeV.cumulatedLoad);

        if (cost > -0.00001) return false;

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
        updateRouteData(routeU);
        updateRouteData(routeV);
        return true;
    }

    bool move9()
    {
        float cost = Parameters.inst.timeCost[nodeUIndex][nodeYIndex] + Parameters.inst.timeCost[nodeVIndex][nodeXIndex] - Parameters.inst.timeCost[nodeUIndex][nodeXIndex] - Parameters.inst.timeCost[nodeVIndex][nodeYIndex]
                    - routeU.penalty - routeV.penalty;

        // Early move pruning to save CPU time. Guarantees that this move cannot improve without checking additional (load, duration...) constraints
        if (cost >= 0) return false;

        cost += penaltyExcessDuration(nodeU.cumulatedTime + routeV.duration - nodeV.cumulatedTime - Parameters.inst.timeCost[nodeVIndex][nodeYIndex] + Parameters.inst.timeCost[nodeUIndex][nodeYIndex])
            + penaltyExcessDuration(routeU.duration - nodeU.cumulatedTime - Parameters.inst.timeCost[nodeUIndex][nodeXIndex] + nodeV.cumulatedTime + Parameters.inst.timeCost[nodeVIndex][nodeXIndex])
            + penaltyExcessLoad(nodeU.cumulatedLoad + routeV.load - nodeV.cumulatedLoad)
            + penaltyExcessLoad(nodeV.cumulatedLoad + routeU.load - nodeU.cumulatedLoad);

        if (cost > -0.00001) return false;

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
            depotUFin.prev.prev = depotUFin;
            depotVFin.prev = depotUpred;
            depotVFin.prev.prev = depotVFin;
        }

        nbMoves++; // Increment move counter before updating route data
        searchCompleted = false;
        updateRouteData(routeU);
        updateRouteData(routeV);
        return true;
    }

    bool swapStar()
    {
        SwapStarElement myBestSwapStar = new SwapStarElement();

        // Preprocessing insertion costs
        preprocessInsertions(routeU, routeV);
        preprocessInsertions(routeV, routeU);

        // Evaluating the moves
        for (nodeU = routeU.depot.next; !nodeU.isDepot; nodeU = nodeU.next)
        {
            for (nodeV = routeV.depot.next; !nodeV.isDepot; nodeV = nodeV.next)
            {
                float deltaPenRouteU = penaltyExcessLoad(routeU.load + Parameters.inst.cli[nodeV.cour].demand - Parameters.inst.cli[nodeU.cour].demand) - routeU.penalty;
                float deltaPenRouteV = penaltyExcessLoad(routeV.load + Parameters.inst.cli[nodeU.cour].demand - Parameters.inst.cli[nodeV.cour].demand) - routeV.penalty;

                // Quick filter: possibly early elimination of many SWAP* due to the capacity constraints/penalties and bounds on insertion costs
                if (deltaPenRouteU + nodeU.deltaRemoval + deltaPenRouteV + nodeV.deltaRemoval <= 0)
                {
                    SwapStarElement mySwapStar = new SwapStarElement();
                    mySwapStar.U = nodeU;
                    mySwapStar.V = nodeV;

                    // Evaluate best reinsertion cost of U in the route of V where V has been removed
                    float extraV = getCheapestInsertSimultRemoval(nodeU, nodeV, mySwapStar.bestPositionU);

                    // Evaluate best reinsertion cost of V in the route of U where U has been removed
                    float extraU = getCheapestInsertSimultRemoval(nodeV, nodeU, mySwapStar.bestPositionV);

                    // Evaluating final cost
                    mySwapStar.moveCost = deltaPenRouteU + nodeU.deltaRemoval + extraU + deltaPenRouteV + nodeV.deltaRemoval + extraV
                        + penaltyExcessDuration(routeU.duration + nodeU.deltaRemoval + extraU + Parameters.inst.cli[nodeV.cour].serviceDuration - Parameters.inst.cli[nodeU.cour].serviceDuration)
                        + penaltyExcessDuration(routeV.duration + nodeV.deltaRemoval + extraV - Parameters.inst.cli[nodeV.cour].serviceDuration + Parameters.inst.cli[nodeU.cour].serviceDuration);

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
            mySwapStar.bestPositionU = bestInsertClient[routeV.cour][nodeU.cour].bestLocation[0];
            float deltaDistRouteU = Parameters.inst.timeCost[nodeU.prev.cour][nodeU.next.cour] - Parameters.inst.timeCost[nodeU.prev.cour][nodeU.cour] - Parameters.inst.timeCost[nodeU.cour][nodeU.next.cour];
            float deltaDistRouteV = bestInsertClient[routeV.cour][nodeU.cour].bestCost[0];
            mySwapStar.moveCost = deltaDistRouteU + deltaDistRouteV
                + penaltyExcessLoad(routeU.load - Parameters.inst.cli[nodeU.cour].demand) - routeU.penalty
                + penaltyExcessLoad(routeV.load + Parameters.inst.cli[nodeU.cour].demand) - routeV.penalty
                + penaltyExcessDuration(routeU.duration + deltaDistRouteU - Parameters.inst.cli[nodeU.cour].serviceDuration)
                + penaltyExcessDuration(routeV.duration + deltaDistRouteV + Parameters.inst.cli[nodeU.cour].serviceDuration);

            if (mySwapStar.moveCost < myBestSwapStar.moveCost)
                myBestSwapStar = mySwapStar;
        }

        // Including RELOCATE from nodeV towards routeU
        for (nodeV = routeV.depot.next; !nodeV.isDepot; nodeV = nodeV.next)
        {
            SwapStarElement mySwapStar = new SwapStarElement();
            mySwapStar.V = nodeV;
            mySwapStar.bestPositionV = bestInsertClient[routeU.cour][nodeV.cour].bestLocation[0];
            float deltaDistRouteU = bestInsertClient[routeU.cour][nodeV.cour].bestCost[0];
            float deltaDistRouteV = Parameters.inst.timeCost[nodeV.prev.cour][nodeV.next.cour] - Parameters.inst.timeCost[nodeV.prev.cour][nodeV.cour] - Parameters.inst.timeCost[nodeV.cour][nodeV.next.cour];
            mySwapStar.moveCost = deltaDistRouteU + deltaDistRouteV
                + penaltyExcessLoad(routeU.load + Parameters.inst.cli[nodeV.cour].demand) - routeU.penalty
                + penaltyExcessLoad(routeV.load - Parameters.inst.cli[nodeV.cour].demand) - routeV.penalty
                + penaltyExcessDuration(routeU.duration + deltaDistRouteU + Parameters.inst.cli[nodeV.cour].serviceDuration)
                + penaltyExcessDuration(routeV.duration + deltaDistRouteV - Parameters.inst.cli[nodeV.cour].serviceDuration);

            if (mySwapStar.moveCost < myBestSwapStar.moveCost)
                myBestSwapStar = mySwapStar;
        }

        if (myBestSwapStar.moveCost > -0.00001) return false;

        // Applying the best move in case of improvement
        if (myBestSwapStar.bestPositionU != null) insertNode(myBestSwapStar.U, myBestSwapStar.bestPositionU);
        if (myBestSwapStar.bestPositionV != null) insertNode(myBestSwapStar.V, myBestSwapStar.bestPositionV);
        nbMoves++; // Increment move counter before updating route data
        searchCompleted = false;
        updateRouteData(routeU);
        updateRouteData(routeV);
        return true;
    }

    float getCheapestInsertSimultRemoval(Node U, Node V, Node bestPosition)
    {
        ThreeBestInsert myBestInsert = bestInsertClient[V.route.cour][U.cour];
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
        float deltaCost = Parameters.inst.timeCost[V.prev.cour][U.cour] + Parameters.inst.timeCost[U.cour][V.next.cour] - Parameters.inst.timeCost[V.prev.cour][V.next.cour];
        if (!found || deltaCost < bestCost)
        {
            bestPosition = V.prev;
            bestCost = deltaCost;
        }

        return bestCost;
    }

    void preprocessInsertions(Route R1, Route R2)
    {
        for (Node U = R1.depot.next; !U.isDepot; U = U.next)
        {
            // Performs the preprocessing
            U.deltaRemoval = Parameters.inst.timeCost[U.prev.cour][U.next.cour] - Parameters.inst.timeCost[U.prev.cour][U.cour] - Parameters.inst.timeCost[U.cour][U.next.cour];
            if (R2.whenLastModified > bestInsertClient[R2.cour][U.cour].whenLastCalculated)
            {
                bestInsertClient[R2.cour][U.cour].reset();
                bestInsertClient[R2.cour][U.cour].whenLastCalculated = nbMoves;
                bestInsertClient[R2.cour][U.cour].bestCost[0] = Parameters.inst.timeCost[0][U.cour] + Parameters.inst.timeCost[U.cour][R2.depot.next.cour] - Parameters.inst.timeCost[0][R2.depot.next.cour];
                bestInsertClient[R2.cour][U.cour].bestLocation[0] = R2.depot;
                for (Node V = R2.depot.next; !V.isDepot; V = V.next)
                {
                    float deltaCost = Parameters.inst.timeCost[V.cour][U.cour] + Parameters.inst.timeCost[U.cour][V.next.cour] - Parameters.inst.timeCost[V.cour][V.next.cour];
                    bestInsertClient[R2.cour][U.cour].compareAndAdd(deltaCost, V);
                }
            }
        }
    }

    void insertNode(Node U, Node V)
    {
        U.prev.next = U.next;
        U.next.prev = U.prev;
        V.next.prev = U;
        U.prev = V;
        U.next = V.next;
        V.next = U;
        U.route = V.route;
    }

    void swapNode(Node U, Node V)
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

    void updateRouteData(Route myRoute)
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
        while (!mynode.isDepot || firstIt)
        {
            mynode = mynode.next;
            myplace++;
            mynode.position = myplace;
            myload += Parameters.inst.cli[mynode.cour].demand;
            mytime += Parameters.inst.timeCost[mynode.prev.cour][mynode.cour] + Parameters.inst.cli[mynode.cour].serviceDuration;
            myReversalDistance += Parameters.inst.timeCost[mynode.cour][mynode.prev.cour] - Parameters.inst.timeCost[mynode.prev.cour][mynode.cour];
            mynode.cumulatedLoad = myload;
            mynode.cumulatedTime = mytime;
            mynode.cumulatedReversalDistance = myReversalDistance;
            if (!mynode.isDepot)
            {
                cumulatedX += Parameters.inst.cli[mynode.cour].coordX;
                cumulatedY += Parameters.inst.cli[mynode.cour].coordY;
                if (firstIt) myRoute.sector.initialize(Parameters.inst.cli[mynode.cour].polarAngle);
                else myRoute.sector.extend(Parameters.inst.cli[mynode.cour].polarAngle);
            }
            firstIt = false;
        }

        myRoute.duration = mytime;
        myRoute.load = myload;
        myRoute.penalty = penaltyExcessDuration(mytime) + penaltyExcessLoad(myload);
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
            myRoute.polarAngleBarycenter = Mathf.Atan2(cumulatedY / (float)myRoute.nbCustomers - Parameters.inst.cli[0].coordY, cumulatedX / (float)myRoute.nbCustomers - Parameters.inst.cli[0].coordX);
            emptyRoutes.Remove(myRoute.cour);
        }
    }

    void loadIndividual(HGSCVRPIndividual indiv)
    {
        emptyRoutes.Clear();
        nbMoves = 0;
        for (int r = 0; r < Parameters.inst.nbVehicles; r++)
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
            updateRouteData(routes[r]);
            routes[r].whenLastTestedSWAPStar = -1;
            for (int i = 1; i <= Parameters.inst.nbClients; i++) // Initializing memory structures
			    bestInsertClient[r][i].whenLastCalculated = -1;
        }
        for (int i = 1; i <= Parameters.inst.nbClients; i++) // Initializing memory structures
		    clients[i].whenLastTestedRI = -1;
    }
}
