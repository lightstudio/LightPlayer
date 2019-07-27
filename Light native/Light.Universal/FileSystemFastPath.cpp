#include <wrl.h>
#include <wrl\client.h>
#include <wrl\wrappers\corewrappers.h>
#include <wrl\async.h>
#include <windows.storage.h>

#include <AsyncOperation.h>

using namespace Microsoft::WRL;
using namespace Microsoft::WRL::Details;
using namespace Microsoft::WRL::Wrappers;
using namespace ABI::Windows::Storage;
using namespace ABI::Windows::Foundation;

thread_local ComPtr<IStorageFileStatics> s_storFileFactory;
thread_local ComPtr<IStorageFolderStatics> s_storFolderFactory;

extern "C" HRESULT __declspec(dllexport) WINAPI GetStorageFileFromPath(wchar_t *path, IStorageFile **file);
extern "C" HRESULT __declspec(dllexport) WINAPI GetStorageFileFromPathAsync(wchar_t *path, IAsyncOperation<IStorageFile*>** op);
extern "C" HRESULT __declspec(dllexport) WINAPI GetStorageFolderFromPath(wchar_t *path, IStorageFolder **file);
extern "C" HRESULT __declspec(dllexport) WINAPI GetStorageFolderFromPathAsync(wchar_t *path, IAsyncOperation<IStorageFolder*>** op);
extern "C" BOOL __declspec(dllexport) WINAPI ShouldUseWinRT(wchar_t *path);

HRESULT WINAPI GetStorageFileFromPath(wchar_t *path, IStorageFile **file)
{
	HRESULT hr = S_OK;
	*file = nullptr;
	if (!s_storFileFactory)
	{
		hr = ABI::Windows::Foundation::GetActivationFactory(
			HStringReference(RuntimeClass_Windows_Storage_StorageFile).Get(),
			&s_storFileFactory);
	}

	if (SUCCEEDED(hr))
	{
		ComPtr<ABI::Windows::Foundation::IAsyncOperation<ABI::Windows::Storage::StorageFile*>> asyncOp;
		hr = s_storFileFactory->GetFileFromPathAsync(
			HStringReference(path).Get(),
			&asyncOp);

		if (SUCCEEDED(hr))
		{
			ComPtr<IAsyncInfo> asyncInfo;
			hr = asyncOp.As(&asyncInfo);

			if (SUCCEEDED(hr))
			{
				AsyncStatus status;

				while (SUCCEEDED(hr = asyncInfo->get_Status(&status)) && status == AsyncStatus::Started)
					SleepEx(0, TRUE);

				if (FAILED(hr) || status != AsyncStatus::Completed)
				{
					asyncInfo->get_ErrorCode(&hr);
				}
				else
				{
					asyncOp->GetResults(file);
				}
			}
		}
	}

	return hr;
}

HRESULT WINAPI GetStorageFileFromPathAsync(wchar_t *path, IAsyncOperation<IStorageFile*>** op)
{
	HRESULT hr = S_OK;
	if (!s_storFileFactory)
	{
		hr = ABI::Windows::Foundation::GetActivationFactory(
			HStringReference(RuntimeClass_Windows_Storage_StorageFile).Get(),
			&s_storFileFactory);
	}

	if (SUCCEEDED(hr))
	{
		ComPtr<IAsyncOperation<StorageFile*>> getfileOp;
		hr = s_storFileFactory->GetFileFromPathAsync(
			HStringReference(path).Get(), &getfileOp);

		if (SUCCEEDED(hr))
		{
			Make<AsyncOperation<IStorageFile>>(getfileOp, [=]
			{
				IStorageFile* result = nullptr;
				getfileOp->GetResults(&result);
				return result;
			}).CopyTo(op);
		}
	}
	if (FAILED(hr))
	{
		Make<AsyncOperation<IStorageFile>>([=]
		{
			return nullptr;
		}).CopyTo(op);
	}
	return hr;
}

HRESULT WINAPI GetStorageFolderFromPath(wchar_t *path, IStorageFolder **file)
{
	HRESULT hr = S_OK;
	*file = nullptr;
	if (!s_storFolderFactory)
	{
		hr = ABI::Windows::Foundation::GetActivationFactory(
			HStringReference(RuntimeClass_Windows_Storage_StorageFolder).Get(),
			&s_storFolderFactory);
	}

	if (SUCCEEDED(hr))
	{
		ComPtr<ABI::Windows::Foundation::IAsyncOperation<ABI::Windows::Storage::StorageFolder*>> asyncOp;
		hr = s_storFolderFactory->GetFolderFromPathAsync(
			HStringReference(path).Get(),
			&asyncOp);

		if (SUCCEEDED(hr))
		{
			ComPtr<IAsyncInfo> asyncInfo;
			hr = asyncOp.As(&asyncInfo);

			if (SUCCEEDED(hr))
			{
				AsyncStatus status;

				while (SUCCEEDED(hr = asyncInfo->get_Status(&status)) && status == AsyncStatus::Started)
					SleepEx(0, TRUE);

				if (FAILED(hr) || status != AsyncStatus::Completed)
				{
					asyncInfo->get_ErrorCode(&hr);
				}
				else
				{
					asyncOp->GetResults(file);
				}
			}
		}
	}

	return hr;
}

HRESULT WINAPI GetStorageFolderFromPathAsync(wchar_t *path, IAsyncOperation<IStorageFolder*>** op)
{
	HRESULT hr = S_OK;
	if (!s_storFolderFactory)
	{
		hr = ABI::Windows::Foundation::GetActivationFactory(
			HStringReference(RuntimeClass_Windows_Storage_StorageFolder).Get(),
			&s_storFolderFactory);
	}

	if (SUCCEEDED(hr))
	{
		ComPtr<ABI::Windows::Foundation::IAsyncOperation<ABI::Windows::Storage::StorageFolder*>> asyncOp;
		hr = s_storFolderFactory->GetFolderFromPathAsync(
			HStringReference(path).Get(),
			&asyncOp);

		if (SUCCEEDED(hr))
		{
			Make<AsyncOperation<IStorageFolder>>(asyncOp, [=]
			{
				IStorageFolder* result = nullptr;
				asyncOp->GetResults(&result);
				return result;
			}).CopyTo(op);
		}
	}
	if (FAILED(hr))
	{
		Make<AsyncOperation<IStorageFolder>>([=]
		{
			return nullptr;
		}).CopyTo(op);
	}
	return hr;
}

BOOL WINAPI ShouldUseWinRT(wchar_t *path)
{
	BOOL useWinRT = FALSE;

	WIN32_FILE_ATTRIBUTE_DATA data;
	if (GetFileAttributesEx(path, GET_FILEEX_INFO_LEVELS::GetFileExInfoStandard, &data))
	{
		if ((data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) == 0 &&
			(data.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0)
		{
			WIN32_FIND_DATA findData;
			auto handle = FindFirstFileEx(path, FindExInfoBasic, &findData, FindExSearchNameMatch, nullptr, NULL);
			if (handle != INVALID_HANDLE_VALUE)
			{
				useWinRT = findData.dwReserved0 == IO_REPARSE_TAG_FILE_PLACEHOLDER;
				FindClose(handle);
			}
		}
	}

	auto error = GetLastError();
	switch (error)
	{
	case ERROR_FILE_NOT_FOUND:
	case ERROR_PATH_NOT_FOUND:
	case ERROR_ACCESS_DENIED:
		useWinRT = true;
		break;
	default:
		break;
	}

	return useWinRT;
}