#pragma once

#include <wrl.h>
#include <wrl/implements.h>
#include <wrl/wrappers/corewrappers.h>

#include <windows.foundation.h>
#include <windows.foundation.collections.h>

#include "StringUtils.h"
extern "C"
{
#include "libavutil\dict.h"
}

class AVDictionaryEntryPair :
	public Microsoft::WRL::RuntimeClass<Microsoft::WRL::RuntimeClassFlags<Microsoft::WRL::RuntimeClassType::WinRt>,
	__FIKeyValuePair_2_HSTRING_HSTRING>
{
	InspectableClass(
		__FIKeyValuePair_2_HSTRING_HSTRING::z_get_rc_name_impl(),
		BaseTrust)

public:
	AVDictionaryEntryPair(AVDictionaryEntry *pair) : m_pair(pair) { }

	STDMETHOD(get_Key)(HSTRING *key) override
	{
		*key = utf8ToHString(m_pair->key);
		return S_OK;
	}

	STDMETHOD(get_Value)(HSTRING *value) override
	{
		*value = utf8ToHString(m_pair->value);
		return S_OK;
	}

private:
	AVDictionaryEntry *m_pair;
};

class AVDictionaryIterator :
	public Microsoft::WRL::RuntimeClass<Microsoft::WRL::RuntimeClassFlags<Microsoft::WRL::RuntimeClassType::WinRt>,
	__FIIterator_1___FIKeyValuePair_2_HSTRING_HSTRING>
{
	InspectableClass(
		__FIIterator_1___FIKeyValuePair_2_HSTRING_HSTRING::z_get_rc_name_impl(),
		BaseTrust)

public:
	AVDictionaryIterator(AVDictionary *dict) : m_dict(dict) { boolean x; MoveNext(&x); }

	STDMETHOD(get_Current)(__FIKeyValuePair_2_HSTRING_HSTRING **current) override
	{
		return Microsoft::WRL::Details::Make<AVDictionaryEntryPair>(m_current).CopyTo(current);
	}
	
	STDMETHOD(get_HasCurrent)(boolean *hasCurrent) override
	{
		*hasCurrent = !!m_current;
		return S_OK;
	}

	STDMETHOD(MoveNext)(boolean *hasCurrent) override
	{
		m_current = av_dict_get(m_dict, "", m_current, AV_DICT_IGNORE_SUFFIX);
		*hasCurrent = !!m_current;
		return S_OK;
	}

private:
	AVDictionary *m_dict;
	AVDictionaryEntry *m_current = nullptr;
};

class AVDictionaryView :
	public Microsoft::WRL::RuntimeClass<Microsoft::WRL::RuntimeClassFlags<Microsoft::WRL::RuntimeClassType::WinRt>,
	__FIMapView_2_HSTRING_HSTRING,
	__FIIterable_1___FIKeyValuePair_2_HSTRING_HSTRING>
{
	InspectableClass(
		__FIMapView_2_HSTRING_HSTRING::z_get_rc_name_impl(),
		BaseTrust)

public:
	virtual ~AVDictionaryView()
	{
		av_dict_free(&m_dict);
	}

	AVDictionaryView(AVDictionary *dict) 
	{
		av_dict_copy(&m_dict, dict, 0);
	}

	virtual STDMETHODIMP Lookup(HSTRING key, HSTRING* item) override
	{
		auto utf8Key = hStringToUtf8(key);
		auto entry = av_dict_get(m_dict, utf8Key.get(), NULL, AV_DICT_IGNORE_SUFFIX);
		if (!entry) *item = Microsoft::WRL::Wrappers::HStringReference(L"").Get();
		else *item = utf8ToHString(entry->value);
		return S_OK;
	}
	
	virtual STDMETHODIMP get_Size(UINT32 *value) override { *value = av_dict_count(m_dict); return S_OK; }

	virtual STDMETHODIMP HasKey(HSTRING key, boolean *hasKey) override
	{
		auto utf8Key = hStringToUtf8(key);
		auto entry = av_dict_get(m_dict, utf8Key.get(), NULL, AV_DICT_IGNORE_SUFFIX);
		*hasKey = !!entry;
		return S_OK;
	}

	virtual STDMETHODIMP Split(
		__FIMapView_2_HSTRING_HSTRING **first,
		__FIMapView_2_HSTRING_HSTRING **second) override
	{
		first = second = nullptr; return S_OK;
	}

	virtual STDMETHODIMP First(__FIIterator_1___FIKeyValuePair_2_HSTRING_HSTRING **first) override
	{
		return Microsoft::WRL::Details::Make<AVDictionaryIterator>(m_dict).CopyTo(first);
	}

private:
	AVDictionary *m_dict = nullptr;
};
