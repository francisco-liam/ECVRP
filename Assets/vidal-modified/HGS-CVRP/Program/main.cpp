#include "Genetic.h"
#include "commandline.h"
#include "LocalSearch.h"
#include "Split.h"
#include "InstanceCVRPLIB.h"
using namespace std;

std::vector<std::tuple<int, int, int>> ReadTuplesFromFile(const std::string& filename, int x) {
    std::ifstream file(filename);
    std::vector<std::tuple<int, int, int>> result;

    if (!file.is_open()) {
        std::cerr << "Error: Cannot open file " << filename << std::endl;
        return result;
    }

    std::string line;
    int count = 0;

    while (std::getline(file, line) && count < x) {
        std::istringstream iss(line);
        int a, b, c;

        if (iss >> a >> b >> c) {
            result.emplace_back(a, b, c);
            count++;
        } else {
            std::cerr << "Warning: Line " << count + 1 << " does not contain three integers: " << line << std::endl;
        }
    }

    file.close();
    return result;
}

int main(int argc, char *argv[])
{
	try
	{
		// Reading the arguments of the program
		CommandLine commandline(argc, argv);

		// Print all algorithm parameter values
		if (commandline.verbose) print_algorithm_parameters(commandline.ap);

		// Reading the data file and initializing some data structures
		if (commandline.verbose) std::cout << "----- READING INSTANCE: " << commandline.pathInstance << std::endl;
		InstanceCVRPLIB cvrp(commandline.pathInstance, commandline.isRoundingInteger);

		Params params(cvrp.x_coords,cvrp.y_coords,cvrp.dist_mtx,cvrp.service_time,cvrp.demands,
			          cvrp.vehicleCapacity,cvrp.durationLimit,commandline.nbVeh,cvrp.isDurationConstraint,commandline.verbose,commandline.ap);

		// Running HGS
		Genetic solver(params);
		solver.run();
		
		// solver.split.generalSplit(solver.offspring, params.nbVehicles);
		// auto tuples = ReadTuplesFromFile("moves.txt", 154);

		// solver.localSearch.penaltyCapacityLS = params.penaltyCapacity;
		// solver.localSearch.penaltyDurationLS = params.penaltyDuration;
		// solver.localSearch.loadIndividual(solver.offspring);

    	// for (const auto& t : tuples) {
		//  	solver.localSearch.performMove(get<0>(t), std::get<1>(t), std::get<2>(t));
		// }
		// solver.localSearch.exportIndividual(solver.offspring);
		// solver.localSearch.run(solver.offspring, params.penaltyCapacity, params.penaltyDuration);

		// for (int value : solver.offspring.chromT)
		// {
		// 	std::cout << value << " ";
		// }
		// std::cout << std::endl;

		// // Exporting the best solution
		// if (params.verbose) std::cout << "----- WRITING BEST SOLUTION IN : " << commandline.pathSolution << std::endl;
		// solver.population.exportCVRPLibFormat(solver.offspring,commandline.pathSolution);
		// solver.population.exportSearchProgress(commandline.pathSolution + ".PG.csv", commandline.pathInstance);

		// Exporting the best solution
		if (solver.population.getBestFound() != NULL)
		{
			if (params.verbose) std::cout << "----- WRITING BEST SOLUTION IN : " << commandline.pathSolution << std::endl;
			solver.population.exportCVRPLibFormat(*solver.population.getBestFound(),commandline.pathSolution);
			solver.population.exportSearchProgress(commandline.pathSolution + ".PG.csv", commandline.pathInstance);
		}
	}
	catch (const string& e) { std::cout << "EXCEPTION | " << e << std::endl; }
	catch (const std::exception& e) { std::cout << "EXCEPTION | " << e.what() << std::endl; }
	return 0;
}

