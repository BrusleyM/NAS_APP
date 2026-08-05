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

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;

            _emailField = root.Q<TextField>("email-field");
            _passwordField = root.Q<TextField>("password-field");
            _loginButton = root.Q<Button>("login-button");
            _richTextLabel = root.Q<Label>("register-link-label");

            if (_loginButton != null)
                _loginButton.clicked += OnLoginClicked;

            if (_richTextLabel != null)
                _richTextLabel.RegisterCallback<PointerUpLinkTagEvent>(OnLinkClicked);
        }

        private void OnLoginClicked()
        {
            EventBus.Publish(new LoginRequestedEvent(_emailField?.value, _passwordField?.value));
        }

        private void OnLinkClicked(PointerUpLinkTagEvent evt)
        {
            if (evt.linkID == "register")
                EventBus.Publish(new NavigateToRegisterRequestedEvent());
        }

        private void OnDisable()
        {
            if (_loginButton != null) _loginButton.clicked -= OnLoginClicked;
            if (_richTextLabel != null)
                _richTextLabel.UnregisterCallback<PointerUpLinkTagEvent>(OnLinkClicked);
        }
    }
}
