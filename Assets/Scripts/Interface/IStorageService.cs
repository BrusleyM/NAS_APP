using System.Collections.Generic;
using System.Threading.Tasks;

namespace NAS.Interfaces
{
    public interface IStorageService
    {
        Task<Result> UploadModelAsync(string localFilePath, string modelKey);
        Task<Result<byte[]>> DownloadModelAsync(string modelKey);
        Task<Result<List<string>>> ListModelsAsync(string prefix = "");
        Task<Result> UploadModelsAsync(IEnumerable<string> localFilePaths, IEnumerable<string> modelKeys);
        Task<Result<List<(string Key, byte[] Data)>>> DownloadModelsAsync(IEnumerable<string> modelKeys);
    }
}