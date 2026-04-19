using UnityEngine;
using UnityEngine.UIElements;
using System;
using UnityEngine.UIElements.Experimental;

namespace NAS.UI.Controllers
{
    public class RegisterCardController : MonoBehaviour
    {
        [SerializeField] private TextField _emailField;
        [SerializeField] private TextField _passwordField;
        [SerializeField] private TextField _confirmPasswordField;
        [SerializeField] private Button _registerButton;
        [SerializeField] private Label _richTextLabel;

        public event Action<string, string, string> OnRegister;   // email, password, confirm
        public event Action OnLoginLink;                           // switch to login card

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;
            
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

        void OnRegisterClicked()
        {
            OnRegister?.Invoke(
                _emailField?.value,
                _passwordField?.value,
                _confirmPasswordField?.value
            );
        }

        private void OnLinkClicked(PointerUpLinkTagEvent evt)
        {
            if (evt.linkID == "login")
                OnLoginLink?.Invoke();
        }

        private void OnDisable()
        {
            _registerButton.clicked -= OnRegisterClicked;
            if (_richTextLabel != null)
                _richTextLabel.UnregisterCallback<PointerUpLinkTagEvent>(OnLinkClicked);
        }
    }
}