using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphMgr : MonoBehaviour
{
    public static GraphMgr inst;
    public GameObject nodePrefab;
    public Transform nodesParent;
    public Transform edgesParent;

    public List<GraphNode> nodes;
    public List<GameObject> edges;

    public bool debug;
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
        if (CVRPMain.inst.graph)
        {
            Vector3 mouseWorldPos = Input.mousePosition;
            mouseWorldPos.z = -Camera.main.transform.position.z;
            mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseWorldPos);
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

            if (debug && hit.collider != null && hit.collider.gameObject.CompareTag("Edge"))
            {
                Debug.Log($"Mouse is over a line! Name :{hit.collider.gameObject.name}");
            }
        }
    }

    public void CreateGraph()
    {
        nodes = new List<GraphNode>();

        Vector3 depotOffset = new Vector3((float) ParametersMgr.inst.cli[0].coordX,
            (float) ParametersMgr.inst.cli[0].coordY, 0);

        for (int i = 0; i < ParametersMgr.inst.cli.Count; i++)
        {
            Client node = ParametersMgr.inst.cli[i];
            Vector2 coordinate = new Vector2((float)node.coordX, (float)node.coordY);
            GameObject newNode = Instantiate(nodePrefab, coordinate, Quaternion.identity, nodesParent);
            newNode.transform.position -= depotOffset;
            newNode.name = $"Node {i+1}";
            
            GraphNode nodeAsset = newNode.AddComponent<GraphNode>();
            nodeAsset.nodeIndex = i;
            nodeAsset.coordinate = coordinate;
            nodeAsset.demand = node.demand;
            nodes.Add(nodeAsset);
        }
    }

    public void DrawRoute(List<int> route, Color color)
    {
        DrawEdge(nodes[0], nodes[route[0]], color);
        for(int i = 0; i < route.Count - 1; i++)
            DrawEdge(nodes[route[i]], nodes[route[i + 1]], color);
        DrawEdge(nodes[route[route.Count - 1]], nodes[0], color);
    }

    public void DrawEdge(GraphNode nodeA, GraphNode nodeB, Color color)
    {
        GameObject newEdge = new GameObject();
        newEdge.transform.SetParent(edgesParent);
        newEdge.name = $"({nodeA.name}, {nodeB.name})";
        newEdge.tag = "Edge";
        edges.Add(newEdge);

        Vector2 startPoint = nodeA.transform.position;
        Vector2 endPoint = nodeB.transform.position;

        LineRenderer lr = newEdge.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.SetPosition(0, startPoint);
        lr.SetPosition(1, endPoint);

        lr.startWidth = 0.8f;
        lr.endWidth = 0.8f;

        lr.sortingOrder = -10;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;

        // Set Collider Position and Size
        Vector2 center = (startPoint + endPoint) / 2f;
        Vector2 direction = endPoint - startPoint;
        float length = direction.magnitude;

        BoxCollider2D col = newEdge.AddComponent<BoxCollider2D>();
        col.size = new Vector2(length, 0.8f); // Width = 0.1f (adjustable)
        col.offset = Vector2.zero;

        // Set GameObject position at center
        newEdge.transform.position = center;

        // Rotate the GameObject so the collider aligns with the line
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        newEdge.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void RemoveAllEdges()
    {
        foreach (GameObject edge in edges)
            Destroy(edge);

        edges.Clear();
    }
}
