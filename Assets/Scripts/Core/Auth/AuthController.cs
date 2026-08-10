using UnityEngine;
using NAS.Core.Auth.Dtos;
using NAS.Core.Events;
using NAS.Core.Models;
using NAS.Core.Networking;

namespace NAS.Core.Auth
{
    /// <summary>
    /// Orchestrates auth requests and publishes domain events. UI and HTTP
    /// details stay outside the caller-facing event flow.
    /// </summary>
    public sealed class AuthController : MonoBehaviour
    {
        private const string LogPrefix = "[NAS Auth]";

        [SerializeField] private ApiSettings _apiSettings;

        private ICustomerAuthApi _authApi;
        private AuthSession _session;
        private bool _requestInProgress;

        public AuthSession Session => _session;

        private void Awake()
        {
            if (_apiSettings == null)
            {
                Debug.LogError($"{LogPrefix} ApiSettings is missing on AuthController.");
                return;
            }

            _session = new AuthSession(new TokenStorage(_apiSettings.PersistToken));
            _authApi = new CustomerAuthApi(this, _apiSettings);
            Debug.Log($"{LogPrefix} Ready. API base URL: {_apiSettings.BaseUrl}");
        }

        private void OnEnable()
        {
            EventBus.Subscribe<LoginRequestedEvent>(HandleLoginRequested);
            EventBus.Subscribe<RegisterRequestedEvent>(HandleRegisterRequested);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LoginRequestedEvent>(HandleLoginRequested);
            EventBus.Unsubscribe<RegisterRequestedEvent>(HandleRegisterRequested);
        }

        private void HandleLoginRequested(LoginRequestedEvent evt)
        {
            if (_requestInProgress)
            {
                Debug.Log($"{LogPrefix} Login ignored because another request is in progress.");
                return;
            }

            if (string.IsNullOrWhiteSpace(evt.Email) || string.IsNullOrWhiteSpace(evt.Password))
            {
                PublishFailure("Email and password are required.");
                return;
            }

            if (_authApi == null)
            {
                PublishFailure("Authentication is not configured.");
                return;
            }

            _requestInProgress = true;
            Debug.Log($"{LogPrefix} Login request started.");
            _authApi.Login(new NAS.Core.Auth.Dtos.CustomerLoginRequest
            {
                email = evt.Email.Trim(),
                password = evt.Password
            }, HandleApiResult);
        }

        private void HandleRegisterRequested(RegisterRequestedEvent evt)
        {
            if (_requestInProgress)
            {
                Debug.Log($"{LogPrefix} Registration ignored because another request is in progress.");
                return;
            }

            if (string.IsNullOrWhiteSpace(evt.FirstName) ||
                string.IsNullOrWhiteSpace(evt.LastName) ||
                string.IsNullOrWhiteSpace(evt.CellNumber) ||
                string.IsNullOrWhiteSpace(evt.Email) ||
                string.IsNullOrWhiteSpace(evt.Password))
            {
                PublishFailure("First name, last name, phone, email, and password are required.");
                return;
            }

            if (evt.Password != evt.ConfirmPassword)
            {
                PublishFailure("Passwords do not match.");
                return;
            }

            if (_authApi == null)
            {
                PublishFailure("Authentication is not configured.");
                return;
            }

            _requestInProgress = true;
            Debug.Log($"{LogPrefix} Registration request started.");
            _authApi.Register(new NAS.Core.Auth.Dtos.CustomerRegisterRequest
            {
                firstName = evt.FirstName.Trim(),
                lastName = evt.LastName.Trim(),
                cellNumber = evt.CellNumber.Trim(),
                email = evt.Email.Trim(),
                password = evt.Password
            }, HandleApiResult);
        }

        private void HandleApiResult(ApiResult<NAS.Core.Auth.Dtos.CustomerAuthResponse> result)
        {
            _requestInProgress = false;

            if (!result.Success)
            {
                Debug.LogWarning($"{LogPrefix} Authentication request failed. Status: {result.Error.StatusCode}, Code: {result.Error.ErrorCode ?? "unknown"}, Trace: {result.Error.TraceId ?? "none"}.");
                PublishFailure(result.Error.UserMessage);
                return;
            }

            try
            {
                _session.Apply(result.Value);
                Debug.Log($"{LogPrefix} Authentication succeeded for customer ID {_session.CurrentUser?.id}.");
                EventBus.Publish(new AuthSucceededEvent(_session.CurrentUser));
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                PublishFailure("The authentication response was invalid.");
            }
        }

        public void Logout()
        {
            Debug.Log($"{LogPrefix} Logout requested.");
            _session?.Logout();
        }

        private static void PublishFailure(string reason)
        {
            Debug.LogWarning($"{LogPrefix} Auth flow stopped: {reason}");
            EventBus.Publish(new AuthFailedEvent(reason));
        }
    }
}
