using UnityEngine;
using UnityEngine.UIElements;
using NAS.Core.Models;
using NAS.Core.Interfaces;
using NAS.Core.Services;
using NAS.Core;

namespace NAS.UI.Controllers
{
    public class EstimatorCardController : MonoBehaviour
    {
        [SerializeField] private Label _retailPriceLabel;
        [SerializeField] private TextField _depositField;
        [SerializeField] private TextField _tradeInField;
        [SerializeField] private TextField _loanTermField;
        [SerializeField] private Label _financedAmountLabel;
        [SerializeField] private Slider _interestSlider;
        [SerializeField] private Label _interestValueLabel;
        [SerializeField] private Slider _balloonSlider;
        [SerializeField] private Label _balloonPercentLabel;
        [SerializeField] private Label _balloonDollarLabel;
        [SerializeField] private Label _monthlyPaymentLabel;
        [SerializeField] private Label _balloonInfoLabel;
        [SerializeField] private Label _loanInterestInfoLabel;
        [SerializeField] private Button _sendButton;
        [SerializeField] private VisualElement _balloonContainer;

        private VehicleInfo _vehicle;
        private ILoanCalculator _loanCalculator;
        
        private float _deposit;
        private float _tradeIn;
        private int _loanTerm;
        private float _interestRate;
        private float _balloonPercent;

        private void OnEnable()
        {
            _loanCalculator = new LoanCalculator();
            
            // Use selected car from GameManager if available, otherwise fallback to default
            if (GameManager.Instance.SelectedCar != null)
            {
                _vehicle = GameManager.Instance.SelectedCar;
            }
            else
            {
                _vehicle = new VehicleInfo
                {
                    modelName = "Tesla Model S · Performance Trim",
                    retailPrice = 75000f
                };
            }
            
            _deposit = 0f;
            _tradeIn = 0f;
            _loanTerm = 48;
            _interestRate = 4.5f;
            _balloonPercent = 0f;

            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;
            
            if (_retailPriceLabel == null) _retailPriceLabel = root.Q<Label>("retail-price");
            if (_depositField == null) _depositField = root.Q<TextField>("deposit-field");
            if (_tradeInField == null) _tradeInField = root.Q<TextField>("tradein-field");
            if (_loanTermField == null) _loanTermField = root.Q<TextField>("loan-term-field");
            if (_financedAmountLabel == null) _financedAmountLabel = root.Q<Label>("financed-amount");
            if (_interestSlider == null) _interestSlider = root.Q<Slider>("interest-slider");
            if (_interestValueLabel == null) _interestValueLabel = root.Q<Label>("interest-rate-value");
            if (_balloonSlider == null) _balloonSlider = root.Q<Slider>("balloon-slider");
            if (_balloonPercentLabel == null) _balloonPercentLabel = root.Q<Label>("balloon-percent-value");
            if (_balloonDollarLabel == null) _balloonDollarLabel = root.Q<Label>("balloon-dollar-value");
            if (_monthlyPaymentLabel == null) _monthlyPaymentLabel = root.Q<Label>("monthly-payment");
            if (_balloonInfoLabel == null) _balloonInfoLabel = root.Q<Label>("balloon-info");
            if (_loanInterestInfoLabel == null) _loanInterestInfoLabel = root.Q<Label>("loan_interest_info");
            if (_sendButton == null) _sendButton = root.Q<Button>("send-button");
            if (_balloonContainer == null) _balloonContainer = root.Q<VisualElement>("balloon-info-container");

            // Display retail price using the selected car's price
            _retailPriceLabel.text = $"R{_vehicle.retailPrice:N0}";
            
            _depositField.value = $"{_deposit}";
            _tradeInField.value = $"{_tradeIn}";
            _loanTermField.value = $"{_loanTerm}";
            _interestSlider.value = _interestRate;
            _balloonSlider.value = _balloonPercent;
            
            _depositField.RegisterValueChangedCallback(evt =>
            {
                var sanitized = SanitizeFloatInput(evt.newValue);
                if (sanitized != evt.newValue)
                    _depositField.SetValueWithoutNotify(sanitized);
                if (float.TryParse(sanitized, out var value))
                {
                    _deposit = ValidateDeposit(value);
                    UpdateAll();
                }
            });
            _tradeInField.RegisterValueChangedCallback(evt =>
            {
                var sanitized = SanitizeFloatInput(evt.newValue);
                if (sanitized != evt.newValue)
                    _tradeInField.SetValueWithoutNotify(sanitized);
                if (float.TryParse(sanitized, out var value))
                {
                    _tradeIn = ValidateTradeIn(value);
                    UpdateAll();
                }
            });
            _loanTermField.RegisterValueChangedCallback(evt =>
            {
                var sanitized = SanitizeIntInput(evt.newValue);
                if (sanitized != evt.newValue)
                    _loanTermField.SetValueWithoutNotify(sanitized);
                if (int.TryParse(sanitized, out var value))
                {
                    _loanTerm = ValidateLoanTerm(value);
                    UpdateAll();
                }
            });
            _interestSlider.RegisterValueChangedCallback(evt => 
            { 
                _interestRate = ValidateInterestRate(evt.newValue); 
                if (Mathf.Abs(_interestSlider.value - _interestRate) > 0.01f) 
                    _interestSlider.SetValueWithoutNotify(_interestRate);
                UpdateAll(); 
            });
            _balloonSlider.RegisterValueChangedCallback(evt => 
            { 
                _balloonPercent = ValidateBalloonPercent(evt.newValue); 
                if (Mathf.Abs(_balloonSlider.value - _balloonPercent) > 0.01f) 
                    _balloonSlider.SetValueWithoutNotify(_balloonPercent);
                UpdateAll(); 
            });
            
            _sendButton.clicked += OnSendToDealer;
            UpdateAll();
        }

        private float ValidateDeposit(float value) => Mathf.Clamp(value, 0, _vehicle.retailPrice);
        private float ValidateTradeIn(float value) => Mathf.Clamp(value, 0, _vehicle.retailPrice);
        private int ValidateLoanTerm(int value) => Mathf.Clamp(value, 1, 120);
        private float ValidateInterestRate(float value) => Mathf.Clamp(value, 0f, 30f);
        private float ValidateBalloonPercent(float value) => Mathf.Clamp(value, 0f, 50f);

        private string SanitizeFloatInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;
            bool hasDecimal = false;
            var result = new System.Text.StringBuilder();
            foreach (var c in input)
            {
                if (char.IsDigit(c))
                    result.Append(c);
                else if ((c == '.' || c == ',') && !hasDecimal)
                {
                    result.Append('.');
                    hasDecimal = true;
                }
            }
            return result.ToString();
        }

        private string SanitizeIntInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;
            var result = new System.Text.StringBuilder();
            foreach (var c in input)
            {
                if (char.IsDigit(c))
                    result.Append(c);
            }
            return result.ToString();
        }

        private void UpdateAll()
        {
            float financed = _vehicle.retailPrice - _deposit - _tradeIn;
            if (financed < 0) financed = 0;
            _financedAmountLabel.text = $"R{financed:N0}";
            
            float balloonAmount = financed * (_balloonPercent / 100f);
            _balloonPercentLabel.text = $"{_balloonPercent:F0}%";
            _balloonDollarLabel.text = $"({balloonAmount:C0})";
            _interestValueLabel.text = $"{_interestRate:F1}%";
            _loanInterestInfoLabel.text = $"* Estimated payment based on {_interestRate:F1}% interest. Actual rates may vary.";
            
            float monthly = _loanCalculator.CalculateMonthlyPayment(financed, balloonAmount, _loanTerm, _interestRate);
            _monthlyPaymentLabel.text = $"{monthly:C0}";
            
            if (_balloonPercent > 0)
            {
                _balloonInfoLabel.text = $"+ {balloonAmount:C0} balloon due at end";
                _balloonContainer.style.display = DisplayStyle.Flex;
            }
            else
            {
                _balloonContainer.style.display = DisplayStyle.None;
            }
        }

        private void OnSendToDealer()
        {
            Debug.Log($"Send to Dealer: Vehicle = {_vehicle.modelName}, Financed = {_financedAmountLabel.text}, Monthly = {_monthlyPaymentLabel.text}");
        }

        private void OnDisable()
        {
            if (_sendButton != null) _sendButton.clicked -= OnSendToDealer;
        }
    }
}