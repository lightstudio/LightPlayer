/*
*   ACPHelper.h
*
*   Date: 14th July, 2014   Author: David Huang
*   (C) 2014 Light Studio. All Rights Reserved.
*/
#pragma once
#ifdef ENABLE_LEGACY

namespace Light
{
	public ref class ACPHelper sealed
	{
	public:
		/*
		* Use in try catch block.
		* Check or initialize the environment.
		*/
		static void CheckOrInitEnv();
		/*
		* Get ANSI Code Page from windows kernel.
		* must initialize first.
		*/
		static int GetACP();
	private:
		ACPHelper(){}
	};

	static public ref class StringConverter sealed
	{
	public:
		static Platform::String^ GetUnicodeString(UINT CodePage, const Platform::Array<byte, 1>^ input);
	};
}
#endif