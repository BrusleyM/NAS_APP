using UnityEngine;
using UnityEngine.SceneManagement;
using System;

namespace NAS.Core.Services
{
    public static class SceneLoader
    {
        public static event Action OnSceneLoadStarted;
        public static event Action<float> OnSceneLoadProgress;
        public static event Action OnSceneLoadComplete;

        public static async void LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            OnSceneLoadStarted?.Invoke();
            var operation = SceneManager.LoadSceneAsync(sceneName, mode);
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                OnSceneLoadProgress?.Invoke(operation.progress);
                await System.Threading.Tasks.Task.Yield();
            }
            OnSceneLoadProgress?.Invoke(1f);
            operation.allowSceneActivation = true;
            OnSceneLoadComplete?.Invoke();
        }
    }
}