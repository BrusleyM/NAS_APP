using System;

namespace NAS.Core.Auth.Dtos
{
    [Serializable]
    public sealed class ApiProblemDetailsDto
    {
        public string type;
        public string title;
        public int status;
        public string detail;
        public string instance;
        public string errorCode;
        public string traceId;
    }
}
