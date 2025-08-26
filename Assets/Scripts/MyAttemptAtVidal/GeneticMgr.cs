using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using static UnityEngine.Networking.UnityWebRequest;

public class GeneticMgr : MonoBehaviour
{
    public static GeneticMgr inst;
    private void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public int nbIter;
    public int nbIterNonProd;
    public void Init()
    {
        nbIter = 0;
        nbIterNonProd = 1;
        PopulationMgr.inst.GeneratePopulation();
        Debug.Log($"----- STARTING GENETIC ALGORITHM RUN {CVRPMain.inst.run}");
    }

    public bool running;
    public bool write;
    public bool done;
    // Update is called once per frame
    void Update()
    {
        if (running)
        {
            bool condition = ParametersMgr.inst.ap.useSetNbOfIter ? nbIter < ParametersMgr.inst.ap.nbIter :
                nbIterNonProd <= ParametersMgr.inst.ap.nbIter &&
                (ParametersMgr.inst.ap.timeLimit == 0 || Time.realtimeSinceStartup - ParametersMgr.inst.startTime < ParametersMgr.inst.ap.timeLimit);

            if (condition)
            {
                running = true;
                nbIter++;
                //Debug.Log(nbIter);

                /* SELECTION AND CROSSOVER */
                Individual offspring = CrossoverOX(PopulationMgr.inst.GetBinaryTournament(), PopulationMgr.inst.GetBinaryTournament());

                /* LOCAL SEARCH */
                LocalSearchMgr.inst.Run(offspring, ParametersMgr.inst.penaltyCapacity, ParametersMgr.inst.penaltyDuration);
                bool isNewBest = PopulationMgr.inst.AddIndividual(offspring, true);
                if (!offspring.eval.isFeasible && ParametersMgr.inst.ran.LCG() % 2 == 0) // Repair half of the solutions in case of infeasibility
                {
                    LocalSearchMgr.inst.Run(offspring, ParametersMgr.inst.penaltyCapacity * 10, ParametersMgr.inst.penaltyDuration * 10); ;
                    if (offspring.eval.isFeasible) isNewBest = (PopulationMgr.inst.AddIndividual(offspring, false) || isNewBest);
                }

                /* TRACKING THE NUMBER OF ITERATIONS SINCE LAST SOLUTION IMPROVEMENT */
                if (isNewBest) nbIterNonProd = 1;
                else nbIterNonProd++;

                /* DIVERSIFICATION, PENALTY MANAGEMENT AND TRACES */
                if (nbIter % ParametersMgr.inst.ap.nbIterPenaltyManagement == 0) PopulationMgr.inst.ManagePenalties();
                if (nbIter % ParametersMgr.inst.ap.nbIterTraces == 0) PopulationMgr.inst.PrintState(nbIter, nbIterNonProd);

                /* FOR TESTS INVOLVING SUCCESSIVE RUNS UNTIL A TIME LIMIT: WE RESET THE ALGORITHM/POPULATION EACH TIME maxIterNonProd IS ATTAINED
                if (params.ap.timeLimit != 0 && nbIterNonProd == params.ap.nbIter)
                {
                    population.restart();
                    nbIterNonProd = 1;
                }
                */

                StatsMgr.inst.RecordRunData();
            }
            else if (running)
            {
                running = false;
                Debug.Log(string.Format(
                    "----- GENETIC ALGORITHM FINISHED AFTER {0} ITERATIONS. TIME SPENT: {1:F2}",
                    nbIter,
                    Time.realtimeSinceStartup - ParametersMgr.inst.startTime));

                if (write)
                {
                    FileWriterMgr.inst.AppendMetricCSV();
                    FileWriterMgr.inst.WriteGraphCSV(FileWriterMgr.inst.fileNames[1],
                        StatsMgr.inst.CalculateGenerationAveragesOverRuns(StatsMgr.inst.minFeasiblePopulationCost));
                    FileWriterMgr.inst.WriteGraphCSV(FileWriterMgr.inst.fileNames[2],
                        StatsMgr.inst.CalculateGenerationAveragesOverRuns(StatsMgr.inst.avgFeasiblePopulationCost));
                }
                done = true;
            }
        }
        else if (CVRPMain.inst.run < CVRPMain.inst.maxRuns - 1)
        {
            SceneManager.LoadScene(1);
        }
        else
        {
            Application.Quit();
        }
    }

    Individual CrossoverOX(Individual parent1, Individual parent2)
    {
        Individual result = new Individual();
        
        // Frequency table to track the customers which have been already inserted
        bool[] freqClient = new bool[ParametersMgr.inst.nbClients + 1];

        // Picking the beginning and end of the crossover zone
        int start = ParametersMgr.inst.ran.Range(0, ParametersMgr.inst.nbClients - 1);
        int end = ParametersMgr.inst.ran.Range(0, ParametersMgr.inst.nbClients - 1);

        // Avoid that start and end coincide by accident
        while (end == start) end = ParametersMgr.inst.ran.Range(0, ParametersMgr.inst.nbClients - 1);

        // Copy from start to end
        int j = start;
        while (j % ParametersMgr.inst.nbClients != (end + 1) % ParametersMgr.inst.nbClients)
	    {
            result.chromT[j % ParametersMgr.inst.nbClients] = parent1.chromT[j % ParametersMgr.inst.nbClients];
            freqClient[result.chromT[j % ParametersMgr.inst.nbClients]] = true;
            j++;
        }

        // Fill the remaining elements in the order given by the second parent
        for (int i = 1; i <= ParametersMgr.inst.nbClients; i++)
	    {
            int temp = parent2.chromT[(end + i) % ParametersMgr.inst.nbClients];
            if (freqClient[temp] == false)
            {
                result.chromT[j % ParametersMgr.inst.nbClients] = temp;
                j++;
            }
        }

        // Complete the individual with the Split algorithm
        SplitMgr.inst.GeneralSplit(result, parent1.eval.nbRoutes);

        return result;

    }
}
