using UnityEngine.Networking;

namespace NAS.Core.Networking
{
    public sealed class AcceptAllCertificatesHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}
