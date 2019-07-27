# JavaScript Lyrics Source Reference Code

## Caller（C Sharp）

```
using Light.Lyrics.External;

public async void OnXxxxxx(...)
{
    await Task.Run(()=>
    {
        try
        {
            using (var s = new JsDownloadSource(
                jsContent: File.ReadAllText("xiami.js"), // Javascript String
                debug: true, // Enable debug mode
                debugSource: @"C:\xiami.js")) // Source path
            {
                string text = null;
                var res = s.LookupLrc("American Idiot", "Green Day");
                if (res.Count != 0)
                    text=s.DownloadLrc(res[0]);
                // Parse, UI and more
            }
        }
        catch(Exception e)
        {
            // Handle exceptions  
        }
    });
}
```

_Note：JsDownloadSource should be used from only one non-UI thread._

Enabling debugging mode allows Visual Studio to attach player process with script debugging mode.

## JavaScript Implementation

A JavaScript lyrics source requires implementation of `lookupLrc` and `downloadLrc` functions.

Available APIs:

*   Windows.Data.Xml.Dom Namespace
*   Windows.Networking Namespace
*   Windows.Web Namespace
*   xmlhttp object in LightLrcComponent.ExternalLrcInfo
*   api.log(_[string]_) method

## JavaScript Method Reference

### lookupLrc(title, artist) Method

Look up lyrics by title and artist.

#### Parameters  

| Name | Type | Description |
| --- | --- | --- |
| _title_ | `String` | Song title. |
| _artist_ | `String` | Artist. |

#### Return

| Type | Description |
| --- | --- |
| Array of `ExternalLrcInfo` | All retrieved results. |

### downloadLrc(lrcinfo) Method

下载指定歌词。

#### Parameters 

| Name | Type | Description |
| --- | --- | --- |
| _lrcinfo_ | `ExternalLrcInfo` | Instance of lyrics information object. |

#### Return 

| Type | Description |
| --- | --- |
| `String` | Downloaded lyrics text. |