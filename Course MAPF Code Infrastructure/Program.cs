//
// The file was modified by Asaf Hecht.
// As part of the personal project in the "Collaborating Machines: Theories and Applications” course.
// The project subject: "Solving MAPF with Bi-Directional A*star".
// Date of submission: 17.07.2017
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace CPF_experiment
{
    /// <summary>
    /// This is the entry point of the application. 
    /// </summary>
    class Program
    {
        private static string RESULTS_FILE_NAME = "Results.csv"; // Overridden by Main
        private static bool onlyReadInstances = false;

        // ******** Some of the project modifications: ********
        // "positionsOfMiddleStage" will be the middle state of the MAPF problem when solving with regular full Astar
        public static String positionsOfMiddleStage = "";
        // "middlePosition" is the array of the middle positions of all the agents, also if they were calculated automatically
        public static List<int[]> middlePosition = new List<int[]> { };
        // "trueMiddlePosition" is an array of the middle positions and will be used as a reference state
        public static List<int[]> trueMiddlePosition = new List<int[]> { };
        // "realGoals" is the goal state of all the agents in the source full MAPF problem
        public static List<int[]> realGoals = new List<int[]> { };
        // "onlySingleAgentsInstances" is an array of all the single agent problems that will be solve separately to get the middle positions in an automating way
        // The number of "null" here should be larger than the actual number of agent you are tring to solve
        public static ProblemInstance[] onlySingleAgentsInstances = new ProblemInstance[] {null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null};
        // Here are global counting variables, they will be displayed in the end of the run as the final results sum-up
        public static int numberOfRun = 0;
        public static int timeFullAstar = 8;
        public static int timeBiDirectionalAstar = 0;
        public static int timeStartingAstar = 0;
        public static int timeReverseAstar = 0;
        public static int costFullAstar = 0;
        public static int costBiDirectionalAstar = 0;
        public static int costStartingAstar = 0;
        public static int costReverseAstar = 0;
        public static int expandedNodesFullAstar = 0;
        public static int expandedNodesBiDirectionalAstar = 0;
        public static int expandedNodesStartingAstar = 0;
        public static int expandedNodesReverseAstar = 0;
        public static int generatedNodesFullAstar = 0;
        public static int generatedNodesBiDirectionalAstar = 0;
        public static int generatedNodesStartingAstar = 0;
        public static int generatedNodesReverseAstar = 0;
        public static int numberOpenNodesFullAstar = 0;
        public static int numberOpenNodesBiDirectionalAstar = 0;
        public static int numberOpenNodesStartingAstar = 0;
        public static int numberOpenNodesReverseAstar = 0;
        // Choose "sloveWithAutomatedSingleAgentMiddlePosition" to be true or false for contorling the middle positons calculation method
        // If true  - the middle positions will be calculated from each separeted single agent solution in an automating way - this new method is fully described in the project report
        // If false - the middle positions will be calculated from the full Astar solution of all the agents together
        public static bool sloveWithAutomatedSingleAgentMiddlePosition = true;
        // version 2: adding the option to solve the problem in a Forward-Forward approach - the second part of the solution will also be solved using a forward solver and not using a reverse approach
        public static bool secondPartIsReverse = false;


        /// <summary>
        /// Simplest run possible with a randomly generated problem instance.
        /// </summary>
        public void SimpleRun()
        {
            Run runner = new Run();
            runner.OpenResultsFile(RESULTS_FILE_NAME);
            runner.PrintResultsFileHeader();
            ProblemInstance instance = runner.GenerateProblemInstance(10, 3, 10);
            instance.Export("Test.instance");
            runner.SolveGivenProblem(instance);            
            runner.CloseResultsFile();
        }

        /// <summary>
        /// Runs a single instance, imported from a given filename.
        /// </summary>
        /// <param name="fileName"></param>
        public void RunInstance(string fileName)
        {
            ProblemInstance instance;
            try
            {
                instance = ProblemInstance.Import(Directory.GetCurrentDirectory() + "\\Instances\\" + fileName);
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("Skipping bad problem instance {0}. Error: {1}", fileName, e.Message));
                return;
            }

            Run runner = new Run();
            bool resultsFileExisted = File.Exists(RESULTS_FILE_NAME);
            runner.OpenResultsFile(RESULTS_FILE_NAME);
            if (resultsFileExisted == false)
                runner.PrintResultsFileHeader();
            runner.SolveGivenProblem(instance);
            runner.CloseResultsFile();
        }

        /// <summary>
        /// Runs a set of experiments.
        /// This function will generate a random instance (or load it from a file if it was already generated)
        /// </summary>
        public void RunExperimentSet(int[] gridSizes, int[] agentListSizes, int[] obstaclesProbs, int instances)
        {
            ProblemInstance instance;
            ProblemInstance instanceForFirstAstar;
            ProblemInstance instanceForSecondReverseAstar;

            string instanceName;
            // build the run - with the defined solvers:
            Run runner = new Run();

            bool resultsFileExisted = File.Exists(RESULTS_FILE_NAME);
            runner.OpenResultsFile(RESULTS_FILE_NAME);
            if (resultsFileExisted == false)
                runner.PrintResultsFileHeader();

            bool continueFromLastRun = false; 
            string[] LastProblemDetails = null;
            string currentProblemFileName = Directory.GetCurrentDirectory() + "\\Instances\\current problem-" + Process.GetCurrentProcess().ProcessName;
            //# removed the last time check:
            /*
            if (File.Exists(currentProblemFileName)) //if we're continuing running from last time
            {
                var lastProblemFile = new StreamReader(currentProblemFileName);
                LastProblemDetails = lastProblemFile.ReadLine().Split(',');  //get the last problem
                lastProblemFile.Close();
                continueFromLastRun = true;
            }
            */
            // build the grid map:
            for (int gs = 0; gs < gridSizes.Length; gs++)
            {
                for (int obs = 0; obs < obstaclesProbs.Length; obs++)
                {
                    runner.ResetOutOfTimeCounters();
                    for (int ag = 0; ag < agentListSizes.Length; ag++)
                    {
                        if (gridSizes[gs] * gridSizes[gs] * (1 - obstaclesProbs[obs] / 100) < agentListSizes[ag]) // Probably not enough room for all agents
                            continue;
                        for (int i = 0; i < instances; i++)
                        {
                            string allocation = Process.GetCurrentProcess().ProcessName.Substring(1);
                            //if (i % 33 != Convert.ToInt32(allocation)) // grids!
                            //    continue;

                            //if (i % 5 != 0) // grids!
                            //    continue;

                            if (continueFromLastRun)  //set the latest problem
                            {
                                gs = int.Parse(LastProblemDetails[0]);
                                obs = int.Parse(LastProblemDetails[1]);
                                ag = int.Parse(LastProblemDetails[2]);
                                i = int.Parse(LastProblemDetails[3]);
                                for (int j = 4; j < LastProblemDetails.Length; j++)
                                {
                                    runner.outOfTimeCounters[j - 4] = int.Parse(LastProblemDetails[j]);
                                }
                                continueFromLastRun = false;
                                continue; // "current problem" file describes last solved problem, no need to solve it again
                            }
                            if (runner.outOfTimeCounters.Length != 0 &&
                                runner.outOfTimeCounters.Sum() == runner.outOfTimeCounters.Length * Constants.MAX_FAIL_COUNT) // All algs should be skipped
                                break;
                            instanceName = "Instance-" + gridSizes[gs] + "-" + obstaclesProbs[obs] + "-" + agentListSizes[ag] + "-" + i;
                            try
                            {
                                // ******** Some of the project modifications: ********
                                // For building new map everytime - chose the line of "WRONG-PATH"
                                numberOfRun = 1;    // numberOfRun 1 is the first solver of the MAPF problem
                                instance = ProblemInstance.Import(Directory.GetCurrentDirectory() + "\\Instances\\" + instanceName);
                                //instance = ProblemInstance.Import(Directory.GetCurrentDirectory() + "\\WRONG-PATH-Instances\\" + instanceName);
                                instance.instanceId = i;
                            }
                            catch (Exception importException)
                            {
                                if (onlyReadInstances)
                                {
                                    Console.WriteLine("File " + instanceName + "  dosen't exist");
                                    return;
                                }
                                // ubild the problem instance:
                                instance = runner.GenerateProblemInstance(gridSizes[gs], agentListSizes[ag], obstaclesProbs[obs] * gridSizes[gs] * gridSizes[gs] / 100);
                                instance.ComputeSingleAgentShortestPaths(); // REMOVE FOR GENERATOR
                                instance.instanceId = i;
                                // save the problem instance to a local file:
                                instance.Export(instanceName);
                            }

                            // ******** Some of the project modifications: ********
                            int numberOfAgents = instance.m_vAgents.Count();    // the number of agents in the MAPF problem

                            // If you chose to run the automated algorithm to find the middle positions
                            if (sloveWithAutomatedSingleAgentMiddlePosition) {
                                for (int j = 0; j < numberOfAgents; j++)
                                {
                                    onlySingleAgentsInstances[j].ComputeSingleAgentShortestPaths();
                                    runner.SolveGivenProblem(onlySingleAgentsInstances[j]);
                                }
                            }
                            // Finished solving the separated single agents 
                            Console.WriteLine("#################################################");
                            if (sloveWithAutomatedSingleAgentMiddlePosition)
                            {
                                // Printing the new calculated middle positions of our MAPF problem
                                Console.WriteLine("Here are the new caculated middle positions from the separeted single agents solutions:");
                                for (int j = 0; j < numberOfAgents; j++)
                                {
                                    Console.Write("|({1},{2})", j, middlePosition[j][0], middlePosition[j][1]);
                                }
                                Console.WriteLine("|");
                                Console.WriteLine("Great, you executed the automated middle positions algorithm!");
                            }

                            // This is the call for solving the source problem with the regular full Astar solver
                            runner.SolveGivenProblem(instance);

                            // Finished the regular full Astar solution and printing the middle positions from the middle of it
                            Console.WriteLine("-----------> Finished the regular A*star solution <-----------\n");
                            Console.WriteLine("The middle stage of the plan from full Astar on all the agents together:\n{0}", positionsOfMiddleStage);

                            numberOfRun = 2;    // numberOfRun 2 is the second run of the solver -> on the first half of the problem (from the start until the middle positions)
                            // Creating new problem instances that will be solve
                            // Load the same MAPF problem map
                            instanceForFirstAstar = ProblemInstance.Import(Directory.GetCurrentDirectory() + "\\Instances\\" + instanceName);
                            instanceForFirstAstar.instanceId = i + 1;
                            instanceForFirstAstar.instanceId = i + numberOfAgents + 1;
                            // If the solver calculated the middle position in the automating method - it will printed it and you can see the differences in the output
                            if (sloveWithAutomatedSingleAgentMiddlePosition)
                            {
                                Console.WriteLine("Here are the new caculated middle positions from the separeted single agents solutions:");
                            }
                            // Changing the goal states of the new instance, the new goal state will be the middle state from the source problem
                            for (int j = 0; j < numberOfAgents; j++)
                            {
                                int[] tempPosition = { 0, 0 };
                                tempPosition[0] = instance.m_vAgents[j].agent.Goal.x;
                                tempPosition[1] = instance.m_vAgents[j].agent.Goal.y;
                                realGoals.Add(tempPosition);
                                trueMiddlePosition.Add(middlePosition[j]);
                                instanceForFirstAstar.m_vAgents[j].agent.Goal.x = middlePosition[j][0];
                                instanceForFirstAstar.m_vAgents[j].agent.Goal.y = middlePosition[j][1];
                                // Printing the middle positions of the new first half problem that is going to be solved
                                Console.WriteLine("Middle position for agent {0}: {1},{2}", j, instanceForFirstAstar.m_vAgents[j].agent.Goal.x, instanceForFirstAstar.m_vAgents[j].agent.Goal.y);
                            }
                            Console.WriteLine("");
                            // Compute the ComputeSingleAgentShortestPaths for the new half instance (required before starting solving the instance)
                            instanceForFirstAstar.ComputeSingleAgentShortestPaths();
                            // Solving the new first half problem instance
                            runner.SolveGivenProblem(instanceForFirstAstar);

                            Console.WriteLine("-----------> Finished first half of A*star <-----------\n");

                            // Start building the second half of the MAPF problem
                            instanceForSecondReverseAstar = ProblemInstance.Import(Directory.GetCurrentDirectory() + "\\Instances\\" + instanceName);
                            instanceForSecondReverseAstar.instanceId = i + numberOfAgents + 2;


                            // Changing the new starting positions to be the middle positions that were calculated before, the goal state is remaining the same as the source problem
                            if (secondPartIsReverse)
                            {
                                for (int j = 0; j < numberOfAgents; j++)
                                {
                                    instanceForSecondReverseAstar.m_vAgents[j].agent.Goal.x = trueMiddlePosition[j][0];
                                    instanceForSecondReverseAstar.m_vAgents[j].agent.Goal.y = trueMiddlePosition[j][1];
                                    instanceForSecondReverseAstar.m_vAgents[j].lastMove.x = realGoals[j][0];
                                    instanceForSecondReverseAstar.m_vAgents[j].lastMove.y = realGoals[j][1];
                                }
                            }
                            else
                            {
                                for (int j = 0; j < numberOfAgents; j++)
                                {
                                    instanceForSecondReverseAstar.m_vAgents[j].lastMove.x = trueMiddlePosition[j][0];
                                    instanceForSecondReverseAstar.m_vAgents[j].lastMove.y = trueMiddlePosition[j][1];
                                }
                            }

                            // NumberOfRun 3 is the third run of the solver -> on the second half of the problem (from the end goal state backward to the middle positions state)
                            numberOfRun = 3;
                            // Compute the ComputeSingleAgentShortestPaths for the new half instance (required before starting solving the instance)
                            instanceForSecondReverseAstar.ComputeSingleAgentShortestPaths();
                            // Solving the new second half problem instance
                            runner.SolveGivenProblem(instanceForSecondReverseAstar);
                            
                            if (secondPartIsReverse)
                            {
                                Console.WriteLine("-----------> Finished the second reverse A*star <-----------\n");
                                Console.WriteLine("\n########################################################################\n************************************************************************\n");
                                Console.WriteLine("You solved the problem using Bi-Directional algorithm - with Forward and Reverse solvers");
                            }
                            else
                            {
                                Console.WriteLine("-----------> Finished the second forward A*star <-----------\n");
                                Console.WriteLine("\n########################################################################\n************************************************************************\n");
                                Console.WriteLine("You solved the problem using the splitted Forward-Forward algorithm - both of the parts were solved using forward search solver");
                            }

                            // Here all the three main solvers (the regular full Astar and the two halves) were finished
                            // Starting to print the final results

                            // Printing the middle positions that were used by the algorithm to solve the MAPF problem
                            Console.WriteLine("\n########################################################################\n************************************************************************\n");
                            Console.WriteLine("The middle position of the plan from full Astar on all the agents together:\n{0}", positionsOfMiddleStage);
                            if (sloveWithAutomatedSingleAgentMiddlePosition)
                            {
                                Console.WriteLine("Here are the new caculated middle positions from the separeted single agents solutions:");
                                for (int j = 0; j < numberOfAgents; j++)
                                {
                                    Console.Write("|({1},{2})", j, instanceForFirstAstar.m_vAgents[j].agent.Goal.x, instanceForFirstAstar.m_vAgents[j].agent.Goal.y);
                                }
                                Console.WriteLine("|");
                                Console.WriteLine("Great, you executed the automated middle positions algorithm!!");
                            }
                            else
                            {
                                Console.WriteLine("The solution was made using this middle positions from the true middle of the real full Astar solver");
                            }

                            // Writing the final results - with all the sum-up statistics
                            Console.WriteLine("\n#################################################");
                            Console.WriteLine("                Final Results!:\n" +
                            "#################################################");
                            Console.WriteLine("---------------------------------------------------\n" +
                            "Total cost full regular A*star = {0}\n" +
                            "Total cost Bi - Directional A* star = {1}\n" +
                            "Bi - Directional details:\n" +
                            "Total cost starting half A* star = {2}\n" +
                            "Total cost reverse half A* star = {3}", costFullAstar, costBiDirectionalAstar, costStartingAstar, costReverseAstar);
                            Console.WriteLine("---------------------------------------------------\n" +
                            "Total time full regular A*star = {0}\n" +
                            "Total time Bi - Directional A* star = {1}\n" +
                            "Bi - Directional details:\n" +
                            "Total time starting half A* star = {2}\n" +
                            "Total time reverse half A* star = {3}", timeFullAstar, timeBiDirectionalAstar, timeStartingAstar, timeReverseAstar);                          
                            /*Console.WriteLine("---------------------------------------------------\n" +
                            "Total expanded nodes full regular A* star = {0}\n" +
                            "Total expanded nodes Bi-Directional A* star = {1}\n" +
                            "Bi - Directional details:\n" +
                            "Total expanded nodes starting half A*star = {2}\n" +
                            "Total expanded nodes reverse half A*star = {3}", expandedNodesFullAstar, expandedNodesBiDirectionalAstar, expandedNodesStartingAstar, expandedNodesReverseAstar);
                            */Console.WriteLine("--------------------------------------------------\n" +
                            "Total generated nodes full regular A*star = {0}\n" +
                            "Total generated nodes Bi-Directional A* star = {1}\n" +
                            "Bi - Directional details:\n" +
                            "Total generated nodes starting half A*star = {2}\n" +
                            "Total generated nodes reverse half A*star = {3}\n" +
                            "--------------------------------------------------", generatedNodesFullAstar, generatedNodesBiDirectionalAstar, generatedNodesStartingAstar, generatedNodesReverseAstar);
                            Console.WriteLine("Total opened nodes full regular A*star = {0}\n" +
                            "Total opened nodes Bi-Directional A* star = {1}\n" +
                            "Bi - Directional details:\n" +
                            "Total opened nodes starting half A*star = {2}\n" +
                            "Total opened nodes reverse half A*star = {3}\n" +
                            "--------------------------------------------------", numberOpenNodesFullAstar, numberOpenNodesBiDirectionalAstar, numberOpenNodesStartingAstar, numberOpenNodesReverseAstar);
                            
                            // Save the latest problem
                            if (File.Exists(currentProblemFileName))
                                File.Delete(currentProblemFileName);
                            var lastProblemFile = new StreamWriter(currentProblemFileName);
                            lastProblemFile.Write("{0},{1},{2},{3}", gs, obs, ag, i);
                            for (int j = 0; j < runner.outOfTimeCounters.Length; j++)
                            {
                                lastProblemFile.Write("," + runner.outOfTimeCounters[j]);
                            }
                            lastProblemFile.Close();
                        }
                    }
                }
            }
            // end of building the grid map
            runner.CloseResultsFile();                    
        }

        protected static readonly string[] daoMapFilenames = { "dao_maps\\den502d.map" ,/* "dao_maps\\ost003d.map"/*, "dao_maps\\brc202d.map"*/};

        protected static readonly string[] mazeMapFilenames = { "mazes-width1-maps\\maze512-1-6.map", "mazes-width1-maps\\maze512-1-2.map",
                                                "mazes-width1-maps\\maze512-1-9.map" };

        /// <summary>
        /// Dragon Age experiment
        /// </summary>
        /// <param name="numInstances"></param>
        /// <param name="mapFileNames"></param>
        public void RunDragonAgeExperimentSet(int numInstances, string[] mapFileNames)
        {
            ProblemInstance instance;
            string instanceName;
            Run runner = new Run();

            bool resultsFileExisted = File.Exists(RESULTS_FILE_NAME);
            runner.OpenResultsFile(RESULTS_FILE_NAME);
            if (resultsFileExisted == false)
                runner.PrintResultsFileHeader();
            // FIXME: Code dup with RunExperimentSet

            TextWriter output;
            int[] agentListSizes = { 8, /*15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100*/};
            //int[] agentListSizes = { 60, 65, 70, 75, 80, 85, 90, 95, 100 };
            //int[] agentListSizes = { 100 };

            bool continueFromLastRun = false;
            string[] lineParts = null;

            //string currentProblemFileName = Directory.GetCurrentDirectory() + "\\Instances\\current problem-" + Process.GetCurrentProcess().ProcessName;
            string currentProblemFileName = Directory.GetCurrentDirectory() + "\\Instances-WRONGpath\\current problem-" + Process.GetCurrentProcess().ProcessName;

            if (File.Exists(currentProblemFileName)) //if we're continuing running from last time
            {
                TextReader input = new StreamReader(currentProblemFileName);
                lineParts = input.ReadLine().Split(',');  //get the last problem
                input.Close();
                continueFromLastRun = true;
            }

            for (int ag = 0; ag < agentListSizes.Length; ag++)
            {
                for (int i = 0; i < numInstances; i++)
                {
                    //string name = Process.GetCurrentProcess().ProcessName.Substring(1);
                    //if (i % 33 != Convert.ToInt32(name)) // DAO!
                    //    continue;

                    for (int map = 0; map < mapFileNames.Length; map++)
                    {
                        if (continueFromLastRun) // Set the latest problem
                        {
                            ag = int.Parse(lineParts[0]);
                            i = int.Parse(lineParts[1]);
                            map = int.Parse(lineParts[2]);
                            for (int j = 3; j < lineParts.Length && j-3 < runner.outOfTimeCounters.Length; j++)
                            {
                                runner.outOfTimeCounters[j - 3] = int.Parse(lineParts[j]);
                            }
                            continueFromLastRun = false;
                            continue;
                        }
                        if (runner.outOfTimeCounters.Sum() == runner.outOfTimeCounters.Length * 20) // All algs should be skipped
                            break;
                        string mapFileName = mapFileNames[map];
                        instanceName = Path.GetFileNameWithoutExtension(mapFileName) + "-" + agentListSizes[ag] + "-" + i;
                        try
                        {
                            instance = ProblemInstance.Import(Directory.GetCurrentDirectory() + "\\Instances\\" + instanceName);
                        }
                        catch (Exception importException)
                        {
                            if (onlyReadInstances)
                            {
                                Console.WriteLine("File " + instanceName + "  dosen't exist");
                                return;
                            }

                            instance = runner.GenerateDragonAgeProblemInstance(mapFileName, agentListSizes[ag]);
                            instance.ComputeSingleAgentShortestPaths(); // Consider just importing the generated problem after exporting it to remove the duplication of this line from Import()
                            instance.instanceId = i;
                            instance.Export(instanceName);
                        }

                        runner.SolveGivenProblem(instance);

                        //save the latest problem
                        File.Delete(currentProblemFileName);
                        output = new StreamWriter(currentProblemFileName);
                        output.Write("{0},{1},{2}", ag, i, map);
                        for (int j = 0; j < runner.outOfTimeCounters.Length; j++)
                        {
                            output.Write("," + runner.outOfTimeCounters[j]);
                        }
                        output.Close();
                    }
                }
            }
            runner.CloseResultsFile();
        }

        /// <summary>
        /// This is the starting point of the program. 
        /// </summary>
        static void Main(string[] args)
        {

            //FileStream fs = new FileStream("output.txt", FileMode.Create);
            //// Save the standard output.
            //TextWriter tmp = Console.Out;
            //StreamWriter sw = new StreamWriter(fs);
            //Console.SetOut(sw);

            Program me = new Program();
            Program.RESULTS_FILE_NAME = Process.GetCurrentProcess().ProcessName + ".csv";
            TextWriterTraceListener tr1 = new TextWriterTraceListener(System.Console.Out);
            Debug.Listeners.Add(tr1);
            if (System.Diagnostics.Debugger.IsAttached)
                Constants.MAX_TIME = int.MaxValue;

            if (Directory.Exists(Directory.GetCurrentDirectory() + "\\Instances") == false)
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Instances");
            }

            Program.onlyReadInstances = false;

            int instances = 1;

            bool runGrids = true;
            bool runDragonAge = false;
            bool runMazesWidth1 = false;
            bool runSpecific = false;

            if (runGrids == true)
            {
                // ******** Some of the project modifications: ********

                // All the problem instances that were mentioned in the project report are attached to the submission in the "problem instances" folder - alongside the report document
                // "gridSize" number "252" are the "Dragon map" and the "KIVA map" - as described in the project report
                int[] gridSizes = new int[] {50/*10,20,30,40*/};

                // Choose how many agents you want to run in tha MAPF problem - the specific test were described in the project report
                int[] agentListSizes = new int[] { /*2, 3,*/ 10 /*5 , 6 /*, 7, 8, 9, 10, 11, 12, 13, 14, 15*//*, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 */};

                // In my project for running the tests as described in the report -> choose "obstaclesPercents" = 66 to run the "Dragon map", choose "obstaclesPercents" = 77 to run the "KIVA map"
                int[] obstaclesPercents = new int[] {20 /*77 ,10 ,15, 20, 25, 30*/};

                // Run the MAPF problem
                me.RunExperimentSet(gridSizes, agentListSizes, obstaclesPercents, instances);
            }
            else if (runDragonAge == true)
                me.RunDragonAgeExperimentSet(instances, Program.daoMapFilenames); // Obstacle percents and grid sizes built-in to the maps.
            else if (runMazesWidth1 == true)
                me.RunDragonAgeExperimentSet(instances, Program.mazeMapFilenames); // Obstacle percents and grid sizes built-in to the maps.
            else if (runSpecific == true)
            {
                me.RunInstance("Instance-5-15-6-13");
            }

            // A function to be used by Eric's PDB code
            //me.runForPdb();
            Console.WriteLine("*********************THE END**************************");
            Console.ReadLine();
        }    
    }
}
