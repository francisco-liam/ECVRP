using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
[System.Serializable]
public class TSPIndividual
{
    public float pathLength;
    public List<int> path;
    public List<int> decodedChromosome;
    public TSPIndividual()
    {

    }

    public TSPIndividual(List<int> chromosome) : base (chromosome)
    { 
        Evaluate();
    }

    public override List<int> DecodeChromosome(int segmentLength)
    {
        decodedChromosome = base.DecodeChromosome(segmentLength);
        return decodedChromosome;
    }

    public override void Evaluate()
    {
        CreatePath();
        pathLength = 0;
        for (int i = 0; i < path.Count; i++)
        {
            if(i != path.Count - 1)
                pathLength += TSPMgr.inst.problem.adjacencyMatrix[path[i],path[i + 1]];
            else
                pathLength += TSPMgr.inst.problem.adjacencyMatrix[path[i], path[0]];
        }

        fitness = 1 / pathLength;
    }

    public List<int> vertices;
    public void CreatePath()
    {
        vertices = Enumerable.Range(1, TSPMgr.inst.problem.nodes.Count - 1).ToList();
        path = new List<int>();
        path.Add(0);

        foreach(int i in chromosome)
        {
            if (vertices.Count != 0)
            {
                int nextNode;
                if (i == 0)
                    nextNode = FindClosestUnexploredNode(path[path.Count - 1], vertices);
                else
                    nextNode = FindFarthestUnexploredNode(path[path.Count - 1], vertices);

                path.Add(nextNode);
                vertices.Remove(nextNode);
            }
            else
                break;
        }
    }

    int FindClosestUnexploredNode(int node, List<int> unxeploredNodes)
    {
        int closestNode = 0;
        float minDistance = Mathf.Infinity;
        foreach (int potentialNode in unxeploredNodes)
        {
            if (TSPMgr.inst.problem.adjacencyMatrix[node, potentialNode] < minDistance)
            {
                closestNode = potentialNode;
                minDistance = TSPMgr.inst.problem.adjacencyMatrix[node, potentialNode];
            }
        }

        return closestNode;
    }

    int FindFarthestUnexploredNode(int node, List<int> unxeploredNodes)
    {
        int closestNode = 0;
        float maxDistance = -Mathf.Infinity;
        foreach (int potentialNode in unxeploredNodes)
        {
            if (TSPMgr.inst.problem.adjacencyMatrix[node, potentialNode] > maxDistance)
            {
                closestNode = potentialNode;
                maxDistance = TSPMgr.inst.problem.adjacencyMatrix[node, potentialNode];
            }
        }

        return closestNode;
    }

    public override string ToString()
    {
        string pathString = "\nPath: ";
        for(int i = 0; i < path.Count; i++)
            pathString += path[i] + ", ";
        
        string decodedChromosomeString = "\nDecoded Chromosome: ";

        foreach (int i in decodedChromosome)
            decodedChromosomeString += i + " ";

        return base.ToString() 
            + decodedChromosomeString
            + pathString 
            + "\nPath Length: " + pathLength.ToString(); ;
    }
}
*/