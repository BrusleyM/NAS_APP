using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace NAS.Core.Networking
{
    // Lazily downloads and caches textures by URL. Deliberately generic - no
    // awareness of CarData or any other caller-specific type.
    public sealed class RemoteTextureLoader
    {
        // Static: survives the coroutine-hosting MonoBehaviour being destroyed
        // and recreated (e.g. CarSelectionScreenController is rebuilt every
        // time the car selection screen is re-entered), so revisiting the
        // screen doesn't redownload thumbnails already fetched this session.
        private static readonly Dictionary<string, Texture2D> SharedCache = new();

        // Instance-level: one retry per RemoteTextureLoader instance (i.e. per
        // screen visit), not one retry ever and not one retry per rebind.
        private readonly HashSet<string> _failedUrls = new();
        private readonly Dictionary<string, List<Action<Texture2D>>> _pending = new();

        private readonly MonoBehaviour _coroutineRunner;
        private readonly bool _trustAnyCertificate;

        public RemoteTextureLoader(MonoBehaviour coroutineRunner, bool trustAnyCertificate = false)
        {
            _coroutineRunner = coroutineRunner;
            _trustAnyCertificate = trustAnyCertificate;
        }

        public void RequestTexture(string url, Action<Texture2D> completed)
        {
            if (string.IsNullOrEmpty(url))
            {
                completed?.Invoke(null);
                return;
            }

            if (SharedCache.TryGetValue(url, out var cached))
            {
                completed?.Invoke(cached);
                return;
            }

            if (_failedUrls.Contains(url))
            {
                completed?.Invoke(null);
                return;
            }

            if (_pending.TryGetValue(url, out var waiters))
            {
                // Already downloading this exact URL - queue behind it instead
                // of starting a second concurrent request for the same image.
                waiters.Add(completed);
                return;
            }

            _pending[url] = new List<Action<Texture2D>> { completed };
            _coroutineRunner.StartCoroutine(Download(url));
        }

        private IEnumerator Download(string url)
        {
            var request = UnityWebRequestTexture.GetTexture(url);
            try
            {
                if (_trustAnyCertificate)
                {
                    request.certificateHandler = new AcceptAllCertificatesHandler();
                }

                yield return request.SendWebRequest();

                var waiters = _pending.TryGetValue(url, out var list) ? list : null;
                _pending.Remove(url);

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var texture = DownloadHandlerTexture.GetContent(request);
                    SharedCache[url] = texture;
                    InvokeAll(waiters, texture);
                }
                else
                {
                    _failedUrls.Add(url);
                    InvokeAll(waiters, null);
                }
            }
            finally
            {
                request.Dispose();
            }
        }

        private static void InvokeAll(List<Action<Texture2D>> waiters, Texture2D texture)
        {
            if (waiters == null)
                return;

            foreach (var waiter in waiters)
                waiter?.Invoke(texture);
        }
    }
}
