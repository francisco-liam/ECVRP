using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;

/*
[System.Serializable]
public enum ProblemType
{
    TSP,
    ECVRP
}

public class GAMgr : MonoBehaviour
{
    public static GAMgr inst;
    public ProblemType problemType;
    public List<Individual> population;
    public int populationSize;
    public int chromosomeSize;
    public float pMutation;
    public float pCrossover;
    public int maxGenerations;

    Dictionary<ProblemType, Func<List<int>, Individual>> problemFactory;

    List<Individual> parents;
    List<Individual> children;
    List<Individual> parentsAndChildren;

    // Start is called before the first frame update
    void Awake()
    {
        inst = this;
        problemFactory = new Dictionary<ProblemType, Func<List<int>, Individual>>
        {
            { ProblemType.TSP, chromosome => new TSPIndividual(chromosome) },
            { ProblemType.ECVRP, chromosome => new ECVRPIndividual(chromosome) },
        };
    }

    public void Init()
    {
        InitializePopulation();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InitializePopulation()
    {
        population = new List<Individual>();
        for (int i = 0; i < populationSize; i++)
        {
            List<int> chromosome = new List<int>();
            for (int j = 0; j < chromosomeSize; j++)
            {
                chromosome.Add(Random.Range(0, 2));
            }
            population.Add(problemFactory[problemType](chromosome));
        }
    }

    public void Selection()
    {
        parents = new List<Individual>();

        float totalFitness = 0;
        foreach (Individual individual in population)
        {
            totalFitness += individual.fitness;
        }

        for (int i = 0; i < population.Count; i++)
        {
            int index = GetProportionalIndex(totalFitness);
            parents.Add(population[index]);
        }
    }

    public int GetProportionalIndex(float totalFitness)
    {
        float threshold = 0;
        float p = Random.Range(0, totalFitness);
        int index = -1;

        while (p >= threshold)
        {
            index++;
            threshold += population[index].fitness;
        }

        return index;
    }

    
    public void Halve()
    {
        parentsAndChildren = new List<Individual>();
        parentsAndChildren.AddRange(parents);
        parentsAndChildren.AddRange(children);
        parentsAndChildren = parentsAndChildren.OrderBy(o => o.fitness).ToList();
        for (int i = 0; i < parents.Count; i++)
        {
            parentsAndChildren.RemoveAt(0);
        }
        population = parentsAndChildren;
    }

    //two point crossover
    public void Crossover(Individual p0, Individual p1)
    {
        int index1 = Random.Range(0, p1.chromosome.Count);
        int index2 = Random.Range(0, p1.chromosome.Count);
        while (index2 == index1)
            index2 = Random.Range(0, p1.chromosome.Count);
        if (index2 < index1)
        {
            int tmp = index2;
            index2 = index1;
            index1 = tmp;
        }

        List<int> child0Chromosome = new List<int>();
        List<int> child1Chromosome = new List<int>();

        for(int i = 0; i < chromosomeSize; i++) 
        { 
            if(i >= index1 && i <= index2)
            {
                child0Chromosome.Add(p1.chromosome[i]);
                child1Chromosome.Add(p0.chromosome[i]);
            }
            else
            {
                child0Chromosome.Add(p0.chromosome[i]);
                child1Chromosome.Add(p1.chromosome[i]);
            }
        }

        children.Add(problemFactory[problemType](child0Chromosome));
        children.Add(problemFactory[problemType](child1Chromosome));
    }

    int generationNumber = 0;
    public void RunGeneration()
    {
        if(generationNumber < maxGenerations)
        {
            Selection();

            children = new List<Individual>();
            for (int i = 0; i < population.Count; i += 2)
            {
                if (Random.Range(0f, 1f) < pCrossover)
                    Crossover(parents[i], parents[i + 1]);
                else
                {
                    children.Add(parents[i]);
                    children.Add(parents[i + 1]);
                }
            }

            foreach (Individual child in children)
            {
                if (Random.Range(0f, 1f) < pMutation)
                    child.Mutation();
            }

            Halve();

            generationNumber++;
        }
        
    }

    public void GenerateGenerationStats()
    {
        float averageFitness = 0f;
        foreach (Individual individual in population)
            averageFitness += individual.fitness;
        averageFitness /= population.Count;
        Debug.Log("Generation: " + generationNumber + " " + averageFitness);
    }
}
*/