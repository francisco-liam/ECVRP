using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class MyRNG
{
    uint seed;

    public uint LCG()
    {
        seed = (1103515245u * seed + 12345u) & 0x7FFFFFFF;
        return seed;
    }


    public int Range(int min, int max)
    {
        uint rnd = LCG();
        return min + (int)(rnd % (uint)(max - min + 1));
    }

    public void Shuffle<T>(IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            uint rnd = LCG();
            int k = (int)(rnd % (uint)(n + 1));  // simple modulo
            // Swap
            T temp = list[k];
            list[k] = list[n];
            list[n] = temp;
        }
    }

    public MyRNG(uint input)
    {
        seed = input;
    }
}
