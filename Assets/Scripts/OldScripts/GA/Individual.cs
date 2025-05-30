using System.Collections.Generic;
using UnityEngine;

/*
[System.Serializable]
public class Individual
{
    public List<int> chromosome;
    public float fitness;
    
    public Individual()
    {

    }

    public Individual(List<int> chromosome)
    {
        this.chromosome = chromosome;
    }

    public virtual void Evaluate()
    {
        
        pathLength = 0;
        for(int i = 0; i < chromosome.Count; i++)
        {
            if(i != chromosome.Count-1 && chromosome[i] != -1 && chromosome[i+1] != -1)
                pathLength += Mathf.RoundToInt(Vector2.Distance(OldGAMgr.inst.nodeCoordinates[chromosome[i]], OldGAMgr.inst.nodeCoordinates[chromosome[i+1]]));
            else if(chromosome[i] != -1 && chromosome[0] != -1)
                pathLength += Mathf.RoundToInt(Vector2.Distance(OldGAMgr.inst.nodeCoordinates[chromosome[i]], OldGAMgr.inst.nodeCoordinates[chromosome[0]]));
        }
        fitness = 1f / pathLength;
        
    }

    public virtual void Mutation()
    {
        int index1 = Random.Range(0, chromosome.Count);
        int index2 = Random.Range(0, chromosome.Count);
        while (index2 == index1)
            index2 = Random.Range(0, chromosome.Count);

        Debug.Log(index1);
        Debug.Log(index2);

        if (index2 < index1)
        {
            int tmp = index2;
            index2 = index1;
            index1 = tmp;
        }

        List<int> newChromosome = new List<int>(chromosome);

        for (int i = 0; i <= index2 - index1 ; i++) 
        {
            newChromosome[index1 + i] = chromosome[index2 - i];
        }
        chromosome = newChromosome;
        Evaluate();  
    }

    public virtual List<int> DecodeChromosome(int segmentLength)
    {
        List<int> decodedChromosome = new List<int>();

        for(int i = 0; i < chromosome.Count; i+= segmentLength)
        {
            int decodedValue = 0;
            for(int j = 0; j < segmentLength; j++)
                decodedValue += (int)(chromosome[i+segmentLength-1-j] * Mathf.Pow(2, j));
            decodedChromosome.Add(decodedValue);
        }

        return decodedChromosome;
    }

    public override string ToString()
    {
        
        string output = "Chromosome: ";

        foreach(int i in chromosome)
        {
            output += i.ToString() + " ";
        }

        output += "\nFitness: " + fitness.ToString();

        return output;
    }
}
*/
