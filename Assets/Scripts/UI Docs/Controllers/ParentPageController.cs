using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using NAS.Core;
using NAS.Core.Events;
using NAS.Core.Networking;

namespace NAS.UI.Controllers
{
    /// <summary>
    /// Owns screen navigation ONLY. It reacts to domain events to decide which
    /// card to show next — it has no idea how login, registration, or auth
    /// validation actually work, and it no longer manually wires callbacks
    /// onto whichever card it just instantiated. Any future system (analytics,
    /// GameManager, a tutorial overlay, etc.) can react to the same events
    /// independently, without ParentPageController knowing they exist.
    /// </summary>
    public class ParentPageController : MonoBehaviour
    {
        private const string ArSceneName = "AR Scene";

        [SerializeField] private VisualTreeAsset _loginCardUxml;
        [SerializeField] private VisualTreeAsset _registerCardUxml;
        [SerializeField] private VisualTreeAsset _carSelectionCardUxml;
        [SerializeField] private VisualTreeAsset _estimatorCardUxml;
        [SerializeField] private VisualTreeAsset _carCardUxml;
        [SerializeField] private string _backgroundImagePath = "Assets/Textures/UI/background.png";

        [Tooltip("Passed to CarSelectionScreenController.Initialize() - same assets AuthController uses. Assign here, not on CarSelectionScreenController itself, since that controller is never pre-placed in the scene (added dynamically via AddComponent) and so has no Inspector of its own to assign these in.")]
        [SerializeField] private ApiSettings _apiSettings;
        [SerializeField] private ApiSettings _apiDomainSettings;

        private UIDocument _uiDocument;
        private VisualElement _cardContainer;

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
                _uiDocument = gameObject.AddComponent<UIDocument>();

            var root = _uiDocument.rootVisualElement;
            _cardContainer = root.Q<VisualElement>("center-container");

            SetBackgroundImage(root);
            SubscribeToFlowEvents();

            DecideInitialScreen();
        }

        private void DecideInitialScreen()
        {
            var session = GameManager.Instance;

            if (session.CurrentUser != null && session.SelectedCar != null)
            {
                if (session.ReturnToEstimator)
                {
                    ShowEstimatorCard();
                    session.ReturnToEstimator = false;
                }
                else
                {
                    ShowCarSelectionScreen();
                }
            }
            else
            {
                ShowLoginCard();
            }
        }

        private void SubscribeToFlowEvents()
        {
            EventBus.Subscribe<AuthSucceededEvent>(OnAuthSucceeded);
            EventBus.Subscribe<NavigateToRegisterRequestedEvent>(OnNavigateToRegister);
            EventBus.Subscribe<NavigateToLoginRequestedEvent>(OnNavigateToLogin);
            EventBus.Subscribe<CarSelectedEvent>(OnCarSelected);
        }

        private void UnsubscribeFromFlowEvents()
        {
            EventBus.Unsubscribe<AuthSucceededEvent>(OnAuthSucceeded);
            EventBus.Unsubscribe<NavigateToRegisterRequestedEvent>(OnNavigateToRegister);
            EventBus.Unsubscribe<NavigateToLoginRequestedEvent>(OnNavigateToLogin);
            EventBus.Unsubscribe<CarSelectedEvent>(OnCarSelected);
        }

        // NOTE on ordering: GameManager also subscribes to AuthSucceededEvent/CarSelectedEvent
        // to update CurrentUser/SelectedCar. Because GameManager is a persistent singleton
        // created before this screen exists, it subscribes first, so by the time these
        // handlers run here, GameManager's session state is already up to date. If you ever
        // reorder initialization, don't rely on that — read data straight off the event
        // payload instead of back through GameManager.
        private void OnAuthSucceeded(AuthSucceededEvent evt) => ShowCarSelectionScreen();
        private void OnNavigateToRegister(NavigateToRegisterRequestedEvent evt) => ShowRegisterCard();
        private void OnNavigateToLogin(NavigateToLoginRequestedEvent evt) => ShowLoginCard();
        // Loads the AR scene rather than showing a card here - unlike the other
        // screens, the AR viewport is a real separate Unity scene (AR Scene.unity,
        // with its own AR Foundation session/XR Origin), not another UI Toolkit
        // card swapped into _cardContainer. ArViewportController (in that scene)
        // owns the Back/Confirm buttons that bring the user back to this scene -
        // see its comments for how DecideInitialScreen() picks the right card
        // to show on return.
        private void OnCarSelected(CarSelectedEvent evt) => SceneManager.LoadScene(ArSceneName);

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
            if (_loginCardUxml == null) return;
            _cardContainer.Clear();
            RemoveCardControllers();
            _loginCardUxml.CloneTree(_cardContainer);
            gameObject.AddComponent<LoginCardController>();
        }

        public void ShowRegisterCard()
        {
            if (_registerCardUxml == null) return;
            _cardContainer.Clear();
            RemoveCardControllers();
            _registerCardUxml.CloneTree(_cardContainer);
            gameObject.AddComponent<RegisterCardController>();
        }

        public void ShowCarSelectionScreen()
        {
            if (_carSelectionCardUxml == null) return;
            _cardContainer.Clear();
            RemoveCardControllers();
            _carSelectionCardUxml.CloneTree(_cardContainer);
            var carSelectionCtrl = gameObject.AddComponent<CarSelectionScreenController>();
            carSelectionCtrl.Initialize(_carCardUxml, _apiSettings, _apiDomainSettings);
        }

        public void ShowEstimatorCard()
        {
            if (_estimatorCardUxml == null) return;
            _cardContainer.Clear();
            RemoveCardControllers();
            _estimatorCardUxml.CloneTree(_cardContainer);
            gameObject.AddComponent<EstimatorCardController>();
        }

        private void RemoveCardControllers()
        {
            var loginCtrl = GetComponent<LoginCardController>();
            if (loginCtrl != null) Destroy(loginCtrl);

            var registerCtrl = GetComponent<RegisterCardController>();
            if (registerCtrl != null) Destroy(registerCtrl);

            var carSelectionCtrl = GetComponent<CarSelectionScreenController>();
            if (carSelectionCtrl != null) Destroy(carSelectionCtrl);

            var estimatorCtrl = GetComponent<EstimatorCardController>();
            if (estimatorCtrl != null) Destroy(estimatorCtrl);
        }

        private void OnDisable()
        {
            UnsubscribeFromFlowEvents();
            RemoveCardControllers();
        }
    }
}
