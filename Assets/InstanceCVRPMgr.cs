using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class InstanceCVRPMgr : MonoBehaviour
{
    public static InstanceCVRPMgr inst;
    public Object file;

    public int nbClients = 0;
    public double vehicleCapacity = double.MaxValue;
    public double durationLimit = 0.0;
    public bool isDurationConstraint = false;
    public bool isRoundingInteger = true; // Set to true if distances should be rounded

    public List<double> x_coords;
    public List<double> y_coords;
    public List<double> demands;
    public List<double> service_time;
    public double[][] dist_mtx;

    void Awake()
    {
        inst = this;
    }

    private void Start()
    {
        ReadInstance(AssetDatabase.GetAssetPath(file));
    }

    public void ReadInstance(string pathToInstance)
    {
        string content, content2, content3;
        double serviceTimeData = 0.0;

        using (StreamReader inputFile = new StreamReader(pathToInstance))
        {
            inputFile.ReadLine();
            inputFile.ReadLine();
            inputFile.ReadLine();

            string token;
            while ((token = ReadNextToken(inputFile)) != "NODE_COORD_SECTION")
            {
                switch (token)
                {
                    case "DIMENSION":
                        content2 = ReadNextToken(inputFile);
                        nbClients = int.Parse(ReadNextToken(inputFile)) - 1;
                        break;
                    case "EDGE_WEIGHT_TYPE":
                        content2 = ReadNextToken(inputFile);
                        content3 = ReadNextToken(inputFile);
                        break;
                    case "CAPACITY":
                        content2 = ReadNextToken(inputFile);
                        vehicleCapacity = double.Parse(ReadNextToken(inputFile), CultureInfo.InvariantCulture);
                        break;
                    case "DISTANCE":
                        content2 = ReadNextToken(inputFile);
                        durationLimit = double.Parse(ReadNextToken(inputFile), CultureInfo.InvariantCulture);
                        isDurationConstraint = true;
                        break;
                    case "SERVICE_TIME":
                        content2 = ReadNextToken(inputFile);
                        serviceTimeData = double.Parse(ReadNextToken(inputFile), CultureInfo.InvariantCulture);
                        break;
                    default:
                        throw new Exception("Unexpected data in input file: " + token);
                }
            }

            if (nbClients <= 0) throw new Exception("Number of nodes is undefined");
            if (vehicleCapacity == double.MaxValue) throw new Exception("Vehicle capacity is undefined");

            x_coords = new List<double>(new double[nbClients + 1]);
            y_coords = new List<double>(new double[nbClients + 1]);
            demands = new List<double>(new double[nbClients + 1]);
            service_time = new List<double>(new double[nbClients + 1]);

            // Reading coordinates
            for (int i = 0; i <= nbClients; i++)
            {
                int node_number = int.Parse(ReadNextToken(inputFile));
                if (node_number != i + 1)
                    throw new Exception("The node numbering is not in order.");

                x_coords[i] = double.Parse(ReadNextToken(inputFile), CultureInfo.InvariantCulture);
                y_coords[i] = double.Parse(ReadNextToken(inputFile), CultureInfo.InvariantCulture);
            }


            // Reading demand section
            content = ReadNextToken(inputFile);
            if (content != "DEMAND_SECTION") throw new Exception("Unexpected data in input file: " + content);

            for (int i = 0; i <= nbClients; i++)
            {
                int idx = int.Parse(ReadNextToken(inputFile));
                demands[i] = double.Parse(ReadNextToken(inputFile), CultureInfo.InvariantCulture);
                service_time[i] = (i == 0) ? 0.0 : serviceTimeData;
            }


            // Compute distance matrix
            dist_mtx = new double[nbClients + 1][];
            for (int i = 0; i <= nbClients; i++)
            {
                dist_mtx[i] = new double[nbClients + 1];
                for (int j = 0; j <= nbClients; j++)
                {
                    double dx = x_coords[i] - x_coords[j];
                    double dy = y_coords[i] - y_coords[j];
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    dist_mtx[i][j] = isRoundingInteger ? Math.Round(dist) : dist;
                }
            }

            // Read depot section
            content = ReadNextToken(inputFile);
            content2 = ReadNextToken(inputFile);
            content3 = ReadNextToken(inputFile);
            content3 = ReadNextToken(inputFile); // Same variable reused in C++, but keep distinct in C#

            if (content != "DEPOT_SECTION")
                throw new Exception("Unexpected data in input file: " + content);
            if (content2 != "1")
                throw new Exception("Expected depot index 1 instead of " + content2);
            if (content3 != "EOF")
                throw new Exception("Unexpected data in input file: " + content3);

        }
    }

    private string ReadNextToken(StreamReader reader)
    {
        while (!reader.EndOfStream)
        {
            int ch;
            string token = "";
            // Skip whitespace
            do
            {
                ch = reader.Read();
                if (ch == -1) return null;
            } while (char.IsWhiteSpace((char)ch));

            // Read token
            do
            {
                token += (char)ch;
                ch = reader.Peek();
                if (ch == -1 || char.IsWhiteSpace((char)ch)) break;
                ch = reader.Read();
            } while (true);

            return token;
        }
        return null;
    }
}
