#include "MyRNG.h"

uint32_t MyRNG::lcg() {
    seed = (1103515245u * seed + 12345u) & 0x7FFFFFFFu;
    return seed;
}


int MyRNG::range(int min, int max) {
    uint32_t rnd = lcg();
    return min + (rnd % (max - min + 1));
}
MyRNG::MyRNG(uint32_t input) {
    seed = input;
}

void MyRNG::setSeed(uint32_t input){
    seed = input;
}