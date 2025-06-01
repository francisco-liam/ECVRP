using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using static UnityEngine.Networking.UnityWebRequest;

public class GeneticMgr : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    int nbIter = 0;
    int nbIterNonProd = 1;
    public void Init()
    {
        PopulationMgr.inst.GeneratePopulation();
    }

    // Update is called once per frame
    void Update()
    {
        if(nbIterNonProd <= CVRPMgr.inst.ap.nbIter && (CVRPMgr.inst.ap.timeLimit == 0 || Time.realtimeSinceStartup - CVRPMgr.inst.startTime < CVRPMgr.inst.ap.timeLimit))
        {
            nbIter++;

            /* SELECTION AND CROSSOVER */
            Individual offspring = CrossoverOX(PopulationMgr.inst.GetBinaryTournament(), PopulationMgr.inst.GetBinaryTournament());

            /* LOCAL SEARCH */
            LocalSearchMgr.inst.Run(offspring, CVRPMgr.inst.penaltyCapacity, CVRPMgr.inst.penaltyDuration);
            bool isNewBest = PopulationMgr.inst.AddIndividual(offspring, true);
            if (!offspring.eval.isFeasible && Random.Range(0, 2) % 2 == 0) // Repair half of the solutions in case of infeasibility
		{
                LocalSearchMgr.inst.Run(offspring, CVRPMgr.inst.penaltyCapacity*10, CVRPMgr.inst.penaltyDuration*10); ;
                if (offspring.eval.isFeasible) isNewBest = (PopulationMgr.inst.AddIndividual(offspring, false) || isNewBest);
            }

        }
    }

    Individual CrossoverOX(Individual parent1, Individual parent2)
    {
        Individual result = new Individual();
        
        // Frequency table to track the customers which have been already inserted
        bool[] freqClient = new bool[CVRPMgr.inst.problem.customers + 1];

        // Picking the beginning and end of the crossover zone
        int start = Random.Range(0, CVRPMgr.inst.problem.customers);
        int end = Random.Range(0, CVRPMgr.inst.problem.customers);

        // Avoid that start and end coincide by accident
        while (end == start) end = Random.Range(0, CVRPMgr.inst.problem.customers);

        // Copy from start to end
        int j = start;
        while (j % CVRPMgr.inst.problem.customers != (end + 1) % CVRPMgr.inst.problem.customers)
	{
            result.chromT[j % CVRPMgr.inst.problem.customers] = parent1.chromT[j % CVRPMgr.inst.problem.customers];
            freqClient[result.chromT[j % CVRPMgr.inst.problem.customers]] = true;
            j++;
        }

        // Fill the remaining elements in the order given by the second parent
        for (int i = 1; i <= CVRPMgr.inst.problem.customers; i++)
	    {
            int temp = parent2.chromT[(end + i) % CVRPMgr.inst.problem.customers];
            if (freqClient[temp] == false)
            {
                result.chromT[j % CVRPMgr.inst.problem.customers] = temp;
                j++;
            }
        }

        // Complete the individual with the Split algorithm
        SplitMgr.inst.GeneralSplit(result, parent1.eval.nbRoutes);

        return result;

    }
}
