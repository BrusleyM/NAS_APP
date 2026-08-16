using NAS.Core;
using UnityEngine;

namespace NAS.Core.Networking
{
    // Resolves which ApiSettings + cert-trust to use from GameManager.CurrentEnvironment.
    // Shared by every feature that needs to talk to the API, so this decision lives in
    // exactly one place instead of being re-implemented (and able to drift) per caller.
    public static class EnvironmentResolver
    {
        public readonly struct Resolved
        {
            public readonly ApiSettings Settings;
            public readonly bool TrustAnyCertificate;

            public Resolved(ApiSettings settings, bool trustAnyCertificate)
            {
                Settings = settings;
                TrustAnyCertificate = trustAnyCertificate;
            }
        }

        public static Resolved Resolve(ApiSettings localSettings, ApiSettings apiDomainSettings, string logPrefix)
        {
            var environment = GameManager.Instance != null
                ? GameManager.Instance.CurrentEnvironment
                : AppEnvironment.Local;

            if (environment == AppEnvironment.ApiDomain)
            {
                if (apiDomainSettings != null)
                    return new Resolved(apiDomainSettings, trustAnyCertificate: true);

                Debug.LogWarning($"{logPrefix} GameManager.CurrentEnvironment is ApiDomain but the ApiDomain ApiSettings asset is not assigned. Falling back to local.");
            }

            return new Resolved(localSettings, trustAnyCertificate: false);
        }
    }
}
