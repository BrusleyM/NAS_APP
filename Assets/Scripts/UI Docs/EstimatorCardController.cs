using UnityEngine;
using UnityEngine.UIElements;
using System;

public class EstimatorCardController : MonoBehaviour
{
    // UI elements
    private Label _retailPriceLabel;
    private FloatField _depositField;
    private FloatField _tradeInField;
    private IntegerField _loanTermField;
    private Label _financedAmountLabel;
    private Slider _interestSlider;
    private Label _interestValueLabel;
    private Slider _balloonSlider;
    private Label _balloonPercentLabel;
    private Label _balloonDollarLabel;
    private Label _monthlyPaymentLabel;
    private Label _balloonInfoLabel;
    private Button _sendButton;

    // Data
    private const float CarPrice = 75000f;
    private float _deposit = 15000f;
    private float _tradeIn = 10000f;
    private int _loanTerm = 48;
    private float _interestRate = 4.5f;
    private float _balloonPercent = 0f; // 0-50

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        
        _retailPriceLabel = root.Q<Label>("retail-price");
        _depositField = root.Q<FloatField>("deposit-field");
        _tradeInField = root.Q<FloatField>("tradein-field");
        _loanTermField = root.Q<IntegerField>("loan-term-field");
        _financedAmountLabel = root.Q<Label>("financed-amount");
        _interestSlider = root.Q<Slider>("interest-slider");
        _interestValueLabel = root.Q<Label>("interest-rate-value");
        _balloonSlider = root.Q<Slider>("balloon-slider");
        _balloonPercentLabel = root.Q<Label>("balloon-percent-value");
        _balloonDollarLabel = root.Q<Label>("balloon-dollar-value");
        _monthlyPaymentLabel = root.Q<Label>("monthly-payment");
        _balloonInfoLabel = root.Q<Label>("balloon-info");
        _sendButton = root.Q<Button>("send-button");

        // Set initial display
        _retailPriceLabel.text = $"${CarPrice:N0}";
        
        // Register callbacks
        _depositField.RegisterValueChangedCallback(evt => { _deposit = evt.newValue; UpdateAll(); });
        _tradeInField.RegisterValueChangedCallback(evt => { _tradeIn = evt.newValue; UpdateAll(); });
        _loanTermField.RegisterValueChangedCallback(evt => { _loanTerm = evt.newValue; UpdateAll(); });
        _interestSlider.RegisterValueChangedCallback(evt => { _interestRate = evt.newValue; UpdateAll(); });
        _balloonSlider.RegisterValueChangedCallback(evt => { _balloonPercent = evt.newValue; UpdateAll(); });
        
        _sendButton.clicked += OnSendToDealer;
        
        // Initial update
        UpdateAll();
    }

    private void UpdateAll()
    {
        // Clamp values (optional)
        _deposit = Mathf.Max(0, _deposit);
        _tradeIn = Mathf.Max(0, _tradeIn);
        _loanTerm = Mathf.Max(1, _loanTerm);
        _interestRate = Mathf.Clamp(_interestRate, 0f, 15f);
        _balloonPercent = Mathf.Clamp(_balloonPercent, 0f, 50f);
        
        // Update fields if they were clamped
        if (Math.Abs(_depositField.value - _deposit) > 0.01f) _depositField.value = _deposit;
        if (Math.Abs(_tradeInField.value - _tradeIn) > 0.01f) _tradeInField.value = _tradeIn;
        if (_loanTermField.value != _loanTerm) _loanTermField.value = _loanTerm;
        if (Math.Abs(_interestSlider.value - _interestRate) > 0.01f) _interestSlider.value = _interestRate;
        if (Math.Abs(_balloonSlider.value - _balloonPercent) > 0.01f) _balloonSlider.value = _balloonPercent;
        
        float financed = CarPrice - _deposit - _tradeIn;
        if (financed < 0) financed = 0;
        _financedAmountLabel.text = $"${financed:N0}";
        
        float balloonAmount = financed * (_balloonPercent / 100f);
        _balloonPercentLabel.text = $"{_balloonPercent:F0}%";
        _balloonDollarLabel.text = $"(${balloonAmount:N0})";
        _interestValueLabel.text = $"{_interestRate:F1}%";
        
        float monthly = CalculateMonthlyPayment(financed, balloonAmount, _loanTerm, _interestRate);
        _monthlyPaymentLabel.text = $"${monthly:N0}";
        
        if (_balloonPercent > 0)
        {
            _balloonInfoLabel.text = $"+ ${balloonAmount:N0} balloon due at end";
            _balloonInfoLabel.style.display = DisplayStyle.Flex;
        }
        else
        {
            _balloonInfoLabel.style.display = DisplayStyle.None;
        }
    }

    private float CalculateMonthlyPayment(float financed, float balloon, int months, float annualRatePercent)
    {
        float principal = financed - balloon;
        if (principal <= 0) return 0;
        
        float monthlyRate = annualRatePercent / 100f / 12f;
        if (monthlyRate == 0f) return principal / months;
        
        float factor = Mathf.Pow(1 + monthlyRate, months);
        float monthly = principal * (monthlyRate * factor) / (factor - 1);
        return monthly;
    }

    private void OnSendToDealer()
    {
        Debug.Log($"Send to Dealer: Financed = {_financedAmountLabel.text}, Monthly = {_monthlyPaymentLabel.text}");
        // Add your own logic (e.g., open email, API call, etc.)
    }

    private void OnDisable()
    {
        if (_sendButton != null) _sendButton.clicked -= OnSendToDealer;
    }
}