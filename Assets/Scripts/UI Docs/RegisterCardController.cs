using UnityEngine;
using UnityEngine.UIElements;
using System;
using UnityEngine.UIElements.Experimental;

public class RegisterCardController : MonoBehaviour
{
    private TextField emailField;
    private TextField passwordField;
    private TextField confirmPasswordField;
    private Button registerButton;
    private Label richTextLabel;

    public event Action<string, string, string> OnRegister; // email, password, confirmPassword
    public event Action OnLoginLink;

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        emailField = root.Q<TextField>("email-field");
        passwordField = root.Q<TextField>("password-field");
        confirmPasswordField = root.Q<TextField>("confirm-password-field");
        registerButton = root.Q<Button>("register-button");
        richTextLabel = root.Q<Label>("login-link-label");

        registerButton.clicked += () => OnRegister?.Invoke(
            emailField?.value,
            passwordField?.value,
            confirmPasswordField?.value
        );

        richTextLabel.RegisterCallback<PointerUpLinkTagEvent>(OnLinkClicked);
    }

    private void OnLinkClicked(PointerUpLinkTagEvent evt)
    {
        if (evt.linkID == "login")
            OnLoginLink?.Invoke();
    }

    private void OnDisable()
    {
        //if (registerButton != null) registerButton.clicked = null;
        if (richTextLabel != null)
            richTextLabel.UnregisterCallback<PointerUpLinkTagEvent>(OnLinkClicked);
    }
}