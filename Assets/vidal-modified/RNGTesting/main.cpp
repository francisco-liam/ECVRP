#include <iostream>
#include "MyRNG.h"

int main() {
    MyRNG rng(8008);  // Initialize with seed 42

    for (int i = 0; i < 5; ++i) {
        std::cout << rng.range(10, 20) << std::endl;
    }

    return 0;
}
