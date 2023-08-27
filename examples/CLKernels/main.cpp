#include "pch.h"
#include "SmartCL.h"

kernel void clmain(global int* data)
{
   printf("data[0] = %i\r\n", data[0]);
}
