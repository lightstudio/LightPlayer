## Light.dll

### Light.SoftwareDecoder Class

#### C++

Add the following namespaces

    using namespace Light;
    using namespace Windows::Storage;
    using namespace Windows::Storage::Streams;
    using namespace Windows::UI::Xaml::Controls;
    using namespace Platform;

Call `SoftwareDecoder::InitializeAll()` when app starts. 
Play an audio file with SoftwareDecoder class
    
    auto mediaElement = ref new MediaElement();
    //Add mediaElement to XAML
    StorageFile^ storageFile = ...; //Get the file somewhere
    try
    {
        mediaElement->SetMediaStreamSource(
            SoftwareDecoder::GetMediaStreamSourceByFile(
                storageFile));
    }
    catch (Exception^ exc)
    {
        //handle the exception
    }
    mediaElement->Play();

#### C Sharp

Add the following namespaces

    using Light;
    using Windows.Storage;
    using Windows.Storage.Streams;
    using Windows.UI.Xaml.Controls;
    using Platform;

Call `SoftwareDecoder.InitializeAll()` when app starts. 
Play an audio file with SoftwareDecoder class

    var mediaElement = new MediaElement();
    //Add mediaElement to XAML
    StorageFile storageFile = ...; //Get the file somewhere
    try
    {
        mediaElement.SetMediaStreamSource(
            SoftwareDecoder.GetMediaStreamSourceByFile(
                storageFile));
    }
    catch (exception exc)
    {
        //handle the exception
    }
    mediaElement.Play();

Note:  
1. The file opened by SoftwareDecoder will be closed automatically when the playback is finished. However, if `GetMediaStreamSourceByStream` is called, the stream will **NOT** be closed automatically.  
2. All methods in SoftwareDecoder class are synchronized methods. They should **NOT** be called in UI thread, which may cause bad user experience.

### Export Functions

#### C Sharp

Add the UnsafeMethods class to the project.

    enum FileType
    {
        INVALID_FILE = -10,
        OTHER_FILE = 0,
        ALAC_FILE = 10,
        AAC_FILE = 20,
    }
    
    private static class UnsafeMethods
    {
        [DllImport("Light.dll", CallingConvention = CallingConvention.Winapi)]
        static extern void GetAlbumCoverFromStream(Windows.Storage.Streams.IRandomAccessStream stream, out Windows.Storage.Streams.IBuffer out_buffer);
        
        [DllImport("Light.dll", CallingConvention = CallingConvention.Winapi)]
        static extern void GetAlbumCoverFromFile(Windows.Storage.StorageFile file, out Windows.Storage.Streams.IBuffer out_buffer);
        
        [DllImport("Light.dll", CallingConvention = CallingConvention.Winapi)]
        static extern void GetAlbumCoverFromPath([MarshalAs(UnmanagedType.LPStr)] string path, out Windows.Storage.Streams.IBuffer out_buffer);
        
        [DllImport("Light.dll", CallingConvention = CallingConvention.Winapi)]
        static extern FileType IsALAC(Windows.Storage.IStorageFile file);

    }
    
##### Code Example

Get the first album cover from an audio file

    var storageFile = ...;
    Windows.Storage.Streams.IBuffer buffer = null;
    UnsafeMethods.GetAlbumCoverFromFile(storageFile, out buffer);
    
    //To display an album cover from IBuffer
    if (buffer != null)
    {
        var imagesource = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
        Windows.Storage.Streams.InMemoryRandomAccessStream mem = new Windows.Storage.Streams.InMemoryRandomAccessStream();
        await mem.WriteAsync(buf);
        mem.Seek(0);
        imagesource.SetSource(mem);
        image.Source = imagesource;
    }
    
Check whether an AAC/M4A/MP4 file is encoded in ALAC

    var fileType = IsALAC(m4aFile);
    switch (type)
    {
        case FileType.AAC_FILE:
            me.SetSource(await sf.OpenReadAsync(), "");
            break;
        case FileType.ALAC_FILE:
            me.SetMediaStreamSource(Light.SoftwareDecoder.GetMediaStreamSourceByFile(sf));
            break;
        case FileType.OTHER_FILE:
        case FileType.INVALID_FILE:
            //handle the error here.
            break;
    }