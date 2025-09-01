#include "MyRNG.h"

uint32_t MyRNG::lcg() {
    uint32_t result = (1103515245 * seed + 12345) % 2147483648;
    seed = result;
    return result;
}

int MyRNG::range(int min, int max) {
    uint32_t rnd = lcg();
    return min + (rnd % (max - min + 1));
}
MyRNG::MyRNG(uint32_t input) {
    seed = input;
}