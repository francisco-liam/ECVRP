using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.U2D.IK;
using UnityEngine.Windows.Speech;

public class LKHTriple
{
    public int u;
    public int i;
    public float g;

    public LKHTriple(int u, int i, float g)
    {
        this.u = u;
        this.i = i;
        this.g = g;
    }
}

public class LKHMgr : MonoBehaviour
{
    public List<int> path;
    public List<int> newPath;
    public static LKHMgr inst;

    private void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Init()
    {
        ApplyLKH(path, TSPMgr.inst.problem.adjacencyMatrix, 5, 2);
    }

    public List<int> ApplyLKH(List<int> tour, float[,] adjacencyMartix, int p1, int p2)
    {
        Stack<LKHTriple> lkhStack = new Stack<LKHTriple>();
        float gain = 0;
        List<int> alternatingTrail = new List<int>();
        List<Vector2> tourEdges= GetTourEdges(tour);
        List<Vector2> exchangeEdges = new List<Vector2>();

        do
        {
            gain = 0;
            exchangeEdges = new List<Vector2>();
            for (int i = 0; i < tour.Count; i++)
            {
                LKHTriple newTriple = new LKHTriple(tour[i], 0, 0);
                lkhStack.Push(newTriple);
            }
            while (lkhStack.Count > 0)
            {
                LKHTriple top = lkhStack.Pop();
                if (alternatingTrail.Count > top.i)
                {
                    alternatingTrail[top.i] = top.u;
                }
                else
                {
                    alternatingTrail.Add(top.u);
                }

                if (top.i % 2 == 0)
                {
                    foreach (int vert in tour)
                    {
                        List<Vector2> alternatingTrailEdges = GetPathEdges(alternatingTrail.GetRange(0, top.i + 1));
                        List<Vector2> diff = tourEdges.Except(alternatingTrailEdges).ToList();
                        Vector2 newEdge = new Vector2(alternatingTrail[top.i], vert);
                        if (diff.Contains(newEdge))
                        {
                            List<Vector2> trailPlusVert = new List<Vector2>(alternatingTrailEdges);
                            trailPlusVert.Add(new Vector2(alternatingTrail[top.i], vert));
                            List<Vector2> tourAndTrailUnion = tourEdges.Union(trailPlusVert).ToList();
                            List<Vector2> unionTour = new List<Vector2>(tourAndTrailUnion);
                            Vector2 vertToStart = new Vector2(vert, alternatingTrail[0]);
                            unionTour.Add(vertToStart);
                            List<Vector2> symmetricDiff = tourEdges.Except(unionTour).Union(unionTour.Except(tourEdges)).ToList();
                            if (top.i <= p2 || (!tourAndTrailUnion.Contains(vertToStart) && IsValidTour(symmetricDiff)))
                            {
                                lkhStack.Push(new LKHTriple(vert, top.i + 1, top.g + adjacencyMartix[top.i, vert]));
                            }
                        }
                    }
                }
                else
                {
                    List<Vector2> alternatingTrailTourEdges = GetTourEdges(alternatingTrail.GetRange(0, top.i + 1));
                    List<Vector2> symmetricDiff = tourEdges.Except(alternatingTrailTourEdges).Union(alternatingTrailTourEdges.Except(tourEdges)).ToList();
                    if (top.g > adjacencyMartix[top.i, 0] &&
                        top.g - adjacencyMartix[top.i, 0] > gain &&
                        IsValidTour(symmetricDiff))
                    {
                        exchangeEdges = alternatingTrailTourEdges;
                        gain = top.g - adjacencyMartix[top.i, 0];
                    }
                    foreach (int vert in tour)
                    {
                        List<Vector2> alternatingTrailPathEdges = GetPathEdges(alternatingTrail.GetRange(0, top.i + 1));
                        List<Vector2> union = tourEdges.Union(alternatingTrailPathEdges).ToList();
                        Vector2 iToVert = new Vector2(alternatingTrail[top.i], vert);
                        if (top.g > adjacencyMartix[alternatingTrail[top.i], vert] && !union.Contains(iToVert))
                        {
                            lkhStack.Push(new LKHTriple(vert, top.i + 1, top.g - adjacencyMartix[top.i, vert]));
                        }
                    }
                }

                LKHTriple peekTop = lkhStack.Peek();
                if (top.i <= peekTop.i)
                {
                    if (gain > 0)
                    {
                        tourEdges = tourEdges.Except(exchangeEdges).Union(exchangeEdges.Except(tourEdges)).ToList();
                        lkhStack.Clear();
                    }
                    else if (top.i > p1)
                    {
                        while (lkhStack.Peek().i > p1)
                            lkhStack.Pop();
                    }
                }
            }
        } while (gain != 0);

        return GetOrderedPath(tourEdges);
    }

    List<Vector2> GetTourEdges(List<int> tour)
    {
        List<Vector2> edges = GetPathEdges(tour);

        edges.Add(new Vector2(tour[tour.Count-1], tour[0]));

        return edges;
    }

    List<Vector2> GetPathEdges(List<int> path)
    {
        List<Vector2> edges = new List<Vector2>();

        for (int i = 0; i < path.Count-1; i++)
        {
            edges.Add(new Vector2(path[i], path[i+1]));
        }

        return edges;
    }

    public static bool IsValidTour(List<Vector2> edges)
    {
        if (edges == null || edges.Count == 0) return false;

        // Build adjacency list
        Dictionary<int, List<int>> adjacencyList = new Dictionary<int, List<int>>();

        foreach (var edge in edges)
        {
            int u = (int)edge.x;
            int v = (int)edge.y;

            if (!adjacencyList.ContainsKey(u)) adjacencyList[u] = new List<int>();
            if (!adjacencyList.ContainsKey(v)) adjacencyList[v] = new List<int>();

            adjacencyList[u].Add(v);
            adjacencyList[v].Add(u);
        }

        // Step 1: Check degree constraint (each node should have exactly 2 edges)
        foreach (var neighbors in adjacencyList.Values)
        {
            if (neighbors.Count != 2)
            {
                return false; // Invalid degree
            }
        }

        // Step 2: Check connectivity and cycle
        return IsSingleCycle(adjacencyList);
    }

    private static bool IsSingleCycle(Dictionary<int, List<int>> adjacencyList)
    {
        HashSet<int> visited = new HashSet<int>();
        int startNode = adjacencyList.Keys.First();
        int current = startNode;
        int prev = -1;

        // Perform DFS traversal
        for (int i = 0; i < adjacencyList.Count; i++)
        {
            visited.Add(current);
            var neighbors = adjacencyList[current];

            // Pick the next node that isn't the one we just came from
            int next = neighbors[0] == prev ? neighbors[1] : neighbors[0];

            prev = current;
            current = next;
        }

        // Step 3: Ensure all nodes are visited and the last one connects back to the start
        return visited.Count == adjacencyList.Count && current == startNode;
    }

    public static List<int> GetOrderedPath(List<Vector2> edges)
    {
        if (edges == null || edges.Count == 0)
            throw new ArgumentException("Edge list cannot be empty.");

        // Build adjacency list
        Dictionary<int, List<int>> adjacencyList = new Dictionary<int, List<int>>();
        HashSet<int> nodes = new HashSet<int>();

        foreach (var edge in edges)
        {
            int u = (int)edge.x;
            int v = (int)edge.y;

            if (!adjacencyList.ContainsKey(u)) adjacencyList[u] = new List<int>();
            if (!adjacencyList.ContainsKey(v)) adjacencyList[v] = new List<int>();

            adjacencyList[u].Add(v);
            adjacencyList[v].Add(u);

            nodes.Add(u);
            nodes.Add(v);
        }

        // Find the start node (must have only one neighbor in case of an open path)
        int startNode = nodes.First();
        foreach (var node in adjacencyList)
        {
            if (node.Value.Count == 1) // A node with only one connection is an endpoint
            {
                startNode = node.Key;
                break;
            }
        }

        return ReconstructPath(startNode, adjacencyList);
    }

    private static List<int> ReconstructPath(int startNode, Dictionary<int, List<int>> adjacencyList)
    {
        List<int> path = new List<int>();
        HashSet<int> visited = new HashSet<int>();

        int current = startNode;
        int prev = -1;

        // Traverse the path
        while (current != -1)
        {
            path.Add(current);
            visited.Add(current);

            int next = -1;
            foreach (var neighbor in adjacencyList[current])
            {
                if (!visited.Contains(neighbor))
                {
                    next = neighbor;
                    break;
                }
            }

            prev = current;
            current = next;
        }

        return path;
    }
}
