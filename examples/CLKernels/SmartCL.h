#pragma once

#ifdef _WINDOWS

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>

#define kernel extern "C" __declspec(dllexport)
#define global
#define local

#pragma warning (disable : 4326) // For void main

#endif // _WINDOWS
