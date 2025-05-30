using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Search;
using UnityEngine;

[System.Serializable]
public class ClientSplit
{
    public float demand;
    public float serviceTime;
    public float d0_x;
    public float dx_0;
    public float dnext;
    public ClientSplit()
    {
        demand = 0.0f;
        serviceTime = 0.0f;
        d0_x = 0.0f;
        dx_0 = 0.0f;
        dnext = 0.0f;
    }

};

public class Deque
{
    private int[] deque;
    int indexFront;
    int indexBack;

    public Deque()
    {
        deque = new int[10];
    }

    public Deque(int nbElements, int firstNode)
    {
        deque = new int[nbElements];
        deque[0] = firstNode;
        indexFront = 0;
        indexBack = 0;
    }

    public void PushBack(int value)
    {
        indexBack++;
        deque[indexBack] = value;
    }

    public void PopBack()
    {
        indexBack--;
    }

    public void PopFront()
    {
        indexFront++;
    }
    public int GetFront()
    {
        return deque[indexFront];
    }

    public int GetNextFront()
    {
        return deque[indexFront + 1];
    }

    public int GetBack()
    {
        return deque[indexBack];
    }

    public void Reset(int firstNode)
    {
        deque[0] = firstNode;
        indexBack = 0;
        indexFront = 0;
    }

    public int Size()
    {
        return indexBack - indexFront + 1;
    }
}


public class SplitMgr : MonoBehaviour
{   
    public static SplitMgr inst;

    public ClientSplit[] cliSplit;
    public int[] tour;
    public float[] sumDistance;
    public float[] sumLoad;
    public int maxVehicles;
    public ArrayVisualizer visualizer;

    float[,] potential;
    int[,] pred;
    public int[] bestPred;
    List<List<int>> chromR = new List<List<int>>();

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

    public void GeneralSplit(Individual indiv, int nbMaxVehicles)
    {
        InitValues();
        
        maxVehicles = (int)Mathf.Max(nbMaxVehicles, Mathf.Ceil(CVRPMgr.inst.totalDemand / CVRPMgr.inst.problem.capacity));

        for (int i = 0; i < maxVehicles; i++)
        {
            chromR.Add(new List<int>());
        }

        for (int i = 1; i <= CVRPMgr.inst.problem.customers; i++)
        {
            cliSplit[i].demand = CVRPMgr.inst.problem.nodes[indiv.chromT[i - 1]].demand;
            cliSplit[i].d0_x = CVRPMgr.inst.problem.adjacencyMatrix[0, indiv.chromT[i - 1]];
            cliSplit[i].dx_0 = CVRPMgr.inst.problem.adjacencyMatrix[indiv.chromT[i - 1], 0];
            if (i < CVRPMgr.inst.problem.customers)
                cliSplit[i].dnext = CVRPMgr.inst.problem.adjacencyMatrix[indiv.chromT[i - 1], indiv.chromT[i]];
            else
                cliSplit[i].dnext = -1e30f;
            sumLoad[i] = sumLoad[i - 1] + cliSplit[i].demand;
            sumDistance[i] = sumDistance[i - 1] + cliSplit[i - 1].dnext;
        }

        if (!SimpleSplit(indiv))
        {
            //Debug.Log("running LF");
            Debug.Log(SplitLF(indiv));
        }

        indiv.EvaluateCompleteCost();
    }

    float Propagate(int i, int j, int k)
    {
        return potential[k, i] + sumDistance[j] - sumDistance[i + 1] + cliSplit[i + 1].d0_x + cliSplit[j].dx_0
         + CVRPMgr.inst.penaltyCapacity * Mathf.Max(sumLoad[j] - sumLoad[i] - CVRPMgr.inst.problem.capacity, 0);
    }

    bool Dominates(int i, int j, int k)
    {
        return potential[k, j] + cliSplit[j + 1].d0_x > potential[k, i] + cliSplit[i + 1].d0_x + sumDistance[j + 1] - sumDistance[i + 1]
         + CVRPMgr.inst.penaltyCapacity * (sumLoad[j] - sumLoad[i]);
    }

    bool DominatesRight(int i, int j, int k)
    {
        return potential[k, j] + cliSplit[j + 1].d0_x < potential[k, i] + cliSplit[i + 1].d0_x + sumDistance[j + 1] - sumDistance[i + 1] + float.Epsilon;
    }

    bool SimpleSplit(Individual indiv)
    {
        // Reinitialize the potential structures
        potential[0, 0] = 0;
        for (int i = 1; i <= CVRPMgr.inst.problem.customers; i++)
		    potential[0, i] = 1e30f;

        Deque queue = new Deque (CVRPMgr.inst.problem.customers + 1, 0);
        for (int i = 1; i <= CVRPMgr.inst.problem.customers; i++)
		{
            // The front is the best predecessor for i
            potential[0, i] = Propagate(queue.GetFront(), i, 0);
            pred[0, i] = queue.GetFront();

            if (i < CVRPMgr.inst.problem.customers)
			{
                // If i is not dominated by the last of the pile
                if (!Dominates(queue.GetBack(), i, 0))
                {
                    // then i will be inserted, need to remove whoever is dominated by i.
                    while (queue.Size() > 0 && DominatesRight(queue.GetBack(), i, 0))
                        queue.PopBack();
                    queue.PushBack(i);
                }
                // Check iteratively if front is dominated by the next front
                while (queue.Size() > 1 && Propagate(queue.GetFront(), i + 1, 0) > Propagate(queue.GetNextFront(), i + 1, 0) - float.Epsilon)
                    queue.PopFront();
            }
        }

        if (potential[0, CVRPMgr.inst.problem.customers] > 1e29) 
        {
            Debug.Log("ERROR : no Split solution has been propagated until the last node");
            return false;
        }

        // Filling the chromR structure
        for (int k = CVRPMgr.inst.problem.vehicles - 1; k >= maxVehicles; k--)
            indiv.chromR[k].Clear();

        int end = CVRPMgr.inst.problem.customers;
        for (int k = maxVehicles - 1; k >= 0; k--)
        {
            indiv.chromR[k].Clear();
            int begin = pred[0, end];
            for (int ii = begin; ii < end; ii++)
                indiv.chromR[k].Add(indiv.chromT[ii]);
            end = begin;
        }

        // Return OK in case the Split algorithm reached the beginning of the routes
        return (end == 0);
    }

    bool SplitLF(Individual indiv)
    {
        Deque deque = new Deque(CVRPMgr.inst.problem.customers + 1, 0);
        for (int k = 0; k <= maxVehicles; k++)
        {
            for(int t = 1; t <= CVRPMgr.inst.problem.customers; t++)
            {
                potential[k, t] = 1e30f;
            }
        }

        potential[0, 0] = 0;

        Deque queue = new Deque(CVRPMgr.inst.problem.customers + 1, 0);
        for (int k = 0; k < maxVehicles; k++)
        {
            // in the Split problem there is always one feasible solution with k routes that reaches the index k in the tour.
            queue.Reset(k);

            // The range of potentials < 1.29 is always an interval.
            // The size of the queue will stay >= 1 until we reach the end of this interval.
            for (int i = k + 1; i <= CVRPMgr.inst.problem.customers && queue.Size() > 0; i++)
			{
                // The front is the best predecessor for i
                potential[k + 1, i] = Propagate(queue.GetFront(), i, k);
                pred[k + 1, i] = queue.GetFront();

                if (i < CVRPMgr.inst.problem.customers)
				{
                // If i is not dominated by the last of the pile 
                if (!Dominates(queue.GetBack(), i, k))
                {
                    // then i will be inserted, need to remove whoever he dominates
                    while (queue.Size() > 0 && DominatesRight(queue.GetBack(), i, k))
                        queue.PopBack();
                    queue.PushBack(i);
                }

                // Check iteratively if front is dominated by the next front
                while (queue.Size() > 1 && Propagate(queue.GetBack(), i + 1, k) > Propagate(queue.GetNextFront(), i + 1, k) - float.Epsilon)
                    queue.PopFront();
                }
            }
        }

        if (potential[maxVehicles, CVRPMgr.inst.problem.customers] > 1e29f)
        {
            Debug.Log("ERROR : no Split solution has been propagated until the last node");
            return false;
        }

        float minCost = potential[maxVehicles, CVRPMgr.inst.problem.customers];
        int nbRoutes = maxVehicles;
        for (int k = 1; k < maxVehicles; k++)
        {
            if (potential[k, CVRPMgr.inst.problem.customers] < minCost)
            {
                minCost = potential[k, CVRPMgr.inst.problem.customers];
                nbRoutes = k;
            }
        }
            

        // Filling the chromR structure
        for (int k = CVRPMgr.inst.problem.vehicles - 1; k >= nbRoutes ; k--)
            indiv.chromR[k].Clear();

        int end = CVRPMgr.inst.problem.customers;
        for (int k = nbRoutes - 1; k >= 0; k--)
        {
            indiv.chromR[k].Clear();
            int begin = pred[k + 1, end];
            for (int ii = begin; ii < end; ii++)
                indiv.chromR[k].Add(indiv.chromT[ii]);
            end = begin;
        }

        // Return OK in case the Split algorithm reached the beginning of the routes
        return (end == 0);
    }

    public void InitValues()
    {
        cliSplit = new ClientSplit[CVRPMgr.inst.problem.customers + 1];
        sumDistance = new float[CVRPMgr.inst.problem.customers + 1];
        sumLoad = new float[CVRPMgr.inst.problem.customers + 1];
        potential = new float[CVRPMgr.inst.problem.vehicles + 1, CVRPMgr.inst.problem.customers + 1];
        pred = new int[CVRPMgr.inst.problem.vehicles + 1, CVRPMgr.inst.problem.customers + 1];

        for (int i = 0; i < cliSplit.Length; i++)
        {
            cliSplit[i] = new ClientSplit();
        }
    }

    public bool DistanceChecking()
    {
        for(int i = 1; i < cliSplit.Length - 1; i++)
        {
            float dist = Mathf.Round(Vector2.Distance(CVRPMgr.inst.problem.nodes[tour[i - 1]].coordinate, CVRPMgr.inst.problem.nodes[tour[i]].coordinate));
            if (dist != cliSplit[i].dnext)
            {
                Debug.Log($"{i} was wrong");
                return false;
            }
        }

        Debug.Log("All good");
        return true;
    }

    public void Init()
    {
        tour = new int[]{
            17, 20, 18, 15, 12, 
            16, 19, 21, 14, 
            13, 11, 4, 3, 8, 10, 
            9, 7, 5, 2, 1, 6
        };

        tour = new int[] { 14, 3, 16, 11, 12, 8, 7, 10, 15, 4, 21, 5, 19, 17, 1, 6, 13, 18, 9, 20, 2 };

        Individual indiv = new Individual();
        indiv.chromT = tour;

        InitValues();

        GeneralSplit(indiv, CVRPMgr.inst.problem.vehicles);

        DistanceChecking();
        
        foreach(List<int> route in indiv.chromR)
        {
            Debug.Log(BasicsChecking.PrintList(route));
        }

        //ReconstructRoutes();

    }
}
