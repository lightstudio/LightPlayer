#pragma once

namespace Light {
	ref class NativeSettingsManager sealed {
	public:
		static property NativeSettingsManager^ Instance { NativeSettingsManager^ get(); }
		property bool AlwaysResample { bool get(); }
		property int PreferredSampleRate { int get(); }
	private:
		NativeSettingsManager();
		Windows::Foundation::Collections::IPropertySet^ _underlyingSet;
	};
}