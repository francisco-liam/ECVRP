using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ECVRPNode
{
    public int nodeNumber;
    public Vector2 coordinate;
    public int demand;
    public bool isCharging;
    public bool isDepot;
}

[System.Serializable]
public class ECVRPProblem
{
    public List<ECVRPNode> nodes;
    public float[,] adjacencyMatrix;
    public int vehlicles;
    public float capacity;
    public float energyCapacity;
    public float energyConsumption;

    public void CreateAdjacencyMatrix()
    {
        adjacencyMatrix = new float[nodes.Count, nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
        {
            for(int j = 0; j < i; j++)
            {
                adjacencyMatrix[i,j] = Vector2.Distance(nodes[i].coordinate, nodes[j].coordinate);
                adjacencyMatrix[j, i] = adjacencyMatrix[i, j];
            }
        }

        string log = "";
        for (int i = 0; i < adjacencyMatrix.Length; i++)
        {
            for (int j = 0; j < adjacencyMatrix.Length; j++)
            {
                log += adjacencyMatrix[i, j] + " ";
            }
            log += "\n";
        }
        Debug.Log(log);
    }
}

public class ECVRPMgr : MonoBehaviour
{
    public static ECVRPMgr inst;
    public ECVRPProblem problem;
    // Start is called before the first frame update
    void Awake()
    {
        inst = this;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Init()
    {
        problem = DataMgr.inst.ReadEVRPFile();

    }
}
