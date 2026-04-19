using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using NAS.Core.Models;
using NAS.Core;

namespace NAS.UI.Controllers
{
    public class CarSelectionScreenController : MonoBehaviour
    {
        // UI elements
        private Button _typeDropdownButton;
        private Label _selectedTypeLabel;
        private VisualElement _dropdownArrow;
        private VisualElement _dropdownMenu;
        private TextField _searchField;
        private ScrollView _carsScrollView;
        private VisualElement _carsContainer;
        private Button _startButton;

        // Data
        private List<CarData> _allCars;
        private List<CarData> _filteredCars;
        private int _selectedCarIndex = 0;
        private string _selectedType = "All Types";
        private string _searchQuery = "";
        private bool _isDropdownOpen = false;

        private readonly List<string> _carTypes = new List<string> { "All Types", "Sedan", "SUV", "Hatchback", "Van" };

        // Event raised when a car is selected (for estimator flow)
        public event Action<VehicleInfo> OnCarSelectedForEstimation;

        [System.Serializable]
        private class CarData
        {
            public string name;
            public string category;
            public string type;
            public string imageUrl; // Resource path or URL
        }

        private void OnEnable()
        {
            InitializeCarData();

            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;

            // Find UI elements
            _typeDropdownButton = root.Q<Button>("type-dropdown-button");
            _selectedTypeLabel = root.Q<Label>("selected-type-label");
            _dropdownArrow = root.Q<VisualElement>("dropdown-arrow");
            _dropdownMenu = root.Q<VisualElement>("dropdown-menu");
            _searchField = root.Q<TextField>("search-field");
            _carsScrollView = root.Q<ScrollView>("cars-scroll-view");
            _carsContainer = root.Q<VisualElement>("cars-container");
            _startButton = root.Q<Button>("start-ar-button");

            // Populate dropdown
            PopulateDropdown();

            // Register events
            _typeDropdownButton.clicked += ToggleDropdown;
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _searchQuery = evt.newValue;
                UpdateFilteredCars();
            });
            _startButton.clicked += OnStartARClicked;

            // Initial load
            UpdateFilteredCars();
        }

        private void InitializeCarData()
        {
            _allCars = new List<CarData>
            {
                new CarData { name = "Tesla Model S", category = "Electric Sedan", type = "Sedan", imageUrl = "cars/tesla_model_s" },
                new CarData { name = "BMW M5", category = "Sport Sedan", type = "Sedan", imageUrl = "cars/bmw_m5" },
                new CarData { name = "Mercedes-Benz E-Class", category = "Luxury Sedan", type = "Sedan", imageUrl = "cars/mercedes_eclass" },
                new CarData { name = "Range Rover Sport", category = "Luxury SUV", type = "SUV", imageUrl = "cars/range_rover" },
                new CarData { name = "Honda CR-V", category = "Compact SUV", type = "SUV", imageUrl = "cars/honda_crv" },
                new CarData { name = "Volkswagen Golf", category = "Compact Hatchback", type = "Hatchback", imageUrl = "cars/vw_golf" },
                new CarData { name = "Mercedes-Benz Sprinter", category = "Cargo Van", type = "Van", imageUrl = "cars/sprinter" }
            };
        }

        private void PopulateDropdown()
        {
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
                item.clicked += () => {
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
            _dropdownMenu.style.display = _isDropdownOpen ? DisplayStyle.Flex : DisplayStyle.None;
            if (_isDropdownOpen)
                _dropdownArrow.AddToClassList("rotate");
            else
                _dropdownArrow.RemoveFromClassList("rotate");
        }

        private void UpdateFilteredCars()
        {
            _filteredCars = _allCars.FindAll(car =>
                (_selectedType == "All Types" || car.type == _selectedType) &&
                (string.IsNullOrEmpty(_searchQuery) ||
                 car.name.ToLower().Contains(_searchQuery.ToLower()) ||
                 car.category.ToLower().Contains(_searchQuery.ToLower()))
            );
            if (_filteredCars.Count == 0)
                _filteredCars = _allCars; // fallback

            _selectedCarIndex = 0;
            BuildCarCards();
        }

        private void BuildCarCards()
        {
            _carsContainer.Clear();
            for (int i = 0; i < _filteredCars.Count; i++)
            {
                var car = _filteredCars[i];
                var card = new VisualElement();
                card.AddToClassList("car-card");
                // Optional: add selected style if needed
                if (i == _selectedCarIndex)
                    card.AddToClassList("selected-car-card");

                var image = new Image();
                image.AddToClassList("car-image");
                // Load image from Resources or keep placeholder
                // var texture = Resources.Load<Texture2D>(car.imageUrl);
                // if (texture != null) image.image = texture;
                card.Add(image);

                var nameLabel = new Label(car.name);
                nameLabel.AddToClassList("car-name");
                card.Add(nameLabel);

                var categoryLabel = new Label(car.category);
                categoryLabel.AddToClassList("car-category");
                card.Add(categoryLabel);

                // Click handler to select this car
                int index = i; // capture for lambda
                card.RegisterCallback<ClickEvent>(evt => {
                    _selectedCarIndex = index;
                    // Update visual selection (remove from all, add to selected)
                    for (int j = 0; j < _carsContainer.childCount; j++)
                    {
                        _carsContainer[j].RemoveFromClassList("selected-car-card");
                    }
                    card.AddToClassList("selected-car-card");
                });

                _carsContainer.Add(card);
            }
        }

        private void OnStartARClicked()
        {
            if (_filteredCars == null || _filteredCars.Count == 0) return;
            var selectedCar = _filteredCars[_selectedCarIndex];
            var vehicle = new VehicleInfo
            {
                modelName = selectedCar.name,
                retailPrice = GetPriceForCar(selectedCar.name)
            };
            OnCarSelectedForEstimation?.Invoke(vehicle);
        }

        private float GetPriceForCar(string carName)
        {
            // Temporary price mapping – replace with real data
            switch (carName)
            {
                case "Tesla Model S": return 75000f;
                case "BMW M5": return 95000f;
                case "Mercedes-Benz E-Class": return 85000f;
                case "Range Rover Sport": return 80000f;
                case "Honda CR-V": return 35000f;
                case "Volkswagen Golf": return 28000f;
                case "Mercedes-Benz Sprinter": return 50000f;
                default: return 50000f;
            }
        }

        private void OnDisable()
        {
            if (_typeDropdownButton != null) _typeDropdownButton.clicked -= ToggleDropdown;
            if (_searchField != null) _searchField.UnregisterValueChangedCallback(null);
            if (_startButton != null) _startButton.clicked -= OnStartARClicked;
        }
    }
}