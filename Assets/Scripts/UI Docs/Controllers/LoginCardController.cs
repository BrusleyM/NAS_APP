using UnityEngine;
using UnityEngine.UIElements;
using System;
using UnityEngine.UIElements.Experimental;

namespace NAS.UI.Controllers
{
    public class LoginCardController : MonoBehaviour
    {
        [SerializeField] private TextField _emailField;
        [SerializeField] private TextField _passwordField;
        [SerializeField] private Button _loginButton;
        [SerializeField] private Label _richTextLabel;

        public event Action<string, string> OnLogin;   // email, password
        public event Action OnRegister;                // switch to register card

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;
            
            // Find UI elements (or use serialized references)
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
            OnLogin?.Invoke(_emailField?.value, _passwordField?.value);
        }

        private void OnLinkClicked(PointerUpLinkTagEvent evt)
        {
            if (evt.linkID == "register")
                OnRegister?.Invoke();
        }

        private void OnDisable()
        {
            _loginButton.clicked -= OnLoginClicked;
            if (_richTextLabel != null)
                _richTextLabel.UnregisterCallback<PointerUpLinkTagEvent>(OnLinkClicked);
        }
    }
}