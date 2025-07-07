using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicsChecking : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        List<int> list = new List<int>();
        for (int i = 0; i < 10; i++)
        {
            list.Add(i);
        }

        Debug.Log(PrintList(list));

        list.RemoveAt(0);

        Debug.Log(PrintList(list));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static string PrintList<T>(IEnumerable<T> list)
    {
        return string.Join(" ", list);
    }
}
