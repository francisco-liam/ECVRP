using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class Individual
{
    public List<int> chromosome;
    public int pathLength;
    public float fitness;

    public Individual(List<int> chromosome)
    {
        this.chromosome = chromosome;
        Evaluate();
    }
    
    public void Evaluate()
    {
        pathLength = 0;
        for(int i = 0; i < chromosome.Count; i++)
        {
            if(i != chromosome.Count-1 && chromosome[i] != -1 && chromosome[i+1] != -1)
                pathLength += Mathf.RoundToInt(Vector2.Distance(GAMgr.inst.nodeCoordinates[chromosome[i]], GAMgr.inst.nodeCoordinates[chromosome[i+1]]));
            else if(chromosome[i] != -1 && chromosome[0] != -1)
                pathLength += Mathf.RoundToInt(Vector2.Distance(GAMgr.inst.nodeCoordinates[chromosome[i]], GAMgr.inst.nodeCoordinates[chromosome[0]]));
        }
        fitness = 1f / pathLength;
    }

    //swaps two elements
    public void Mutation()
    {
        float p = Random.Range(0f, 1f);

        if (p > GAMgr.inst.pMutation)
            return;

        int index1 = Random.Range(0, chromosome.Count);
        int index2 = Random.Range(0, chromosome.Count);
        while (index2 == index1)
            index2 = Random.Range(0, chromosome.Count);

        int tmp = chromosome[index1];
        chromosome[index1] = chromosome[index2];
        chromosome[index2] = tmp;
        Evaluate();
    }

    public override string ToString()
    {
        string output = "Chromosome: ";

        foreach(int i in chromosome)
        {
            output += i.ToString() + " ";
        }

        output += "\nPath Length: " + pathLength.ToString();
        output += "\nFitness: " + fitness.ToString();

        return output;
    }
}
