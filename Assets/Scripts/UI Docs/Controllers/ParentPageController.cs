using UnityEngine;
using UnityEngine.UIElements;
using System;
using NAS.Core;
using NAS.Core.Models;

namespace NAS.UI.Controllers
{
    public class ParentPageController : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset _loginCardUxml;
        [SerializeField] private VisualTreeAsset _registerCardUxml;
        [SerializeField] private VisualTreeAsset _carSelectionCardUxml;
        [SerializeField] private VisualTreeAsset _estimatorCardUxml;
        [SerializeField] private string _backgroundImagePath = "Assets/Textures/UI/background.png";

        private UIDocument _uiDocument;
        private VisualElement _cardContainer;
        private CarSelectionScreenController _carSelectionController;

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
                _uiDocument = gameObject.AddComponent<UIDocument>();

            /* var parentUxml = Resources.Load<VisualTreeAsset>("ParentPage");
             if (parentUxml == null)
             {
                 Debug.LogError("ParentPage UXML not found in Resources!");
                 return;
             }
             parentUxml.CloneTree(_uiDocument.rootVisualElement);*/

            var root = _uiDocument.rootVisualElement;
            _cardContainer = root.Q<VisualElement>("center-container");
            
            SetBackgroundImage(root);

            // Decide which screen to show first
            if (GameManager.Instance.CurrentUser != null && GameManager.Instance.SelectedCar != null)
            {
                // If returning from AR with estimator flag, show estimator
                if (GameManager.Instance.ReturnToEstimator)
                {
                    ShowEstimatorCard();
                    GameManager.Instance.ReturnToEstimator = false;
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
            RemoveControllers();
            _loginCardUxml.CloneTree(_cardContainer);
            var loginController = gameObject.AddComponent<LoginCardController>();
            loginController.OnLogin += HandleLogin;
            loginController.OnRegister += ShowRegisterCard;
        }

        public void ShowRegisterCard()
        {
            if (_registerCardUxml == null) return;
            _cardContainer.Clear();
            RemoveControllers();
            _registerCardUxml.CloneTree(_cardContainer);
            var registerController = gameObject.AddComponent<RegisterCardController>();
            registerController.OnRegister += HandleRegister;
            registerController.OnLoginLink += ShowLoginCard;
        }

        public void ShowCarSelectionScreen()
        {
            if (_carSelectionCardUxml == null) return;
            _cardContainer.Clear();
            RemoveControllers();
            _carSelectionCardUxml.CloneTree(_cardContainer);
            _carSelectionController = gameObject.AddComponent<CarSelectionScreenController>();
            _carSelectionController.OnCarSelectedForEstimation += HandleCarSelected;
        }

        public void ShowEstimatorCard()
        {
            if (_estimatorCardUxml == null) return;
            _cardContainer.Clear();
            RemoveControllers();
            _estimatorCardUxml.CloneTree(_cardContainer);
            gameObject.AddComponent<EstimatorCardController>();
        }

        private void HandleLogin(string email, string password)
        {
            // Temporary: create a dummy user
            GameManager.Instance.CurrentUser = new User { email = email, displayName = email };
            ShowCarSelectionScreen();
        }

        private void HandleRegister(string email, string password, string confirm)
        {
            GameManager.Instance.CurrentUser = new User { email = email, displayName = email };
            ShowCarSelectionScreen();
        }

        private void HandleCarSelected(VehicleInfo selectedCar)
        {
            GameManager.Instance.SelectedCar = selectedCar;
            ShowEstimatorCard();
        }

        private void RemoveControllers()
        {
            var loginCtrl = GetComponent<LoginCardController>();
            if (loginCtrl != null) Destroy(loginCtrl);
            
            var registerCtrl = GetComponent<RegisterCardController>();
            if (registerCtrl != null) Destroy(registerCtrl);
            
            var estimatorCtrl = GetComponent<EstimatorCardController>();
            if (estimatorCtrl != null) Destroy(estimatorCtrl);
            
            if (_carSelectionController != null)
            {
                _carSelectionController.OnCarSelectedForEstimation -= HandleCarSelected;
                Destroy(_carSelectionController);
            }
        }

        private void OnDisable()
        {
            RemoveControllers();
        }
    }
}