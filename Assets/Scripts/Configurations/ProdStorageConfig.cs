using Amazon;
using Amazon.CognitoIdentity;
using Amazon.S3;
using NAS.Core.Interfaces;
using UnityEngine;

namespace NAS.Configuration
{
    [CreateAssetMenu(fileName = "ProdStorageConfig", menuName = "NAS/Prod Storage Config")]
    public class ProdStorageConfig : ScriptableObject, IStorageConfig
    {
        [SerializeField] private string _prodIdentityPoolId;
        [SerializeField] private string _prodRegion = "af-south-1";
        [SerializeField] private string _prodServiceUrl = "https://fly.storage.tigris.dev";
        [SerializeField] private string _prodBucketName;

        public string ServiceUrl => _prodServiceUrl;
        public string BucketName => _prodBucketName;

        public IAmazonS3 CreateS3Client()
        {
            var credentials = new CognitoAWSCredentials(
                _prodIdentityPoolId,
                RegionEndpoint.GetBySystemName(_prodRegion)
            );
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