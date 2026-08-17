using System.Collections.Generic;
using NAS.Core;
using NAS.Core.Events;
using NAS.Core.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace NAS.UI.Controllers
{
    /// <summary>
    /// Top bar + Customize sheet for the AR viewport scene (AR Scene.unity).
    /// Pre-placed on a scene GameObject with its own UIDocument - unlike the
    /// Main-App-scene card controllers, this isn't added dynamically via
    /// AddComponent, since this scene only ever shows one screen and has no
    /// router.
    /// </summary>
    public class ArViewportController : MonoBehaviour
    {
        private const string PaintCategoryId = "paint";

        private static readonly string[] CategoryIds = { "wheel", "paint", "trims", "dashboard" };
        private static readonly string[] CategoryButtonNames = { "category-wheel", "category-paint", "category-trims", "category-dashboard" };

        private Button _backButton;
        private Button _confirmButton;
        private Label _carNameLabel;

        private Button _settingsButton;
        private Button _sheetCloseButton;
        private VisualElement _sheetBackdrop;
        private VisualElement _customizeSheet;

        // Wheel/Trims/Dashboard have no data model to back real customization
        // yet (see .claude/CLAUDE.md's "Vehicle catalog" section) - tapping
        // them just shows the same "Coming soon" placeholder the whole sheet
        // used to show. Paint is real: backed by CarData.exteriorColors,
        // fetched from the customer vehicle API.
        private readonly Dictionary<string, Button> _categoryButtons = new Dictionary<string, Button>();
        private VisualElement _swatchRow;
        private Label _categoryPlaceholderText;
        private Button _selectedSwatch;
        private string _activeCategoryId;

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

        private VisualElement _root;

        private void BindUi(VisualElement root)
        {
            _root = root;
            _backButton = root.Q<Button>("back-button");
            _confirmButton = root.Q<Button>("confirm-button");
            _carNameLabel = root.Q<Label>("car-name-label");
            _settingsButton = root.Q<Button>("settings-button");
            _sheetCloseButton = root.Q<Button>("sheet-close-button");
            _sheetBackdrop = root.Q<VisualElement>("sheet-backdrop");
            _customizeSheet = root.Q<VisualElement>("customize-sheet");
            _swatchRow = root.Q<VisualElement>("swatch-row");
            _categoryPlaceholderText = root.Q<Label>("category-placeholder-text");

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

            for (var i = 0; i < CategoryIds.Length; i++)
            {
                var button = root.Q<Button>(CategoryButtonNames[i]);
                if (button == null) continue;
                var categoryId = CategoryIds[i];
                _categoryButtons[categoryId] = button;
                button.clicked += () => OnCategoryClicked(categoryId);
            }

            EventBus.Subscribe<EnterArRequestedEvent>(OnEnterAr);
            ShowForCurrentCar();
        }

        // AR Scene is loaded once and never reloaded (see ParentPageController.OnCarSelected) -
        // this used to be inline in BindUi() since that only ever ran once per
        // visit under the old reload-every-time design. Now it's the thing
        // that refreshes per-visit state (car name, a clean Customize sheet,
        // no stale swatch selection from whatever car was showing last time),
        // called both on the first bind above and on every EnterArRequestedEvent
        // after that.
        private void ShowForCurrentCar()
        {
            ShowUi();

            var selectedCar = GameManager.Instance != null ? GameManager.Instance.SelectedCar : null;
            if (_carNameLabel != null)
                _carNameLabel.text = selectedCar != null ? selectedCar.modelName : string.Empty;

            SetCustomizeSheetOpen(false);
            _activeCategoryId = null;
            foreach (var pair in _categoryButtons)
                pair.Value.RemoveFromClassList("category-button--active");
            _selectedSwatch = null;
            if (_swatchRow != null)
                _swatchRow.Clear();
            SetElementVisible(_categoryPlaceholderText, false);
        }

        private void OnEnterAr(EnterArRequestedEvent evt) => ShowForCurrentCar();

        private void ShowUi()
        {
            if (_root != null)
                _root.style.display = DisplayStyle.Flex;
        }

        private void HideUi()
        {
            if (_root != null)
                _root.style.display = DisplayStyle.None;
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnterArRequestedEvent>(OnEnterAr);

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

        private void OnCategoryClicked(string categoryId)
        {
            _activeCategoryId = categoryId;

            foreach (var pair in _categoryButtons)
                pair.Value.EnableInClassList("category-button--active", pair.Key == categoryId);

            if (categoryId == PaintCategoryId)
            {
                ShowPaintSwatches();
            }
            else
            {
                SetElementVisible(_swatchRow, false);
                SetElementVisible(_categoryPlaceholderText, true);
            }
        }

        private void ShowPaintSwatches()
        {
            SetElementVisible(_categoryPlaceholderText, false);

            var selectedCar = GameManager.Instance != null ? GameManager.Instance.SelectedCar : null;
            var colors = selectedCar != null ? selectedCar.exteriorColors : null;

            if (_swatchRow == null)
                return;

            _swatchRow.Clear();
            _selectedSwatch = null;

            if (colors == null || colors.Count == 0)
            {
                // No color data for this car - fall back to the same
                // placeholder the other categories use rather than showing an
                // empty row.
                SetElementVisible(_swatchRow, false);
                SetElementVisible(_categoryPlaceholderText, true);
                return;
            }

            SetElementVisible(_swatchRow, true);

            foreach (var option in colors)
            {
                var swatch = new Button { name = $"swatch-{option.id}" };
                swatch.AddToClassList("color-swatch");

                if (ColorUtility.TryParseHtmlString(option.hexCode, out var color))
                    swatch.style.backgroundColor = new StyleColor(color);

                swatch.clicked += () => OnSwatchClicked(swatch, option);
                _swatchRow.Add(swatch);
            }
        }

        private void OnSwatchClicked(Button swatch, CarColorOption option)
        {
            if (_selectedSwatch != null)
                _selectedSwatch.RemoveFromClassList("color-swatch--selected");

            swatch.AddToClassList("color-swatch--selected");
            _selectedSwatch = swatch;

            EventBus.Publish(new PaintColorSelectedEvent(option.hexCode));
        }

        private static void SetElementVisible(VisualElement element, bool visible)
        {
            if (element != null)
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Leaves GameManager.ReturnToEstimator at its default (false).
        // ParentPageController.OnExitAr() -> DecideInitialScreen() sees
        // CurrentUser+SelectedCar already set and ReturnToEstimator false, so
        // it reopens car selection - restored to the previously selected car
        // via CarSelectionScreenController's index-restore logic. Neither
        // scene reloads anymore (see GameEvents.cs's ExitArRequestedEvent doc
        // comment for why) - HideUi() just hides this screen, ParentPageController
        // shows its own UI back in response to the event.
        private void OnBackClicked()
        {
            HideUi();
            EventBus.Publish(new ExitArRequestedEvent());
        }

        // ReturnToEstimatorRequestedEvent is exactly what GameManager already
        // listens for to set ReturnToEstimator = true, which
        // DecideInitialScreen() checks to show the estimator card instead of
        // car selection - reusing that existing "return from AR" contract
        // rather than adding a new event for it.
        private void OnConfirmClicked()
        {
            EventBus.Publish(new ReturnToEstimatorRequestedEvent());
            HideUi();
            EventBus.Publish(new ExitArRequestedEvent());
        }
    }
}
