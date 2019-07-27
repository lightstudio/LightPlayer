#include "NativeSettingsManager.h"
#include <mutex>

using namespace Light;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::Storage;

std::mutex instance_mutex;
NativeSettingsManager^ _instance;

NativeSettingsManager::NativeSettingsManager() {
	auto container = ApplicationData::Current->LocalSettings->CreateContainer(
		"SettingsManager", 
		ApplicationDataCreateDisposition::Always);
	_underlyingSet = container->Values;
}

NativeSettingsManager^ NativeSettingsManager::Instance::get() {
	if (_instance == nullptr) {
		std::lock_guard<std::mutex> lock(instance_mutex);
		if (_instance == nullptr) {
			_instance = ref new NativeSettingsManager();
		}
	}
	return _instance;
}

bool NativeSettingsManager::AlwaysResample::get() {
	if (_underlyingSet->HasKey(L"AlwaysResample")) {
		return (bool)_underlyingSet->Lookup(L"AlwaysResample");
	}
	else {
		return false;
	}
}

int NativeSettingsManager::PreferredSampleRate::get() {
	if (_underlyingSet->HasKey(L"PreferredSampleRate")) {
		return (int)_underlyingSet->Lookup(L"PreferredSampleRate");
	}
	else {
		return 0;
	}
}