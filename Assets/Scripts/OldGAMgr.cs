using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;


public class OldGAMgr : MonoBehaviour
{
    public static OldGAMgr inst;
    public List<Vector2> nodeCoordinates;
    List<int> nodeNumbers;
    public UnityEngine.Object file;
    public ComputeShader shader;

    public int populationSize;
    public int maxGenerations;
    public List<Individual> population;
    List<Individual> parents;
    public List<Individual> children;
    public int chromosomeSize;
    public List<int> test;

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
        //test = CreateChromosomeBuffer(population);
        //CSRunGA();
        //Crossover(population[0], population[1]);
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

        chromosomeSize = population[0].chromosome.Count;
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

        Debug.Log(index1);
        Debug.Log(index2);

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

    float totalFitness;

    public void Selection()
    {
        parents = new List<Individual>();
        SetTotalFitness();
        for (int i = 0; i < population.Count; i++)
        {
            int index = GetProportionalIndex();
            parents.Add(population[index]);
        }
    }

    public void SetTotalFitness()
    {
        totalFitness = 0;
        foreach (Individual individual in population)
        {
            totalFitness += individual.fitness;
        }
    }

    public int GetProportionalIndex()
    {
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

    /*
    public int randTestSize;
    public List<int> randTest;
    public List<int> csChildren;
    public List<int> csPopulation;
    public void CSRunGA()
    {
        //Selection();

        List<int> populationGenes = CreateChromosomeBuffer(population);
        List<int> pathLengths = new List<int>();
        List<float> fitnesses = new List<float>();
        foreach (Individual individual in population)
        {
            pathLengths.Add(individual.pathLength);
            fitnesses.Add(individual.fitness);
        }
        
        ComputeBuffer populationBuffer = new ComputeBuffer(populationGenes.Count, sizeof(int));
        populationBuffer.SetData(populationGenes);

        ComputeBuffer pathBuffer = new ComputeBuffer(population.Count, sizeof(int));
        pathBuffer.SetData(pathLengths);

        ComputeBuffer fitnessBuffer = new ComputeBuffer(population.Count, sizeof(float));
        fitnessBuffer.SetData(fitnesses);

        ComputeBuffer randBuffer = new ComputeBuffer(randTestSize, sizeof(int));

        ComputeBuffer childrenBuffer = new ComputeBuffer(populationGenes.Count, sizeof(int));

        shader.SetInt("frameIndex", Random.Range(0,1000000));
        shader.SetInt("chromosomeSize", chromosomeSize);
        shader.SetInt("populationSize", population.Count);
        shader.SetFloat("pCrossover", pCrossover);
        shader.SetFloat("pCrossover", pCrossover);
        shader.SetBuffer(0, "population", populationBuffer);
        shader.SetBuffer(0, "children", childrenBuffer);
        shader.SetBuffer(0, "pathLengths", pathBuffer);
        shader.SetBuffer(0, "fitnesses", fitnessBuffer);
        shader.SetBuffer(0, "randTest", randBuffer);

        shader.Dispatch(0, 64, 1, 1);

        int[] randTestArray = new int[randTestSize];
        randBuffer.GetData(randTestArray);
        randTest = new List<int>(randTestArray);

        int[] csChildrenArray = new int[population.Count * chromosomeSize];
        childrenBuffer.GetData(csChildrenArray);
        csChildren = new List<int>(csChildrenArray);

        int[] csPopulationArray = new int[population.Count * chromosomeSize];
        populationBuffer.GetData(csPopulationArray);
        csPopulation = new List<int>(csPopulationArray);

        Debug.Log(randTest[0]);
        Debug.Log(randTest[1]);

        populationBuffer.Dispose();
        childrenBuffer.Dispose();
        pathBuffer.Dispose();
        fitnessBuffer.Dispose();
        randBuffer.Dispose();
    }

    public List<int> CreateChromosomeBuffer(List<Individual> pop)
    {
        List<int> buffer = new List<int>();
        foreach(Individual individual in pop)
        {
            buffer.AddRange(individual.chromosome);
        }
        return buffer;
    }
    
    public void LoadBufferToPropulation(List<int> buffer)
    {
        for(int i = 0; i < population.Count; i++)
        {
            population[i].chromosome = buffer.GetRange(i*chromosomeSize, chromosomeSize);
        }
    }
    */
    int generation = 0;
    public float time;
    // Update is called once per frame
    void Update()
    {
        /*
        if(generation < maxGenerations)
        {
            RunGA();
            float averagePathLength = 0f;
            foreach (Individual individual in population)
                averagePathLength += individual.pathLength;
            averagePathLength /= population.Count;
            Debug.Log("Generation: " + generation + " " + averagePathLength);
            generation++;
            time += Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            LoadBufferToPropulation(test);
        }
        */
    }
}
