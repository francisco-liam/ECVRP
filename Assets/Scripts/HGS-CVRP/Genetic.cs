using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;

public class Genetic : MonoBehaviour
{
    HGSCVRPIndividual offspring;

    bool ran;

    private void Update()
    {
        if (!ran)
        {
            HGSCVRPIndividual hGSCVRPIndividual = new HGSCVRPIndividual();
            run();
            ran = true;
        }
        
    }


    void run()
    {
        /* INITIAL Population.inst */
        Population.inst.generatePopulation();

        int nbIter;
        int nbIterNonProd = 1;
        if (Parameters.inst.verbose) Debug.Log("----- STARTING GENETIC ALGORITHM");
        for (nbIter = 0; nbIterNonProd <= Parameters.inst.ap.nbIter /*&& (Parameters.inst.ap.timeLimit == 0 || (double)(clock() -Parameters.inst.startTime)/ (double)CLOCKS_PER_SEC < Parameters.inst.ap.timeLimit)*/ ; nbIter++)
	    {
            /* SELECTION AND CROSSOVER */
            crossoverOX(offspring, Population.inst.getBinaryTournament(), Population.inst.getBinaryTournament());

            /* LOCAL SEARCH */
            LocalSearch.inst.run(offspring, Parameters.inst.penaltyCapacity, Parameters.inst.penaltyDuration);
            bool isNewBest = Population.inst.addIndividual(offspring, true);
            if (!offspring.eval.isFeasible && Random.Range(0,10) % 2 == 0) // Repair half of the solutions in case of infeasibility
		    {
                LocalSearch.inst.run(offspring, Parameters.inst.penaltyCapacity * 10, Parameters.inst.penaltyDuration * 10);
                if (offspring.eval.isFeasible) isNewBest = (Population.inst.addIndividual(offspring, false) || isNewBest);
            }

            /* TRACKING THE NUMBER OF ITERATIONS SINCE LAST SOLUTION IMPROVEMENT */
            if (isNewBest) nbIterNonProd = 1;
            else nbIterNonProd++;

            /* DIVERSIFICATION, PENALTY MANAGEMENT AND TRACES 
            if (nbIter % Parameters.inst.ap.nbIterPenaltyManagement == 0) Population.inst.managePenalties();
            if (nbIter % Parameters.inst.ap.nbIterTraces == 0) Population.inst.printState(nbIter, nbIterNonProd);
            */

            /* FOR TESTS INVOLVING SUCCESSIVE RUNS UNTIL A TIME LIMIT: WE RESET THE ALGORITHM/Population.inst EACH TIME maxIterNonProd IS ATTAINED
            if (Parameters.inst.ap.timeLimit != 0 && nbIterNonProd == Parameters.inst.ap.nbIter)
		    {
                Population.inst.restart();
                nbIterNonProd = 1;
            }
            */
        }
        //if (Parameters.inst.verbose) Debug.Log("----- GENETIC ALGORITHM FINISHED AFTER " << nbIter << " ITERATIONS. TIME SPENT: " << (double)(clock() - Parameters.inst.startTime) / (double)CLOCKS_PER_SEC << std::endl;
    }

    void crossoverOX(HGSCVRPIndividual result, HGSCVRPIndividual parent1, HGSCVRPIndividual parent2)
    {
	    // Frequency table to track the customers which have been already inserted
	    List<bool> freqClient = Enumerable.Repeat(false, Parameters.inst.nbClients + 1).ToList();

        // Picking the beginning and end of the crossover zone
        int start = Random.Range(0, Parameters.inst.nbClients - 1);
        int end = Random.Range(0, Parameters.inst.nbClients - 1);

        // Avoid that start and end coincide by accident
        while (end == start) end = Random.Range(0, Parameters.inst.nbClients - 1);

        // Copy from start to end
        int j = start;
	    while (j % Parameters.inst.nbClients != (end + 1) % Parameters.inst.nbClients)
	    {
		    result.chromT[j % Parameters.inst.nbClients] = parent1.chromT[j % Parameters.inst.nbClients];
		    freqClient[result.chromT[j % Parameters.inst.nbClients]] = true;
		    j++;
	    }

        // Fill the remaining elements in the order given by the second parent
        for (int i = 1; i <= Parameters.inst.nbClients; i++)
	    {
            int temp = parent2.chromT[(end + i) % Parameters.inst.nbClients];
            if (freqClient[temp] == false)
            {
                result.chromT[j % Parameters.inst.nbClients] = temp;
                j++;
            }
        }

        // Complete the individual with the Split algorithm
        Split.inst.generalSplit(result, parent1.eval.nbRoutes);
    }


}
