namespace NAS.Core.Networking
{
    public readonly struct ApiResult<T>
    {
        public bool Success { get; }
        public T Value { get; }
        public ApiError Error { get; }

        private ApiResult(bool success, T value, ApiError error)
        {
            Success = success;
            Value = value;
            Error = error;
        }

        public static ApiResult<T> FromSuccess(T value) => new(true, value, default);
        public static ApiResult<T> FromError(ApiError error) => new(false, default, error);
    }

    public readonly struct ApiError
    {
        public long StatusCode { get; }
        public string ErrorCode { get; }
        public string Detail { get; }
        public string TraceId { get; }

        public ApiError(long statusCode, string errorCode, string detail, string traceId)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            Detail = detail;
            TraceId = traceId;
        }

        public string UserMessage => string.IsNullOrWhiteSpace(Detail)
            ? "Something went wrong. Please try again later."
            : Detail;
    }
}
