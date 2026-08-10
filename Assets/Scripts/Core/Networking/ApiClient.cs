using System;
using System.Collections;
using NAS.Core.Auth.Dtos;
using UnityEngine;
using UnityEngine.Networking;

namespace NAS.Core.Networking
{
    public sealed class ApiClient
    {
        private readonly ApiSettings _settings;

        public ApiClient(ApiSettings settings)
        {
            _settings = settings;
        }

        public IEnumerator PostJson<TRequest, TResponse>(string path, TRequest payload, Action<ApiResult<TResponse>> completed)
        {
            var request = new UnityWebRequest($"{_settings.BaseUrl}/{path.TrimStart('/')}", UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload))),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = _settings.TimeoutSeconds
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                completed?.Invoke(ApiResult<TResponse>.FromSuccess(JsonUtility.FromJson<TResponse>(request.downloadHandler.text)));
                request.Dispose();
                yield break;
            }

            completed?.Invoke(ApiResult<TResponse>.FromError(ParseError(request)));
            request.Dispose();
        }

        private static ApiError ParseError(UnityWebRequest request)
        {
            var problem = string.IsNullOrWhiteSpace(request.downloadHandler?.text)
                ? null
                : JsonUtility.FromJson<ApiProblemDetailsDto>(request.downloadHandler.text);

            return new ApiError(
                request.responseCode,
                problem?.errorCode,
                problem?.detail ?? request.error,
                problem?.traceId);
        }
    }
}
