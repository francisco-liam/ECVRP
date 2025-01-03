using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Schema;
using UnityEditor;
using UnityEngine;

public class GAMgr : MonoBehaviour
{
    public static GAMgr inst;
    public List<Vector2> nodeCoordinates;
    List<int> nodeNumbers;
    public UnityEngine.Object file;

    public int populationSize;
    public int maxGenerations;
    public List<Individual> population;
    public List<Individual> parents;
    public List<Individual> children;

    public float pMutation;
    public float pCrossover;

    string filePath;

    // Start is called before the first frame update
    void Awake()
    {
        inst = this;
        filePath = AssetDatabase.GetAssetPath(file);
        InitializeNodes();
        InitializePopulation();
        RunGA();

    }

    bool readingCoords = false;
    public void InitializeNodes()
    {
        nodeCoordinates = new List<Vector2>();
        nodeNumbers = new List<int>();

        StreamReader sr = new StreamReader(filePath);
        string line = sr.ReadLine();
        string[] coord;

        while (line != null)
        {
            if (line == "EOF")
                readingCoords = false;

            if (readingCoords)
            {
                line = line.Trim();
                line = Regex.Replace(line, @"\s+", " ");
                coord = line.Split();
                nodeCoordinates.Add(new Vector2(float.Parse(coord[1]), float.Parse(coord[2])));
                nodeNumbers.Add(nodeNumbers.Count);
            }

            if (line == "NODE_COORD_SECTION")
                readingCoords = true;

            line = sr.ReadLine();
        }
    }

    public void InitializePopulation()
    {
        population = new List<Individual>();
        for(int i = 0; i < populationSize; i++)
        {
            System.Random rand = new System.Random();
            List<int> shuffledList = nodeNumbers.OrderBy(_ => rand.Next()).ToList();
            population.Add(new Individual(shuffledList));
        }
    }

    //PMX crossover
    public void Crossover(Individual p0, Individual p1)
    {
        int index1 = UnityEngine.Random.Range(0, p1.chromosome.Count);
        int index2 = UnityEngine.Random.Range(0, p1.chromosome.Count);
        while (index2 == index1)
            index2 = UnityEngine.Random.Range(0, p1.chromosome.Count);
        if(index2 < index1)
        {
            int tmp = index2;
            index2 = index1;
            index1 = tmp;
        }

        children.Add(new Individual(GetChildFromParents(p0, p1, index1, index2)));
        children.Add(new Individual(GetChildFromParents(p1, p0, index1, index2)));
    }

    public List<int> GetChildFromParents(Individual p0, Individual p1, int index1, int index2) 
    {
        List<int> child = new List<int>();
        for (int i = 0; i < p1.chromosome.Count; i++)
        {
            child.Add(-1);
        }

        for (int i = index1; i <= index2; i++)
        {
            child[i] = p0.chromosome[i];
        }

        for (int i = index1; i <= index2; i++)
        {
            if (child.Contains(p1.chromosome[i]))
                continue;

            int indexOfNinP1 = p1.chromosome.IndexOf(p0.chromosome[i]);
            while (child[indexOfNinP1] != -1)
            {
                int symbolAtIndex = child[indexOfNinP1];
                indexOfNinP1 = p1.chromosome.IndexOf(symbolAtIndex);
            }

            child[indexOfNinP1] = p1.chromosome[i];
        }

        for(int i = 0; i < p1.chromosome.Count; i++)
        {
            if (child[i] == -1)
                child[i] = p1.chromosome[i];
        }

        return child;
    }

    public void Selection()
    {
        parents = new List<Individual>();
        for (int i = 0; i < population.Count; i++)
        {
            int index = GetProportionalIndex();
            parents.Add(population[index]);
        }
    }

    public int GetProportionalIndex()
    {
        float totalFitness = 0;
        foreach(Individual individual in population)
        {
            totalFitness += individual.fitness;
        }

        float threshold = 0;
        float p = UnityEngine.Random.Range(0, totalFitness);
        int index = -1;

        while (p >= threshold)
        {
            index++;
            threshold += population[index].fitness;
        }

        return index;
    }

    List<Individual> parentsAndChildren;
    public void Halve()
    {
        parentsAndChildren = new List<Individual>();
        parentsAndChildren.AddRange(parents);
        parentsAndChildren.AddRange(children);
        parentsAndChildren = parentsAndChildren.OrderBy(o => o.fitness).ToList();
        for(int i = 0; i < parents.Count; i++)
        {
            parentsAndChildren.RemoveAt(0);
        }
        population = parentsAndChildren;
    }

    public void RunGA()
    {
        Selection();

        children = new List<Individual>();
        for(int i = 0; i < population.Count; i += 2)
        {
            if(UnityEngine.Random.Range(0f, 1f) < pCrossover)
                Crossover(parents[i], parents[i + 1]);
            else
            {
                children.Add(parents[i]);
                children.Add(parents[i + 1]);
            }
        }

        foreach(Individual child in children)
            child.Mutation();

        Halve();
    }

    int generation = 0;
    // Update is called once per frame
    void Update()
    {
        if(generation < maxGenerations)
        {
            RunGA();
            float averagePathLength = 0f;
            foreach (Individual individual in population)
                averagePathLength += individual.pathLength;
            averagePathLength /= population.Count;
            Debug.Log(averagePathLength);
            generation++;
        }
    }
}
