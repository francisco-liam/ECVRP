using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class ECVRPIndividual : Individual
{
    List<int> decodedChromosone;
    public List<List<int>> routes;
    public List<float> pathLengths;

    public ECVRPIndividual()
    {
        chromosome = new List<int>();
        for (int i = 0; i < ECVRPMgr.inst.problem.nodes.Count; i++)
        {
            chromosome.Add(Random.Range(0, 2));
            chromosome.Add(Random.Range(0, 2));
        }

        routes = new List<List<int>>();
        pathLengths = new List<float>();
        for (int i = 0; i < ECVRPMgr.inst.problem.vehlicles; i++)
        {
            routes.Add(new List<int>());
            pathLengths.Add(0);
        }
    }

    public ECVRPIndividual(List<int> chromosome) : base(chromosome)
    {

    }

    public void ConstructPaths()
    {
        //init routes
        routes = new List<List<int>>();

        //init is visited dictionary
        Dictionary<ECVRPNode, bool> isVisitedDictionary = new Dictionary<ECVRPNode, bool>();
        foreach (ECVRPNode node in ECVRPMgr.inst.problem.nodes)
        {
            if(!node.isDepot && !node.isCharging)
                isVisitedDictionary.Add(node, false);
        }

        DecodeChromosone();

        foreach(int heuristic in decodedChromosone)
        {
            int tourIndex = pathLengths.IndexOf(pathLengths.Min());
        }

    }

    public void DecodeChromosone()
    {
        decodedChromosone = new List<int>();
        for(int i = 0; i < chromosome.Count;i+=2)
        {
            int decodedValue = 2 * chromosome[i] + 1 * chromosome[i+1];
            decodedChromosone.Add(decodedValue);
        }
    }

    public void Evaluate()
    {
        pathLengths = new List<float>();
        foreach (List<int> route in routes)
        {
            float routePathLength = 0;
            for (int i = 0; i < route.Count; i++)
            {
                if (i != route.Count - 1)
                    routePathLength += Mathf.RoundToInt(Vector2.Distance(ECVRPMgr.inst.problem.nodes[route[i]].coordinate, ECVRPMgr.inst.problem.nodes[route[i + 1]].coordinate));
                else
                    routePathLength += Mathf.RoundToInt(Vector2.Distance(ECVRPMgr.inst.problem.nodes[route[i]].coordinate, ECVRPMgr.inst.problem.nodes[route[0]].coordinate));
            }
        }
    }
}
