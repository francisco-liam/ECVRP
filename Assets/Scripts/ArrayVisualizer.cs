using UnityEngine;

public class ArrayVisualizer : MonoBehaviour
{
    // Your 2D array (not serialized by Unity directly)
    public int[,] myArray = new int[4, 4]
    {
        { 1, 2, 3, 4 },
        { 5, 6, 7, 8 },
        { 9, 10, 11, 12 },
        { 13, 14, 15, 16 }
    };

    public void SetArray(int[,] array)
    {
        myArray = array;
    }
}
