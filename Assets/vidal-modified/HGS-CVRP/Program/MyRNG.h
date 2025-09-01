#ifndef MYRNG_H
#define MYRNG_H

#include <cstdint>
#include <vector>
#include <algorithm>

class MyRNG
{
public:
    MyRNG(uint32_t input);
    void setSeed(uint32_t input);
    uint32_t lcg();
    int range(int min, int max);

    template <typename T>
    void shuffle(std::vector<T>& list)
    {
        int n = list.size();
        while (n > 1)
        {
            n--;
            uint32_t rnd = lcg();
            int k = rnd % (n + 1);
            std::swap(list[k], list[n]);
        }
    }

private:
    uint32_t seed;
};

#endif
