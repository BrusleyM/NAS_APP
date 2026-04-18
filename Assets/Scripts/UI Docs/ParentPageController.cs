using UnityEngine;
using UnityEngine.UIElements;

public class ParentPageController : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset _loginCardUxml;
    [SerializeField] private VisualTreeAsset _registerCardUxml;
    [SerializeField] private VisualTreeAsset _estimatorCardUxml;
    [SerializeField] private string _backgroundImagePath = "Assets/Textures/UI/image.png";

    private UIDocument _uiDocument;
    private VisualElement _cardContainer;

    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
            _uiDocument = gameObject.AddComponent<UIDocument>();

        /*var parentUxml = Resources.Load<VisualTreeAsset>("ParentPage");
        if (parentUxml == null)
        {
            Debug.LogError("ParentPage UXML not found in Resources!");
            return;
        }
        parentUxml.CloneTree(_uiDocument.rootVisualElement);*/

        var root = _uiDocument.rootVisualElement;
        _cardContainer = root.Q<VisualElement>("center-container");
        
        SetBackgroundImage(root);
        ShowLoginCard();
    }

    private void SetBackgroundImage(VisualElement root)
    {
        var backgroundImage = root.Q<Image>("background-image");
        if (backgroundImage != null && !string.IsNullOrEmpty(_backgroundImagePath))
        {
            var texture = Resources.Load<Texture2D>(_backgroundImagePath);
            if (texture != null)
                backgroundImage.image = texture;
            else
                Debug.LogWarning($"Background image not found at: {_backgroundImagePath}");
        }
    }

    public void ShowLoginCard()
    {
        if (_loginCardUxml == null)
        {
            Debug.LogError("LoginCard UXML not assigned!");
            return;
        }

        // Clean up previous card and controllers
        _cardContainer.Clear();
        RemoveControllers();

        // Instantiate new login card
        _loginCardUxml.CloneTree(_cardContainer);
        
        // Add a fresh controller and subscribe to its events
        var loginController = gameObject.AddComponent<LoginCardController>();
        loginController.OnLogin += HandleLogin;
        loginController.OnRegister += ShowRegisterCard;
    }

    public void ShowRegisterCard()
    {
        if (_registerCardUxml == null)
        {
            Debug.LogError("RegisterCard UXML not assigned!");
            return;
        }

        _cardContainer.Clear();
        RemoveControllers();

        _registerCardUxml.CloneTree(_cardContainer);
        
        var registerController = gameObject.AddComponent<RegisterCardController>();
        registerController.OnRegister += HandleRegister;
        registerController.OnLoginLink += ShowLoginCard;
    }

    public void ShowEstimatorCard()
    {
        _cardContainer.Clear();
        RemoveControllers();
        _estimatorCardUxml.CloneTree(_cardContainer);
        gameObject.AddComponent<EstimatorCardController>();
    }
    private void RemoveControllers()
    {
        var loginCtrl = GetComponent<LoginCardController>();
        if (loginCtrl != null) Destroy(loginCtrl);
        
        var registerCtrl = GetComponent<RegisterCardController>();
        if (registerCtrl != null) Destroy(registerCtrl);
    }

    private void HandleLogin(string email, string password)
    {
        Debug.Log($"Login attempted: {email}");
        // Add your authentication logic here
        ShowEstimatorCard();
    }

    private void HandleRegister(string email, string password, string confirmPassword)
    {
        Debug.Log($"Register attempted: {email}");
        // Add your registration logic here
    }

    private void OnDisable()
    {
        RemoveControllers();
    }
}