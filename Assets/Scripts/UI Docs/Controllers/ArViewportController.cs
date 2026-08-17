using NAS.Core;
using NAS.Core.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace NAS.UI.Controllers
{
    /// <summary>
    /// Top bar for the AR viewport scene (AR Scene.unity). Pre-placed on a
    /// scene GameObject with its own UIDocument - unlike the Main-App-scene
    /// card controllers, this isn't added dynamically via AddComponent, since
    /// this scene only ever shows one screen and has no router.
    /// </summary>
    public class ArViewportController : MonoBehaviour
    {
        private const string MainAppSceneName = "Main App";

        private Button _backButton;
        private Button _confirmButton;
        private Label _carNameLabel;

        // Settings button + "Customize" sheet are a placeholder for now (open/close
        // only, no Wheel/Paint/Trims/Dashboard grid yet) - see .claude/CLAUDE.md's
        // AR viewport section for why that grid is deliberately deferred.
        private Button _settingsButton;
        private Button _sheetCloseButton;
        private VisualElement _sheetBackdrop;
        private VisualElement _customizeSheet;

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
                return;

            // Unity doesn't guarantee OnEnable order across components on the same
            // GameObject - UIDocument's own OnEnable (which builds rootVisualElement)
            // may not have run yet when this one fires. Same class of gotcha as the
            // documented AddComponent<T>()/OnEnable ordering issue elsewhere in this
            // project - poll for a frame instead of assuming it's ready synchronously.
            var root = uiDocument.rootVisualElement;
            if (root == null)
            {
                StartCoroutine(WaitForRootThenBind(uiDocument));
                return;
            }

            BindUi(root);
        }

        private System.Collections.IEnumerator WaitForRootThenBind(UIDocument uiDocument)
        {
            while (uiDocument.rootVisualElement == null)
                yield return null;

            BindUi(uiDocument.rootVisualElement);
        }

        private void BindUi(VisualElement root)
        {
            _backButton = root.Q<Button>("back-button");
            _confirmButton = root.Q<Button>("confirm-button");
            _carNameLabel = root.Q<Label>("car-name-label");
            _settingsButton = root.Q<Button>("settings-button");
            _sheetCloseButton = root.Q<Button>("sheet-close-button");
            _sheetBackdrop = root.Q<VisualElement>("sheet-backdrop");
            _customizeSheet = root.Q<VisualElement>("customize-sheet");

            var selectedCar = GameManager.Instance != null ? GameManager.Instance.SelectedCar : null;
            if (_carNameLabel != null)
                _carNameLabel.text = selectedCar != null ? selectedCar.modelName : string.Empty;

            if (_backButton != null)
                _backButton.clicked += OnBackClicked;
            if (_confirmButton != null)
                _confirmButton.clicked += OnConfirmClicked;
            if (_settingsButton != null)
                _settingsButton.clicked += OnSettingsClicked;
            if (_sheetCloseButton != null)
                _sheetCloseButton.clicked += OnCloseSheetClicked;
            if (_sheetBackdrop != null)
                _sheetBackdrop.RegisterCallback<ClickEvent>(OnBackdropClicked);
        }

        private void OnDisable()
        {
            if (_backButton != null)
                _backButton.clicked -= OnBackClicked;
            if (_confirmButton != null)
                _confirmButton.clicked -= OnConfirmClicked;
            if (_settingsButton != null)
                _settingsButton.clicked -= OnSettingsClicked;
            if (_sheetCloseButton != null)
                _sheetCloseButton.clicked -= OnCloseSheetClicked;
            if (_sheetBackdrop != null)
                _sheetBackdrop.UnregisterCallback<ClickEvent>(OnBackdropClicked);
        }

        private void OnSettingsClicked() => SetCustomizeSheetOpen(true);
        private void OnCloseSheetClicked() => SetCustomizeSheetOpen(false);
        private void OnBackdropClicked(ClickEvent evt) => SetCustomizeSheetOpen(false);

        private void SetCustomizeSheetOpen(bool open)
        {
            var display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (_sheetBackdrop != null)
                _sheetBackdrop.style.display = display;
            if (_customizeSheet != null)
                _customizeSheet.style.display = display;
        }

        // Leaves GameManager.ReturnToEstimator at its default (false). Back in
        // the Main App scene, ParentPageController.DecideInitialScreen() sees
        // CurrentUser+SelectedCar already set (GameManager is a
        // DontDestroyOnLoad singleton, survives the scene load) and
        // ReturnToEstimator false, so it reopens car selection - restored to
        // the previously selected car via CarSelectionScreenController's
        // index-restore logic.
        private void OnBackClicked() => SceneManager.LoadScene(MainAppSceneName);

        // ReturnToEstimatorRequestedEvent is exactly what GameManager already
        // listens for to set ReturnToEstimator = true, which
        // DecideInitialScreen() checks to show the estimator card instead of
        // car selection on the next scene load - reusing that existing
        // "return from AR" contract rather than adding a new event.
        private void OnConfirmClicked()
        {
            EventBus.Publish(new ReturnToEstimatorRequestedEvent());
            SceneManager.LoadScene(MainAppSceneName);
        }
    }
}
