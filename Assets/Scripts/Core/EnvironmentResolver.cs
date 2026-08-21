using NAS.Core.Networking;
using UnityEngine;

namespace NAS.Core
{
    // Resolves which ApiSettings + cert-trust to use from GameManager.CurrentEnvironment,
    // reading the three ApiSettings references from GameManager itself rather than having
    // every caller wire its own copies - shared by every feature that needs to talk to the
    // API, so this decision (and its config) lives in exactly one place instead of being
    // re-implemented (and able to drift) per caller.
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

        public static Resolved Resolve(string logPrefix)
        {
            var gameManager = GameManager.Instance;
            var environment = gameManager != null ? gameManager.CurrentEnvironment : AppEnvironment.Local;

            if (environment == AppEnvironment.ApiDomain)
            {
                if (gameManager != null && gameManager.ApiDomainSettings != null)
                    return new Resolved(gameManager.ApiDomainSettings, trustAnyCertificate: true);

                Debug.LogWarning($"{logPrefix} GameManager.CurrentEnvironment is ApiDomain but the ApiDomain ApiSettings asset is not assigned. Falling back to local.");
            }

            if (environment == AppEnvironment.ApiIp)
            {
                if (gameManager != null && gameManager.ApiIpSettings != null)
                    return new Resolved(gameManager.ApiIpSettings, trustAnyCertificate: true);

                Debug.LogWarning($"{logPrefix} GameManager.CurrentEnvironment is ApiIp but the ApiIp ApiSettings asset is not assigned. Falling back to local.");
            }

            return new Resolved(gameManager != null ? gameManager.ApiSettings : null, trustAnyCertificate: false);
        }
    }
}
