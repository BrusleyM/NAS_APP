using UnityEngine;
using UnityEngine.UIElements;
using System;
using UnityEngine.UIElements.Experimental;

public class LoginCardController : MonoBehaviour
{
    private TextField _emailField;
    private TextField _passwordField;
    private Button _loginButton;
    private Label _richTextLabel;

    public event Action<string, string> OnLogin;  // email, password
    public event Action OnRegister;

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        _emailField = root.Q<TextField>("email-field");
        _passwordField = root.Q<TextField>("password-field");
        _loginButton = root.Q<Button>("login-button");
        _richTextLabel = root.Q<Label>("register-link-label");

        // Login button event
        _loginButton.clicked += () => OnLogin?.Invoke(_emailField?.value, _passwordField?.value);

        // ✅ Register link click events
        _richTextLabel.RegisterCallback<PointerUpLinkTagEvent>(OnLinkClicked);
        _richTextLabel.RegisterCallback<PointerDownLinkTagEvent>(OnLinkPressed);
    }

    private void OnLinkClicked(PointerUpLinkTagEvent evt)
    {
        // evt.linkID contains the ID you set in the <link> tag
        if (evt.linkID == "register")
        {
            Debug.Log("Register link clicked");
            OnRegister?.Invoke();
        }
    }

    private void OnLinkPressed(PointerDownLinkTagEvent evt)
    {
        // Optional: handle visual feedback on press
        Debug.Log($"Link pressed: {evt.linkID}");
    }

    private void OnDisable()
    {
        //if (loginButton != null) loginButton.clicked = null;
        if (_richTextLabel != null)
        {
            _richTextLabel.UnregisterCallback<PointerUpLinkTagEvent>(OnLinkClicked);
            _richTextLabel.UnregisterCallback<PointerDownLinkTagEvent>(OnLinkPressed);
        }
    }
}