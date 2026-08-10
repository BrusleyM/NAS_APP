using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using NAS.Core.Models;
using NAS.Core.Events;
using NAS.UI.Components;

namespace NAS.UI.Controllers
{
    /// <summary>
    /// Screen controller for car catalog browsing. Delegates card paging to CarPager
    /// and publishes CarSelectedEvent when the user starts AR.
    /// </summary>
    public class CarSelectionScreenController : MonoBehaviour
    {
        private VisualTreeAsset _carCardTemplate;

        private Button _typeDropdownButton;
        private Label _selectedTypeLabel;
        private VisualElement _dropdownArrow;
        private VisualElement _dropdownMenu;
        private TextField _searchField;
        private EventCallback<ChangeEvent<string>> _onSearchChanged;
        private VisualElement _carsScrollView;
        private VisualElement _carsContainer;
        private Label _emptyStateLabel;
        private Button _startButton;
        private bool _isDragging;
        private float _dragStartX;
        private int _dragPointerId = -1;

        private List<CarData> _allCars;
        private List<CarData> _filteredCars;
        private CarPager _pager;
        private string _selectedType = "All Types";
        private string _searchQuery = "";
        private bool _isDropdownOpen;

        private readonly List<string> _carTypes = new List<string>
        {
            "All Types", "Sedan", "SUV", "Hatchback", "Van"
        };

        public void Initialize(VisualTreeAsset carCardTemplate)
        {
            _carCardTemplate = carCardTemplate;
            SetupUI();
        }

        private void OnEnable()
        {
            InitializeCarData();

            if (_carCardTemplate != null)
                SetupUI();
        }

        private void SetupUI()
        {
            if (_pager != null)
                return;

            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null || _carCardTemplate == null)
                return;

            var root = uiDocument.rootVisualElement;

            _typeDropdownButton = root.Q<Button>("type-dropdown-button");
            _selectedTypeLabel = root.Q<Label>("selected-type-label");
            _dropdownArrow = root.Q<VisualElement>("dropdown-arrow");
            _dropdownMenu = root.Q<VisualElement>("dropdown-menu");
            _searchField = root.Q<TextField>("search-field");
            _carsScrollView = root.Q<VisualElement>("cars-scroll-view");
            _carsContainer = root.Q<VisualElement>("cars-container");
            _emptyStateLabel = root.Q<Label>("empty-state-label");
            _startButton = root.Q<Button>("start-ar-button");

            _pager = new CarPager(_carsContainer, _carCardTemplate);

            PopulateDropdown();

            _typeDropdownButton.clicked += ToggleDropdown;
            _onSearchChanged = evt =>
            {
                _searchQuery = evt.newValue;
                UpdateFilteredCars();
            };
            _searchField.RegisterValueChangedCallback(_onSearchChanged);
            _startButton.clicked += OnStartARClicked;

            _carsScrollView.RegisterCallback<PointerDownEvent>(OnCarTrackPointerDown);
            _carsScrollView.RegisterCallback<PointerMoveEvent>(OnCarTrackPointerMove);
            _carsScrollView.RegisterCallback<PointerUpEvent>(OnCarTrackPointerUp);
            _carsScrollView.RegisterCallback<PointerCaptureOutEvent>(OnCarTrackPointerCaptureOut);

            UpdateFilteredCars();
        }

        private void InitializeCarData()
        {
            var loaded = Resources.LoadAll<CarData>("Cars");
            _allCars = new List<CarData>(loaded);

            if (_allCars.Count == 0)
            {
                Debug.LogWarning(
                    "CarSelectionScreenController: no CarData assets found under Resources/Cars. " +
                    "Create some via Assets > Create > NAS > Car Data.");
            }
        }

        private void PopulateDropdown()
        {
            if (_dropdownMenu == null)
                return;

            _dropdownMenu.Clear();
            for (int i = 0; i < _carTypes.Count; i++)
            {
                var type = _carTypes[i];
                var item = new Button();
                item.text = type;
                item.AddToClassList("dropdown-item");
                if (i == _carTypes.Count - 1)
                    item.AddToClassList("last-dropdown-item");
                if (type == _selectedType)
                    item.AddToClassList("selected");

                item.clicked += () =>
                {
                    _selectedType = type;
                    _selectedTypeLabel.text = type;
                    _isDropdownOpen = false;
                    _dropdownMenu.style.display = DisplayStyle.None;
                    _dropdownArrow.RemoveFromClassList("rotate");
                    UpdateFilteredCars();
                };
                _dropdownMenu.Add(item);
            }
        }

        private void ToggleDropdown()
        {
            _isDropdownOpen = !_isDropdownOpen;
            if (_isDropdownOpen)
                PositionDropdownMenu();
            _dropdownMenu.style.display = _isDropdownOpen ? DisplayStyle.Flex : DisplayStyle.None;
            if (_isDropdownOpen)
                _dropdownArrow.AddToClassList("rotate");
            else
                _dropdownArrow.RemoveFromClassList("rotate");
        }

        private void PositionDropdownMenu()
        {
            var parent = _dropdownMenu.parent;
            if (parent == null)
                return;

            var buttonBound = _typeDropdownButton.worldBound;
            var parentBound = parent.worldBound;

            _dropdownMenu.style.left = buttonBound.xMin - parentBound.xMin;
            _dropdownMenu.style.top = buttonBound.yMax - parentBound.yMin + 4f;
            _dropdownMenu.style.width = buttonBound.width;
        }

        private void UpdateFilteredCars()
        {
            if (_allCars == null)
                return;

            _filteredCars = _allCars.FindAll(car =>
                (_selectedType == "All Types" || car.type == _selectedType) &&
                (string.IsNullOrEmpty(_searchQuery) ||
                 car.carName.ToLower().Contains(_searchQuery.ToLower()) ||
                 car.category.ToLower().Contains(_searchQuery.ToLower()))
            );

            if (_pager != null)
                _pager.SetCars(_filteredCars, startIndex: 0);

            UpdateEmptyState();
        }

        private void UpdateEmptyState()
        {
            if (_emptyStateLabel == null || _carsScrollView == null || _startButton == null)
                return;

            bool hasResults = _filteredCars != null && _filteredCars.Count > 0;

            _emptyStateLabel.style.display = hasResults ? DisplayStyle.None : DisplayStyle.Flex;
            _carsScrollView.style.display = hasResults ? DisplayStyle.Flex : DisplayStyle.None;
            _startButton.SetEnabled(hasResults);
        }

        private void OnStartARClicked()
        {
            if (_pager == null || !_pager.HasCars)
                return;

            var selectedCar = _pager.SelectedCar;
            var vehicle = new VehicleInfo
            {
                modelName = selectedCar.carName,
                retailPrice = selectedCar.retailPrice
            };
            EventBus.Publish(new CarSelectedEvent(vehicle));
        }

        private void OnDisable()
        {
            if (_typeDropdownButton != null)
                _typeDropdownButton.clicked -= ToggleDropdown;
            if (_searchField != null && _onSearchChanged != null)
                _searchField.UnregisterValueChangedCallback(_onSearchChanged);
            if (_startButton != null)
                _startButton.clicked -= OnStartARClicked;

            if (_carsScrollView != null)
            {
                _carsScrollView.UnregisterCallback<PointerDownEvent>(OnCarTrackPointerDown);
                _carsScrollView.UnregisterCallback<PointerMoveEvent>(OnCarTrackPointerMove);
                _carsScrollView.UnregisterCallback<PointerUpEvent>(OnCarTrackPointerUp);
                _carsScrollView.UnregisterCallback<PointerCaptureOutEvent>(OnCarTrackPointerCaptureOut);
            }

            _pager = null;
        }
    

private void OnCarTrackPointerDown(PointerDownEvent evt)
        {
            if (_pager == null || !_pager.HasCars)
                return;

            _isDragging = true;
            _dragPointerId = evt.pointerId;
            _dragStartX = evt.position.x;
            _carsScrollView.CapturePointer(evt.pointerId);
        }

        private void OnCarTrackPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || evt.pointerId != _dragPointerId)
                return;

            float deltaX = evt.position.x - _dragStartX;

            // Clamp: don't let the drag reveal a neighbor that doesn't exist.
            if (deltaX > 0 && !_pager.CanGoPrevious)
                deltaX = 0;
            else if (deltaX < 0 && !_pager.CanGoNext)
                deltaX = 0;

            _carsContainer.style.translate = new Translate(deltaX, 0);
        }

        private void OnCarTrackPointerUp(PointerUpEvent evt)
        {
            if (!_isDragging || evt.pointerId != _dragPointerId)
                return;

            _carsScrollView.ReleasePointer(evt.pointerId);
            EndDrag(evt.position.x - _dragStartX);
        }

        private void OnCarTrackPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (!_isDragging)
                return;

            EndDrag(0f);
        }

        private void EndDrag(float totalDeltaX)
        {
            _isDragging = false;
            _dragPointerId = -1;

            float viewportWidth = _carsScrollView.resolvedStyle.width;
            float threshold = viewportWidth * 0.25f;

            if (totalDeltaX > threshold && _pager.CanGoPrevious)
                _pager.Previous();
            else if (totalDeltaX < -threshold && _pager.CanGoNext)
                _pager.Next();

            // Instant swap: no easing. RefreshCards() has already rebound content
            // around the (possibly new) center slot, so rest position is always
            // translate 0 — nothing to compute or ease toward.
            _carsContainer.style.translate = new Translate(0, 0);
        }
}
}
