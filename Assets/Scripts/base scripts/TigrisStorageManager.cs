using System;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using UnityEngine;

public class TigrisStorageManager : MonoBehaviour
{
    [Header("Tigris Configuration")]
    [SerializeField] private string _accessKeyId = "tid_YOUR_ACCESS_KEY";
    [SerializeField] private string _secretAccessKey = "tsec_YOUR_SECRET_KEY";
    [SerializeField] private string _serviceURL = "https://fly.storage.tigris.dev";
    [SerializeField] private string _bucketName = "your-bucket-name";

    private IAmazonS3 _s3Client;

    void Awake()
    {
        InitializeS3Client();
    }
    //test
    async void Start()
    {
        //check objects in the bucket
        // Wait a moment for the client to initialize (optional)
        await Task.Delay(100);
        ListObjects();
        await Task.Delay(100);
        TestUpload();
        await Task.Delay(100);
        ListObjects();
    }
    public async void TestUpload()
    {
        string testPath = Application.persistentDataPath + "/test.txt";
        System.IO.File.WriteAllText(testPath, "Hello Tigris from Unity!");
        bool success = await UploadFileAsync(testPath, "test/hello.txt");
        if (success)
        {
            Debug.Log("Upload test passed.");
        }
    }

    private void InitializeS3Client()
    {
        var credentials = new BasicAWSCredentials(_accessKeyId, _secretAccessKey);
        var config = new AmazonS3Config
        {
            ServiceURL = _serviceURL,
            UseHttp = false,
            ForcePathStyle = true // Critical for Tigris
        };

        _s3Client = new AmazonS3Client(credentials, config);
        Debug.Log("S3 Client initialized.");
    }

    /// <summary>
    /// Lists up to 10 objects in the bucket (for testing).
    /// </summary>
    public async void ListObjects()
    {
        try
        {
            var request = new ListObjectsRequest
            {
                BucketName = _bucketName,
                MaxKeys = 10
            };

            var response = await _s3Client.ListObjectsAsync(request);
            Debug.Log($"Found {response.S3Objects.Count} objects:");
            foreach (var obj in response.S3Objects)
            {
                Debug.Log($" - {obj.Key} ({obj.Size} bytes)");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ListObjects failed: {e.Message}");
        }
    }

    /// <summary>
    /// Uploads a file from the local file system.
    /// </summary>
    public async Task<bool> UploadFileAsync(string localFilePath, string key)
    {
        try
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                FilePath = localFilePath
            };

            var response = await _s3Client.PutObjectAsync(putRequest);
            Debug.Log($"Uploaded {key} successfully. ETag: {response.ETag}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Upload failed: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Downloads an object as a byte array.
    /// </summary>
    public async Task<byte[]> DownloadObjectAsync(string key)
    {
        try
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            using var response = await _s3Client.GetObjectAsync(getRequest);
            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
        catch (Exception e)
        {
            Debug.LogError($"Download failed: {e.Message}");
            return null;
        }
    }

    private void OnDestroy()
    {
        if (_s3Client != null)
        {
            _s3Client.Dispose();
        }
    }
}