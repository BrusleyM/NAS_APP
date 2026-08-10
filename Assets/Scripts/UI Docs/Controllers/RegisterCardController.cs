using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using NAS.Core.Events;

namespace NAS.UI.Controllers
{
    /// <summary>
    /// Purely a view. Publishes what the user did — AuthController decides if
    /// it's valid, ParentPageController decides where to go next.
    /// </summary>
    public class RegisterCardController : MonoBehaviour
    {
                [SerializeField] private TextField _firstNameField;
        [SerializeField] private TextField _lastNameField;
        [SerializeField] private TextField _cellNumberField;
[SerializeField] private TextField _emailField;
        [SerializeField] private TextField _passwordField;
        [SerializeField] private TextField _confirmPasswordField;
        [SerializeField] private Button _registerButton;
        [SerializeField] private Label _richTextLabel;

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;

                        _firstNameField = root.Q<TextField>("first-name-field");
            _lastNameField = root.Q<TextField>("last-name-field");
            _cellNumberField = root.Q<TextField>("cell-number-field");
_emailField = root.Q<TextField>("email-field");
            _passwordField = root.Q<TextField>("password-field");
            _confirmPasswordField = root.Q<TextField>("confirm-password-field");
            _registerButton = root.Q<Button>("register-button");
            _richTextLabel = root.Q<Label>("login-link-label");

            if (_registerButton != null)
                _registerButton.clicked += OnRegisterClicked;

            if (_richTextLabel != null)
                _richTextLabel.RegisterCallback<PointerUpLinkTagEvent>(OnLinkClicked);
        }

private void OnRegisterClicked()
        {
            EventBus.Publish(new RegisterRequestedEvent(
                _firstNameField?.value,
                _lastNameField?.value,
                _cellNumberField?.value,
                _emailField?.value,
                _passwordField?.value,
                _confirmPasswordField?.value));
        }

        private void OnLinkClicked(PointerUpLinkTagEvent evt)
        {
            if (evt.linkID == "login")
                EventBus.Publish(new NavigateToLoginRequestedEvent());
        }

        private void OnDisable()
        {
            if (_registerButton != null) _registerButton.clicked -= OnRegisterClicked;
            if (_richTextLabel != null)
                _richTextLabel.UnregisterCallback<PointerUpLinkTagEvent>(OnLinkClicked);
        }
    }
}
