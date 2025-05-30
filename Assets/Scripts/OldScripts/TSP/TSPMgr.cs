using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

/*
[System.Serializable]
public class TSPProblem
{
    public List<Vector2> nodes;
    public float[,] adjacencyMatrix;

    public void CreateAdjacencyMatrix()
    {
        adjacencyMatrix = new float[nodes.Count, nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = 0; j < i; j++)
            {
                adjacencyMatrix[i, j] = Vector2.Distance(nodes[i], nodes[j]);
                adjacencyMatrix[j, i] = adjacencyMatrix[i, j];
            }
        }
    }

    public void PrintAdjacencyMatrix()
    {
        string log = "";
        for (int i = 0; i < adjacencyMatrix.GetLength(0); i++)
        {
            for (int j = 0; j < adjacencyMatrix.GetLength(1); j++)
            {
                log += adjacencyMatrix[i, j] + ", ";
            }
            log += "\n";
        }
        Debug.Log(log);
    }
}

public class TSPMgr : MonoBehaviour
{
    public static TSPMgr inst;
    public TSPProblem problem;
    public TSPIndividual test;

    private void Awake()
    {
        inst = this;        
    }

    // Start is called before the first frame update
    public void Init()
    {
        problem = DataMgr.inst.ReadTSPFile();
        problem.CreateAdjacencyMatrix();
        //problem.PrintAdjacencyMatrix();

        List<int> chromosome = new List<int>();
        for(int i = 0; i < problem.nodes.Count; i++)
        {
            chromosome.Add(Random.Range(0,2));
        }
        test = new TSPIndividual(chromosome);
        test.CreatePath();
        test.Evaluate();
        test.DecodeChromosome(2);
        Debug.Log(test);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
*/