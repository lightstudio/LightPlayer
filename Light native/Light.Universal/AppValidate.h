/*
*   AppValidate.h
* 
*   Date: 2nd Aug, 2014   Author: David Huang
*   (C) 2014 Light Studio. All Rights Reserved.
*/

#pragma once

#ifdef ENABLE_LEGACY
#define PUBLISHER_DISPLAY_NAME L"Light Studio"
#define WP_PUBLISHER_NAME L"CN=2C3FCC71-19E4-4A4C-9002-3AC6F6215707"

#ifdef LIGHT_INTERNAL_TEST
#define PUBLISHER_DISPLAY_NAME L"imbushuo Dev"
#define WP_PUBLISHER_NAME L"CN=951B7044-8393-494E-946C-13F75A4CC013"
#endif

#if _DEBUG
#include <debugapi.h>

#if WINAPI_FAMILY!=WINAPI_FAMILY_PHONE_APP	
	#define ValidateLicense \
		if (Windows::ApplicationModel::Package::Current->IsDevelopmentMode)\
			OutputDebugString(L"This component cannot be used under dev mode\n");\
		if (!Windows::ApplicationModel::Store::CurrentApp::LicenseInformation->IsActive)\
			OutputDebugString(L"This component need an active license.\n");\
		if (Windows::ApplicationModel::Package::Current->PublisherDisplayName != PUBLISHER_DISPLAY_NAME)\
			OutputDebugString(L"This component can only be used by Light Studio\n");
	#else
	#define ValidateLicense \
		if (!Windows::ApplicationModel::Store::CurrentApp::LicenseInformation->IsActive)\
			OutputDebugString(L"This component need an active license.");\
		if (Windows::ApplicationModel::Package::Current->Id->Publisher != WP_PUBLISHER_NAME)\
			OutputDebugString(L"This component can only be used by Light Studio");
	#endif

#elif NDEBUG
	#if WINAPI_FAMILY!=WINAPI_FAMILY_PHONE_APP
	#define ValidateLicense \
		if (Windows::ApplicationModel::Package::Current->IsDevelopmentMode)\
			throw Platform::Exception::CreateException(0,L"This component cannot be used under dev mode");\
		if (!Windows::ApplicationModel::Store::CurrentApp::LicenseInformation->IsActive)\
			throw Platform::Exception::CreateException(0,L"This component need an active license.");\
		if (Windows::ApplicationModel::Package::Current->PublisherDisplayName != PUBLISHER_DISPLAY_NAME)\
			throw Platform::Exception::CreateException(0,L"This component can only be used by Light Studio");
	#else
	#define ValidateLicense \
		if (!Windows::ApplicationModel::Store::CurrentApp::LicenseInformation->IsActive)\
			throw Platform::Exception::CreateException(0,L"This component need an active license.");\
		if (Windows::ApplicationModel::Package::Current->Id->Publisher != WP_PUBLISHER_NAME)\
			throw Platform::Exception::CreateException(0,L"This component can only be used by Light Studio");
	#endif
#endif
#endif