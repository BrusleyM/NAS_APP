using NAS.Interfaces;

namespace NAS.Storage
{
    public class ProdStorageService : BaseStorageService
    {
        public ProdStorageService(IStorageConfig config)
            : base(config.CreateS3Client(), config.BucketName)
        {
        }
    }
}