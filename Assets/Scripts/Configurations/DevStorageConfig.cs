using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using NAS.Core.Interfaces;
using UnityEngine;

namespace NAS.Configuration
{
    [CreateAssetMenu(fileName = "DevStorageConfig", menuName = "NAS/Dev Storage Config")]
    public class DevStorageConfig : ScriptableObject, IStorageConfig
    {
        [SerializeField] private string _devAccessKey;
        [SerializeField] private string _devSecretKey;
        [SerializeField] private string _devServiceUrl = "https://fly.storage.tigris.dev";
        [SerializeField] private string _devBucketName;

        public string ServiceUrl => _devServiceUrl;
        public string BucketName => _devBucketName;

        public IAmazonS3 CreateS3Client()
        {
            var credentials = new BasicAWSCredentials(_devAccessKey, _devSecretKey);
            var config = new AmazonS3Config
            {
                ServiceURL = ServiceUrl,
                UseHttp = false,
                ForcePathStyle = true
            };
            return new AmazonS3Client(credentials, config);
        }
    }
}