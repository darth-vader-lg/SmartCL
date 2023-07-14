#include "pch.h"
#include "SmartCL.h"

kernel void main(global int* data)
{
   printf("data[0] = %i\r\n", data[0]);
}
