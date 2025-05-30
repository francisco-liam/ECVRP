using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class DataMgr : MonoBehaviour
{
    public static DataMgr inst;
    public UnityEngine.Object file;

    // Start is called before the first frame update
    void Awake()
    {
        inst = this;
    }

    public ECVRPProblem ReadEVRPFile()
    {
        int stage = 0; //0 = header; 1 = coords; 2 = demand; 3 = stations; 4 = depot
        List<ECVRPNode> nodes = new List<ECVRPNode>();

        StreamReader sr = new StreamReader(AssetDatabase.GetAssetPath(file));
        string line = sr.ReadLine();
        string[] coordString;
        ECVRPProblem problem = new ECVRPProblem();

        while (line != null)
        {
            if(line == "NODE_COORD_SECTION " || line == "DEMAND_SECTION " || line == "STATIONS_COORD_SECTION " || line == "DEPOT_SECTION" || line == "EOF")
            {
                stage++;
                line = sr.ReadLine();
                continue;
            }

            line = line.Trim();
            coordString = line.Split();

            if (stage == 0)
            {
                if (coordString[0] == "VEHICLES:")
                    problem.vehlicles = int.Parse(coordString[1]);
                if (coordString[0] == "CAPACITY:")
                    problem.capacity = float.Parse(coordString[1]);
                if (coordString[0] == "ENERGY_CAPACITY:")
                    problem.energyCapacity = float.Parse(coordString[1]);
                if (coordString[0] == "ENERGY_CONSUMPTION:")
                    problem.energyConsumption = float.Parse(coordString[1]);
            }

            if (stage == 1)
            {
                ECVRPNode node = new ECVRPNode();
                node.nodeNumber = int.Parse(coordString[0]);
                node.coordinate = new Vector2(float.Parse(coordString[1]), float.Parse(coordString[2]));
                nodes.Add(node);
            }

            if(stage > 1)
            {
                ECVRPNode result = nodes.Find(s => s.nodeNumber == int.Parse(coordString[0]));
                if(result != null)
                {
                    if(stage == 2)
                        result.demand = int.Parse(coordString[1]);
                    if (stage == 3)
                        result.isCharging = true;
                    if (stage == 4)
                        result.isDepot = true;
                }       
            }

            line = sr.ReadLine();
        }

        problem.nodes = nodes;
        problem.CreateAdjacencyMatrix();
        return problem;
    }

    /*
    public TSPProblem ReadTSPFile()
    {
        List<Vector2> nodes = new List<Vector2>();
        bool readingCoordinates = false;

        StreamReader sr = new StreamReader(AssetDatabase.GetAssetPath(file));
        string line = sr.ReadLine();
        string[] coordString;

        while (line != null)
        {
            if (line == "EOF")
                readingCoordinates = false;

            if (readingCoordinates)
            {
                line = line.Trim();
                coordString = line.Split();
                Vector2 node = new Vector2(float.Parse(coordString[1]), float.Parse(coordString[2]));
                nodes.Add(node);
            }

            if (line == "NODE_COORD_SECTION")
                readingCoordinates = true;

            line = sr.ReadLine();
        }

        TSPProblem problem = new TSPProblem();
        problem.nodes = nodes;
        return problem;
    }
    */

    public CVRPProblem ReadCVRPFile()
    {
        int stage = 0; //0 = header; 1 = coords; 2 = demand; 3 = depot
        List<CVRPNode> nodes = new List<CVRPNode>();

        StreamReader sr = new StreamReader(AssetDatabase.GetAssetPath(file));
        string line = sr.ReadLine();
        string[] coordString;
        CVRPProblem problem = new CVRPProblem();
        bool depotFound = false;
        CVRPNode depot = new CVRPNode();

        while (line != null)
        {
            line = line.Trim();
            coordString = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (coordString[0] == "NODE_COORD_SECTION" || coordString[0] == "DEMAND_SECTION" || coordString[0] == "DEPOT_SECTION" || coordString[0] == "EOF")
            {
                stage++;
                line = sr.ReadLine();
                continue;
            }

            if (stage == 0)
            {
                if (coordString[0] == "NAME")
                {
                    string name = coordString[2];
                    Match match = Regex.Match(line, @"k(\d{1,2})");
                    int number = 0;

                    if (match.Success)
                    {
                        number = int.Parse(match.Groups[1].Value);
                    }

                    problem.vehicles = number;
                }
                if (coordString[0] == "CAPACITY")
                    problem.capacity = float.Parse(coordString[2]);
            }

            if (stage == 1)
            {
                CVRPNode node = new CVRPNode();

                node.nodeNumber = int.Parse(coordString[0]);
                node.coordinate = new Vector2(float.Parse(coordString[1]), float.Parse(coordString[2]));

                if (!depotFound)
                {
                    depot = node;
                    depotFound = true;
                }

                node.polarAngle = CircleSector.positive_mod((int) (32768f *
                    Mathf.Atan2(node.coordinate.y - depot.coordinate.y, node.coordinate.x - depot.coordinate.x) / Mathf.PI));
                nodes.Add(node);
            }

            if (stage > 1)
            {
                CVRPNode result = nodes.Find(s => s.nodeNumber == int.Parse(coordString[0]));
                if (result != null)
                {
                    if (stage == 2)
                        result.demand = int.Parse(coordString[1]);
                    if (stage == 3)
                        result.isDepot = true;
                }
            }

            line = sr.ReadLine();
        }

        problem.nodes = nodes;
        problem.customerNodes = new List<CVRPNode>(nodes);
        problem.customerNodes.RemoveAt(0);
        int customers = 0;
        foreach (CVRPNode node in nodes)
        {
            if(!node.isDepot)
                customers++;
        }
        problem.customers = customers;
        problem.CreateAdjacencyMatrix();
        return problem;
    }

}
