using NAS.Core.Interfaces;

namespace NAS.Storage
{
    public class DevStorageService : BaseStorageService
    {
        public DevStorageService(IStorageConfig config)
            : base(config.CreateS3Client(), config.BucketName)
        {
        }
    }
}