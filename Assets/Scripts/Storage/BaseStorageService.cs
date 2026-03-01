using Amazon.S3;
using Amazon.S3.Model;
using NAS.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace NAS.Storage
{
    public abstract class BaseStorageService : IStorageService
    {
        protected readonly IAmazonS3 _s3Client;
        protected readonly string _bucketName;

        protected BaseStorageService(IAmazonS3 s3Client, string bucketName)
        {
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
            _bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
        }

        public virtual async Task<Result> UploadModelAsync(string localFilePath, string modelKey)
        {
            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = modelKey,
                    FilePath = localFilePath
                };
                await _s3Client.PutObjectAsync(putRequest);
                Debug.Log($"[BaseStorageService] Uploaded {modelKey}");
                return Result.Success();
            }
            catch (AmazonS3Exception e)
            {
                Debug.LogError($"[BaseStorageService] S3 upload failed: {e.Message}");
                return Result.Failure($"S3 upload error: {e.Message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[BaseStorageService] Unexpected upload error: {e.Message}");
                return Result.Failure($"Unexpected error: {e.Message}");
            }
        }

        public virtual async Task<Result<byte[]>> DownloadModelAsync(string modelKey)
        {
            try
            {
                var getRequest = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = modelKey
                };
                using var response = await _s3Client.GetObjectAsync(getRequest);
                using var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream);
                return Result<byte[]>.Success(memoryStream.ToArray());
            }
            catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return Result<byte[]>.Failure($"Model '{modelKey}' not found.");
            }
            catch (AmazonS3Exception e)
            {
                return Result<byte[]>.Failure($"S3 download error: {e.Message}");
            }
            catch (Exception e)
            {
                return Result<byte[]>.Failure($"Unexpected error: {e.Message}");
            }
        }

        public virtual async Task<Result<List<string>>> ListModelsAsync(string prefix = "")
        {
            try
            {
                var listRequest = new ListObjectsRequest
                {
                    BucketName = _bucketName,
                    Prefix = prefix
                };
                var response = await _s3Client.ListObjectsAsync(listRequest);
                var keys = new List<string>();
                foreach (var obj in response.S3Objects)
                    keys.Add(obj.Key);
                return Result<List<string>>.Success(keys);
            }
            catch (Exception e)
            {
                return Result<List<string>>.Failure($"List failed: {e.Message}");
            }
        }

        public virtual async Task<Result> UploadModelsAsync(IEnumerable<string> localFilePaths, IEnumerable<string> modelKeys)
        {
            var tasks = new List<Task<Result>>();
            using var enumerator1 = localFilePaths.GetEnumerator();
            using var enumerator2 = modelKeys.GetEnumerator();
            while (enumerator1.MoveNext() && enumerator2.MoveNext())
                tasks.Add(UploadModelAsync(enumerator1.Current, enumerator2.Current));

            var results = await Task.WhenAll(tasks);
            foreach (var result in results)
                if (!result.IsSuccess)
                    return Result.Failure("One or more uploads failed.");
            return Result.Success();
        }

        public virtual async Task<Result<List<(string Key, byte[] Data)>>> DownloadModelsAsync(IEnumerable<string> modelKeys)
        {
            var tasks = new List<Task<Result<(string Key, byte[] Data)>>>();
            foreach (var key in modelKeys)
                tasks.Add(DownloadModelWithKeyAsync(key));

            var results = await Task.WhenAll(tasks);
            var successList = new List<(string Key, byte[] Data)>();
            foreach (var result in results)
            {
                if (result.IsSuccess)
                    successList.Add(result.Value);
                else
                    Debug.LogWarning($"[BaseStorageService] Failed to download {result.Value.Key}: {result.ErrorMessage}");
            }
            return Result<List<(string Key, byte[] Data)>>.Success(successList);
        }

        private async Task<Result<(string Key, byte[] Data)>> DownloadModelWithKeyAsync(string key)
        {
            var downloadResult = await DownloadModelAsync(key);
            if (downloadResult.IsSuccess)
                return Result<(string Key, byte[] Data)>.Success((key, downloadResult.Value));
            else
                return Result<(string Key, byte[] Data)>.Failure(downloadResult.ErrorMessage);
        }
    }
}