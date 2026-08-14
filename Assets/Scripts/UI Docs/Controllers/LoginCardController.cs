using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using NAS.Core.Events;

namespace NAS.UI.Controllers
{
    /// <summary>
    /// Purely a view. It reads the login form and publishes what happened —
    /// it doesn't know or care who (AuthController) handles it, or who
    /// (ParentPageController) navigates away afterwards.
    /// </summary>
    public class LoginCardController : MonoBehaviour
    {
        [SerializeField] private TextField _emailField;
        [SerializeField] private TextField _passwordField;
        [SerializeField] private Button _loginButton;
        [SerializeField] private Label _richTextLabel;
        [SerializeField] private Label _errorLabel; // general/backend-level error, "login-error-label"
        [SerializeField] private Label _emailErrorLabel;
        [SerializeField] private Label _passwordErrorLabel;
        [SerializeField] private Button _passwordToggleButton;

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;

            _emailField = root.Q<TextField>("email-field");
            _passwordField = root.Q<TextField>("password-field");
            _loginButton = root.Q<Button>("login-button");
            _richTextLabel = root.Q<Label>("register-link-label");
            _errorLabel = root.Q<Label>("login-error-label");
            _emailErrorLabel = root.Q<Label>("email-error-label");
            _passwordErrorLabel = root.Q<Label>("password-error-label");
            _passwordToggleButton = root.Q<Button>("password-toggle-button");

            if (_loginButton != null)
                _loginButton.clicked += OnLoginClicked;

            if (_richTextLabel != null)
                _richTextLabel.RegisterCallback<PointerUpLinkTagEvent>(OnLinkClicked);

            if (_passwordToggleButton != null)
            {
                _passwordToggleButton.clicked += TogglePasswordVisibility;
                // Set initial state
                _passwordToggleButton.text = string.Empty;
                UpdateToggleButtonIcon(_passwordToggleButton, true);
                if (_passwordField != null) _passwordField.isPasswordField = true;
            }

            EventBus.Subscribe<AuthFailedEvent>(OnAuthFailed);
        }

        private void OnLoginClicked()
        {
            ClearAllErrors();
            EventBus.Publish(new LoginRequestedEvent(_emailField?.value, _passwordField?.value));
        }

        private void OnLinkClicked(PointerUpLinkTagEvent evt)
        {
            if (evt.linkID == "register")
                EventBus.Publish(new NavigateToRegisterRequestedEvent());
        }

        private void TogglePasswordVisibility()
        {
            if (_passwordField == null) return;
            _passwordField.isPasswordField = !_passwordField.isPasswordField;
            if (_passwordToggleButton != null)
                UpdateToggleButtonIcon(_passwordToggleButton, _passwordField.isPasswordField);
        }

        private static void UpdateToggleButtonIcon(Button button, bool isHidden)
        {
            if (isHidden) button.AddToClassList("password-toggle--hidden");
            else button.RemoveFromClassList("password-toggle--hidden");
        }

        private void OnAuthFailed(AuthFailedEvent evt)
        {
            if (evt.FieldErrors != null)
            {
                SetFormError(null);
                SetFieldError(_emailField, _emailErrorLabel,
                    evt.FieldErrors.TryGetValue("email", out var emailMsg) ? emailMsg : null);
                SetFieldError(_passwordField, _passwordErrorLabel,
                    evt.FieldErrors.TryGetValue("password", out var pwMsg) ? pwMsg : null);
            }
            else
            {
                ClearAllErrors();
                SetFormError(evt.Reason);
            }
        }

        private void ClearAllErrors()
        {
            SetFormError(null);
            SetFieldError(_emailField, _emailErrorLabel, null);
            SetFieldError(_passwordField, _passwordErrorLabel, null);
        }

        private static void SetFieldError(TextField field, Label errorLabel, string message)
        {
            bool hasError = !string.IsNullOrEmpty(message);

            if (field != null)
            {
                if (hasError) field.AddToClassList("input-field--error");
                else field.RemoveFromClassList("input-field--error");
            }

            if (errorLabel != null)
            {
                errorLabel.text = message ?? string.Empty;
                errorLabel.style.display = hasError ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void SetFormError(string message)
        {
            if (_errorLabel == null) return;
            _errorLabel.text = message ?? string.Empty;
            _errorLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void OnDisable()
        {
            if (_loginButton != null) _loginButton.clicked -= OnLoginClicked;
            if (_richTextLabel != null)
                _richTextLabel.UnregisterCallback<PointerUpLinkTagEvent>(OnLinkClicked);
            if (_passwordToggleButton != null)
                _passwordToggleButton.clicked -= TogglePasswordVisibility;

            EventBus.Unsubscribe<AuthFailedEvent>(OnAuthFailed);
        }
    }
}
