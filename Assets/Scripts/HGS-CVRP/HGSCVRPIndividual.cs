using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class SortedMultiSet<T> : IEnumerable<T>
{
    private SortedDictionary<T, int> _dict;

    public SortedMultiSet()
    {
        _dict = new SortedDictionary<T, int>();
    }

    public SortedMultiSet(IEnumerable<T> items) : this()
    {
        Add(items);
    }

    public bool Contains(T item)
    {
        return _dict.ContainsKey(item);
    }

    public void Add(T item)
    {
        if (_dict.ContainsKey(item))
            _dict[item]++;
        else
            _dict[item] = 1;
    }

    public void Add(IEnumerable<T> items)
    {
        foreach (var item in items)
            Add(item);
    }

    public void Remove(T item)
    {
        if (!_dict.ContainsKey(item))
            throw new ArgumentException();
        if (--_dict[item] == 0)
            _dict.Remove(item);
    }

    // Return the last value in the multiset
    public T Peek()
    {
        if (!_dict.Any())
            throw new NullReferenceException();
        return _dict.Last().Key;
    }

    // Return the last value in the multiset and remove it.
    public T Pop()
    {
        T item = Peek();
        Remove(item);
        return item;
    }
    public int TotalCount()
    {
        return _dict.Values.Sum();
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var kvp in _dict)
            for (int i = 0; i < kvp.Value; i++)
                yield return kvp.Key;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}

public class HGSCVRPIndividual : MonoBehaviour
{
    public EvalIndiv eval;                                                         // Solution cost parameters
    public List<int> chromT;                                                // Giant tour representing the individual
    public List<List<int>> chromR;                               // For each vehicle, the associated sequence of deliveries (complete solution)
    public List<int> successors;                                            // For each node, the successor in the solution (can be the depot 0)
    public List<int> predecessors;                                      // For each node, the predecessor in the solution (can be the depot 0)
    public SortedMultiSet<Tuple<float, HGSCVRPIndividual>> indivsPerProximity;   // The other individuals in the population, ordered by increasing proximity (the set container follows a natural ordering based on the first value of the pair)
    public float biasedFitness;

    public HGSCVRPIndividual()
    {
        successors = new List<int>(new int[Parameters.inst.nbClients +1]);
        predecessors = new List<int>(new int[Parameters.inst.nbClients + 1]);

        chromR = new List<List<int>>();
        for(int i = 0; i < Parameters.inst.nbVehicles; i++)
            chromR.Add(new List<int>());

        chromT = new List<int>();
        for (int i = 0; i < Parameters.inst.nbClients; i++) chromT[i] = i + 1;
        for (int i = chromT.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (chromT[i], chromT[j]) = (chromT[j], chromT[i]);
        }

        eval = new EvalIndiv();
        eval.penalizedCost = float.MaxValue;
        indivsPerProximity = new SortedMultiSet<Tuple<float, HGSCVRPIndividual>>();
    }

    public void evaluateCompleteCost()
    {
        eval = new EvalIndiv();
	    for (int r = 0; r< Parameters.inst.nbVehicles; r++)
	    {
		    if (chromR[r].Count != 0)
		    {
			    float distance = Parameters.inst.timeCost[0][chromR[r][0]];
                float load = Parameters.inst.cli[chromR[r][0]].demand;
                float service = Parameters.inst.cli[chromR[r][0]].serviceDuration;
			    predecessors[chromR[r][0]] = 0;
			    for (int i = 1; i<chromR[r].Count; i++)
			    {
				    distance += Parameters.inst.timeCost[chromR[r][i - 1]][chromR[r][i]];
				    load += Parameters.inst.cli[chromR[r][i]].demand;
				    service += Parameters.inst.cli[chromR[r][i]].serviceDuration;
				    predecessors[chromR[r][i]] = chromR[r][i - 1];
				    successors[chromR[r][i - 1]] = chromR[r][i];
			    }
                successors[chromR[r][chromR[r].Count - 1]] = 0;
                distance += Parameters.inst.timeCost[chromR[r][chromR[r].Count - 1]][0];
                eval.distance += distance;
                eval.nbRoutes++;
                if (load > Parameters.inst.vehicleCapacity) eval.capacityExcess += load - Parameters.inst.vehicleCapacity;
                if (distance + service > Parameters.inst.durationLimit) eval.durationExcess += distance + service - Parameters.inst.durationLimit;
		    }
	    }

	    eval.penalizedCost = eval.distance + eval.capacityExcess * Parameters.inst.penaltyCapacity + eval.durationExcess * Parameters.inst.penaltyDuration;
        eval.isFeasible = (eval.capacityExcess < 0.000001 && eval.durationExcess < 0.000001);
    }

    public HGSCVRPIndividual(HGSCVRPIndividual original)
    {
        eval = original.eval;
        chromT = new List<int>(original.chromT);
        chromR = new List<List<int>>(original.chromR);
        successors = new List<int>(original.successors);
        predecessors = new List<int>(original.predecessors);
        indivsPerProximity = new SortedMultiSet<Tuple<float, HGSCVRPIndividual>>(original.indivsPerProximity);
        biasedFitness = original.biasedFitness;
    }
}
