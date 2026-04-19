using Amazon.S3;

namespace NAS.Core.Interfaces
{
    public interface IStorageConfig
    {
        string ServiceUrl { get; }
        string BucketName { get; }
        IAmazonS3 CreateS3Client();
    }
}