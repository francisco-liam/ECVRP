using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

public class ClientSplit
{
    public float demand;
    public float serviceTime;
    public float d0_x;
    public float dx_0;
    public float dnext;
};

public class Deque
{
    private LinkedList<int> deque;
    private LinkedListNode<int> indexFront;  // Pointer to the front element
    private LinkedListNode<int> indexBack;   // Pointer to the back element

    public Deque()
    {
        deque = new LinkedList<int>();
    }

    public Deque(int nbElements, int firstNode)
    {
        deque = new LinkedList<int>();  // Initialize the linked list

        // Add the first element
        deque.AddFirst(firstNode);

        // Initialize indexFront and indexBack to the first element
        indexFront = deque.First;
        indexBack = deque.First;

        // Add remaining elements (if any) to the deque, initialized with default values (0)
        for (int i = 1; i < nbElements; i++)
        {
            deque.AddLast(0);  // Adding default 0 for the remaining elements
        }
    }

    public void PushBack(int value)
    {
        deque.AddLast(value);  // Adds to the back of the deque
    }

    public void PushFront(int value)
    {
        deque.AddFirst(value); // Adds to the front of the deque
    }

    public void PopBack()
    {
        if (deque.Count > 0)
            deque.RemoveLast();  // Removes the last element
    }

    public void PopFront()
    {
        if (deque.Count > 0)
            deque.RemoveFirst(); // Removes the first element
    }
    public int GetFront()
    {
        return deque.First.Value; // Returns the front element
    }

    // Get the next front element (second element in the deque)
    public int GetNextFront()
    {
        if (indexFront?.Next != null)
        {
            return indexFront.Next.Value;
        }
        else
        {
            throw new InvalidOperationException("There is no next front element");
        }
    }

    public int GetBack()
    {
        return deque.Last.Value; // Returns the back element
    }

    public void Reset(int firstNode)
    {
        deque.Clear();  // Clear the existing deque
        deque.AddFirst(firstNode);  // Add the new first node
        indexFront = deque.First;
        indexBack = deque.First;
    }

    public int Size()
    {
        return deque.Count; // Returns the number of elements in the deque
    }
}



public class Split : MonoBehaviour
{
    public static Split inst;
    
    // Problem parameters
    int maxVehicles;

    /* Auxiliary data structures to run the Linear Split algorithm */
    List<ClientSplit> cliSplit;
    List<List<float>> potential;  // Potential vector
    List<List<int>> pred;  // Indice of the predecessor in an optimal path
    List<float> sumDistance; // sumDistance[i] for i > 1 contains the sum of distances : sum_{k=1}^{i-1} d_{k,k+1}
    List<float> sumLoad; // sumLoad[i] for i >= 1 contains the sum of loads : sum_{k=1}^{i} q_k
    List<float> sumService; // sumService[i] for i >= 1 contains the sum of service time : sum_{k=1}^{i} s_k


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // To be called with i < j only
    // Computes the cost of propagating the label i until j
    public float propagate(int i, int j, int k)
    {
        return potential[k][i] + sumDistance[j] - sumDistance[i + 1] + cliSplit[i + 1].d0_x + cliSplit[j].dx_0
            + Parameters.inst.penaltyCapacity * Mathf.Max(sumLoad[j] - sumLoad[i] - Parameters.inst.vehicleCapacity, 0);
    }

    // Tests if i dominates j as a predecessor for all nodes x >= j+1
    // We assume that i < j
    public bool dominates(int i, int j, int k)
    {
        return potential[k][j] + cliSplit[j + 1].d0_x > potential[k][i] + cliSplit[i + 1].d0_x + sumDistance[j + 1] - sumDistance[i + 1]
            + Parameters.inst.penaltyCapacity * (sumLoad[j] - sumLoad[i]);
    }

    // Tests if j dominates i as a predecessor for all nodes x >= j+1
    // We assume that i < j
    public bool dominatesRight(int i, int j, int k)
    {
        return potential[k][j] + cliSplit[j + 1].d0_x < potential[k][i] + cliSplit[i + 1].d0_x + sumDistance[j + 1] - sumDistance[i + 1] + 0.00001;
    }

    public void generalSplit(HGSCVRPIndividual indiv, int nbMaxVehicles)
    {
        // Do not apply Split with fewer vehicles than the trivial (LP) bin packing bound
        maxVehicles = (int)Mathf.Max(nbMaxVehicles, Mathf.Ceil(Parameters.inst.totalDemand/ Parameters.inst.vehicleCapacity));

        // Initialization of the data structures for the linear split algorithms
        // Direct application of the code located at https://github.com/vidalt/Split-Library
        for (int i = 1; i <= Parameters.inst.nbClients; i++)
	{
            cliSplit[i].demand = Parameters.inst.cli[indiv.chromT[i - 1]].demand;
            cliSplit[i].serviceTime = Parameters.inst.cli[indiv.chromT[i - 1]].serviceDuration;
            cliSplit[i].d0_x = Parameters.inst.timeCost[0][indiv.chromT[i - 1]];
            cliSplit[i].dx_0 = Parameters.inst.timeCost[indiv.chromT[i - 1]][0];
            if (i < Parameters.inst.nbClients) 
                cliSplit[i].dnext = Parameters.inst.timeCost[indiv.chromT[i - 1]][indiv.chromT[i]];
            else 
                cliSplit[i].dnext = float.MinValue;
            sumLoad[i] = sumLoad[i - 1] + cliSplit[i].demand;
            sumService[i] = sumService[i - 1] + cliSplit[i].serviceTime;
            sumDistance[i] = sumDistance[i - 1] + cliSplit[i - 1].dnext;
        }

        // We first try the simple split, and then the Split with limited fleet if this is not successful
        if (!splitSimple(indiv))
            splitLF(indiv);

        // Build up the rest of the Individual structure
        indiv.evaluateCompleteCost();
    }

    public bool splitSimple(HGSCVRPIndividual indiv)
    {
        // Reinitialize the potential structures
        potential[0][0] = 0;
        for (int i = 1; i <= Parameters.inst.nbClients; i++)
		potential[0][i] = float.MaxValue;

        // MAIN ALGORITHM -- Simple Split using Bellman's algorithm in topological order
        // This code has been maintained as it is very simple and can be easily adapted to a variety of constraints, whereas the O(n) Split has a more restricted application scope
        if (Parameters.inst.isDurationConstraint)
	    {
            for (int i = 0; i < Parameters.inst.nbClients; i++)
		    {
                float load = 0;
                float distance = 0;
                float serviceDuration = 0;
                for (int j = i + 1; j <= Parameters.inst.nbClients && load <= 1.5 * Parameters.inst.vehicleCapacity ; j++)
			    {
                    load += cliSplit[j].demand;
                    serviceDuration += cliSplit[j].serviceTime;
                    if (j == i + 1) distance += cliSplit[j].d0_x;
                    else distance += cliSplit[j - 1].dnext;
                    float cost = distance + cliSplit[j].dx_0
                        + Parameters.inst.penaltyCapacity* Mathf.Max(load - Parameters.inst.vehicleCapacity, 0)
                        + Parameters.inst.penaltyDuration* Mathf.Max(distance + cliSplit[j].dx_0 + serviceDuration - Parameters.inst.durationLimit, 0);
                    if (potential[0][i] + cost < potential[0][j])
                    {
                        potential[0][j] = potential[0][i] + cost;
                        pred[0][j] = i;
                    }
                }
            }
        }

        else
        {
            Deque queue = new Deque(Parameters.inst.nbClients + 1, 0);
            for (int i = 1; i <= Parameters.inst.nbClients; i++)
		    {
                // The front is the best predecessor for i
                potential[0][i] = propagate(queue.GetFront(), i, 0);
                pred[0][i] = queue.GetFront();

                if (i < Parameters.inst.nbClients)
			    {
                    // If i is not dominated by the last of the pile
                    if (!dominates(queue.GetBack(), i, 0))
                    {
                        // then i will be inserted, need to remove whoever is dominated by i.
                        while (queue.Size() > 0 && dominatesRight(queue.GetBack(), i, 0))
                            queue.PopBack();
                        queue.PushBack(i);
                    }
                    // Check iteratively if front is dominated by the next front
                    while (queue.Size() > 1 && propagate(queue.GetFront(), i + 1, 0) > propagate(queue.GetNextFront(), i + 1, 0) - 0.00001)
                        queue.PopFront();
                }
            }
        }

        if (potential[0][Parameters.inst.nbClients] > 1e29)
            throw new InvalidOperationException("ERROR : no Split solution has been propagated until the last node");

        // Filling the chromR structure
        for (int k = Parameters.inst.nbVehicles - 1; k >= maxVehicles; k--)
		indiv.chromR[k].Clear();

        int end = Parameters.inst.nbClients;
        for (int k = maxVehicles - 1; k >= 0; k--)
        {
            indiv.chromR[k].Clear();
            int begin = pred[0][end];
            for (int ii = begin; ii < end; ii++)
                indiv.chromR[k].Add(indiv.chromT[ii]);
            end = begin;
        }

        // Return OK in case the Split algorithm reached the beginning of the routes
        return (end == 0);
    }

    public bool splitLF(HGSCVRPIndividual indiv)
    {
        // Initialize the potential structures
        potential[0][0] = 0;
        for (int k = 0; k <= maxVehicles; k++)
            for (int i = 1; i <= Parameters.inst.nbClients; i++)
			potential[k][i] = 1e30f;

        // MAIN ALGORITHM -- Simple Split using Bellman's algorithm in topological order
        // This code has been maintained as it is very simple and can be easily adapted to a variety of constraints, whereas the O(n) Split has a more restricted application scope
        if (Parameters.inst.isDurationConstraint) 
	    {
                for (int k = 0; k < maxVehicles; k++)
                {
                    for (int i = k; i < Parameters.inst.nbClients && potential[k][i] < 1e29f ; i++)
			    {
                    float load = 0;
                    float serviceDuration = 0;
                    float distance = 0;
                    for (int j = i + 1; j <= Parameters.inst.nbClients && load <= 1.5 * Parameters.inst.vehicleCapacity ; j++) // Setting a maximum limit on load infeasibility to accelerate the algorithm
				    {
                        load += cliSplit[j].demand;
                        serviceDuration += cliSplit[j].serviceTime;
                        if (j == i + 1) distance += cliSplit[j].d0_x;
                        else distance += cliSplit[j - 1].dnext;
                        float cost = distance + cliSplit[j].dx_0
                                    + Parameters.inst.penaltyCapacity* Mathf.Max(load - Parameters.inst.vehicleCapacity, 0)
                                    + Parameters.inst.penaltyDuration* Mathf.Max(distance + cliSplit[j].dx_0 + serviceDuration - Parameters.inst.durationLimit, 0);
                        if (potential[k][i] + cost < potential[k + 1][j])
                        {
                            potential[k + 1][j] = potential[k][i] + cost;
                            pred[k + 1][j] = i;
                        }
                    }
                }
            }
        }
	    else // MAIN ALGORITHM -- Without duration constraints in O(n), from "Vidal, T. (2016). Split algorithm in O(n) for the capacitated vehicle routing problem. C&OR"
	    {
		    Deque queue = new Deque(Parameters.inst.nbClients + 1, 0);
		    for (int k = 0; k<maxVehicles; k++)
		    {
			    // in the Split problem there is always one feasible solution with k routes that reaches the index k in the tour.
			    queue.Reset(k);

			    // The range of potentials < 1.29 is always an interval.
			    // The size of the queue will stay >= 1 until we reach the end of this interval.
			    for (int i = k + 1; i <= Parameters.inst.nbClients && queue.Size() > 0; i++)
			    {
				    // The front is the best predecessor for i
				    potential[k + 1][i] = propagate(queue.GetFront(), i, k);
                    pred[k + 1][i] = queue.GetFront();

				    if (i< Parameters.inst.nbClients)
				    {
					    // If i is not dominated by the last of the pile 
					    if (!dominates(queue.GetBack(), i, k))
					    {
						    // then i will be inserted, need to remove whoever he dominates
						    while (queue.Size() > 0 && dominatesRight(queue.GetBack(), i, k))
							    queue.PopBack();
						    queue.PushBack(i);
					    }

                        // Check iteratively if front is dominated by the next front
                        while (queue.Size() > 1 && propagate(queue.GetFront(), i + 1, k) > propagate(queue.GetNextFront(), i + 1, k) - 0.00001f)
                            queue.PopFront();
				    }
			    }
		    }
	    }

	    if (potential[maxVehicles][Parameters.inst.nbClients] > 1e29f)
            throw new InvalidOperationException("ERROR : no Split solution has been propagated until the last node");

        // It could be cheaper to use a smaller number of vehicles
        double minCost = potential[maxVehicles][Parameters.inst.nbClients];
        int nbRoutes = maxVehicles;
        for (int k = 1; k < maxVehicles; k++)
            if (potential[k][Parameters.inst.nbClients] < minCost)
            { minCost = potential[k][Parameters.inst.nbClients]; nbRoutes = k; }

        // Filling the chromR structure
        for (int k = Parameters.inst.nbVehicles - 1; k >= nbRoutes ; k--)
		        indiv.chromR[k].Clear();

        int end = Parameters.inst.nbClients;
        for (int k = nbRoutes - 1; k >= 0; k--)
        {
            indiv.chromR[k].Clear();
            int begin = pred[k + 1][end];
            for (int ii = begin; ii < end; ii++)
                indiv.chromR[k].Add(indiv.chromT[ii]);
            end = begin;
        }

        // Return OK in case the Split algorithm reached the beginning of the routes
        return (end == 0);
    }
}
