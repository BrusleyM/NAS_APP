using UnityEngine;
using UnityEngine.UIElements;

namespace NAS.UI.Controllers
{
    public class ParentPageController : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset _loginCardUxml;
        [SerializeField] private VisualTreeAsset _registerCardUxml;
        [SerializeField] private VisualTreeAsset _estimatorCardUxml;
        [SerializeField] private string _backgroundImagePath = "Assets/Textures/UI/background.png";

        private UIDocument _uiDocument;
        private VisualElement _cardContainer;

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
                _uiDocument = gameObject.AddComponent<UIDocument>();

            // Load the parent UXML (assumed to be in Resources)
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

            _cardContainer.Clear();
            RemoveControllers();
            _loginCardUxml.CloneTree(_cardContainer);
            
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
            if (_estimatorCardUxml == null)
            {
                Debug.LogError("EstimatorCard UXML not assigned!");
                return;
            }

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
            
            var estimatorCtrl = GetComponent<EstimatorCardController>();
            if (estimatorCtrl != null) Destroy(estimatorCtrl);
        }

        // For now, just log. Later you can store the user object or call API.
        private void HandleLogin(string email, string password)
        {
            Debug.Log($"Login attempt: {email}");
            // TODO: Store user session? For now, just switch to estimator.
            ShowEstimatorCard();
        }

        private void HandleRegister(string email, string password, string confirmPassword)
        {
            Debug.Log($"Register attempt: {email}");
            // After register, you might auto-login and show estimator.
            ShowEstimatorCard();
        }

        private void OnDisable()
        {
            RemoveControllers();
        }
    }
}