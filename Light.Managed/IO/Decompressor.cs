using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage; 


namespace Light.NETCore.IO
{
    public static class Decompressor
    {
        static public IAsyncOperation<bool> UnZipFile(StorageFile file, StorageFolder destination)
        {
            return Task.Run<bool>(async () =>
            {
                try
                {
                    var filename = file.DisplayName;
                    var zipStream = await file.OpenStreamForReadAsync();

                    var zipMemoryStream = new MemoryStream((int)zipStream.Length);

                    zipStream.CopyTo(zipMemoryStream);
                    var storage = destination;

                    var archive = new ZipArchive(zipMemoryStream, ZipArchiveMode.Read);

                    foreach (var entry in archive.Entries)
                    {
                        try
                        {
                            if (entry.Name == "")
                            {
                                // Folder 
                                await CreateRecursiveFolder(storage, entry);
                            }
                            else
                            {
                                // File 
                                await ExtractFile(storage, entry);
                            }
                        }

                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }).AsAsyncOperation<bool>();
        }

        static private async Task CreateRecursiveFolder(StorageFolder folder, ZipArchiveEntry entry)
        {
            var steps = entry.FullName.Split('/').ToList();

            steps.RemoveAt(steps.Count() - 1);

            foreach (var i in steps)
            {
                try
                {
                    var newFolder = await folder.CreateFolderAsync(i, CreationCollisionOption.OpenIfExists);

                }
                catch (Exception ex)
                {
                    var x = ex;
                }
            }
        }

        static private async Task ExtractFile(StorageFolder folder, ZipArchiveEntry entry)
        {
            var steps = entry.FullName.Split('/').ToList();

            steps.RemoveAt(steps.Count() - 1);

            foreach (var i in steps)
            {
                folder = await folder.CreateFolderAsync(i, CreationCollisionOption.OpenIfExists);
            }

            using (var fileData = entry.Open())
            {
                var outputFile = await folder.CreateFileAsync(entry.Name, CreationCollisionOption.ReplaceExisting);

                using (var outputFileStream = await outputFile.OpenStreamForWriteAsync())
                {
                    fileData.CopyTo(outputFileStream);
                    await outputFileStream.FlushAsync();
                }
            }
        } 

    }
}
