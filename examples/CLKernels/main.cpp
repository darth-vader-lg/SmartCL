#include "pch.h"

kernel void _main(global int* data)
{
   printf("data[0] = %i", data[0]);
}
