#pragma once

namespace Light {
	public ref class AudioIndexCue sealed {
	public:
		property Light::IMediaInfo^ TrackInfo;
		property Windows::Foundation::TimeSpan Duration;
		property Windows::Foundation::TimeSpan StartTime;
	};
}