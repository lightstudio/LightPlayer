Light.Playback.Playlist
=======

PlaylistManager Class
------

### Syntax
#### C Sharp
`public sealed class PlaylistManager`
#### C++
`public ref class PlaylistManager sealed`

*** IMPORTANT: This class is not visible to JavaScript apps. ***

### Members
The PlaylistManager class has these types of members:

- Constructors
- Events
- Methods
- Properties

### Constructors
The PlaylistManager class has these constructors.

- `PlaylistManager()`
    > Initialize a new instance of the PlaylistManager class.

- `PlaylistManager(StorageFile listFile)`
    > Initialize a new instance of the PlaylistManager class with specificed playlist file.

- `PlaylistManager(string listData)`
    > Initialize a new instance of the PlaylistManager class with specificed playlist data string.

### Events
The PlaylistManager class has these events.

*** IMPORTANT: These events are only available for Windows. ***

- `AllMediaClipStarted`
- `AllMediaClipEnded`
- `MediaClipStarted`
- `MediaClipChanged`
- `MediaClipFailed`

### Methods
The PlaylistManager class has these methods. With C#, Visual Basic, and C++, it also inherits methods from the Object class.

- `AddClipAsync(IStorageFile mediaFile)`
- `AddClipAsync(IStorageFile mediaFile, IStorageFile mediaSectionManifest)`
- `AddClipsAsync(IEnumerable<IStorageFile> mediaFiles)`
- `AddClips(IEnumerable<KeyValuePair<IMediaInfo,string>> indexedMediaFiles)`
- `RemoveClip(int index)`
- `Stringfy()`

### Properties
The PlaylistManager class has these properties.

- `Clips`
- `CurrentClip`
- `IsShuffle`
- `LoopOption`
- `TagResolver`

### Remarks
If `AddClipAsync` or `AddClipsAsync` is called, a tag resolver must be provided for `TagResolver` property. Otherwise, a `InvalidOperationException` will be thrown.

The playlist can be serialized to a JSON array or deserialized from a JSON array for storage purpose.

### Requirements
Minimum supported phone: Light 2.0 INT RELEASE 370
Minimum supported desktop: Light 2.1 INT RELEASE 0

### See Also
- ITagResolver
- IMediaInfo

