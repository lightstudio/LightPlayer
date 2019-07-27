#pragma once

#define NOMINMAX 
#include <collection.h>
#include <ppltasks.h>

// Direct2D, WRL, WinCodec, etc.
#include <wrl.h>
#include <d3d11_2.h>
#include <d2d1_2.h>
#include <d2d1effects_1.h>
#include <dwrite_2.h>
#include <wincodec.h>
#include <agile.h>
#include <shcore.h>

#include "windows.ui.xaml.media.dxinterop.h"

#include <stdio.h>
#include <stdlib.h>
#include <algorithm>
#include <iostream>
#include <Windows.h>
#if !_DEBUG
#define DebugMessage(x)
#else
#define DebugMessage(x) OutputDebugString(x)
#endif

//#define ENABLE_VIDEO
#define FS_BUFFER_SIZE 0x200000
#define FS_BUFFER_SIZE_SCAN 2048
