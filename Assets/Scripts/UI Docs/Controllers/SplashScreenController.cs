using UnityEngine;
using UnityEngine.UIElements;
using NAS.Core.Events;

namespace NAS.UI.Controllers
{
    /// <summary>
    /// Splash card shown by ParentPageController before DecideInitialScreen()
    /// runs. Purely a view, same as the other cards: it doesn't know what
    /// happens after the user taps "Get Started" (login vs. car selection vs.
    /// estimator) - it just publishes SplashDismissedEvent and lets
    /// ParentPageController decide, the same way LoginCardController publishes
    /// LoginRequestedEvent without knowing what AuthController does with it.
    ///
    /// The spinner + "Initializing AR sensors..." text from the Figma
    /// reference (mobile-designs.tsx's SplashScreen) are kept as flavor only -
    /// there's no real async initialization to gate on yet (no network calls
    /// exist at this point in the flow), so this deliberately does NOT fake a
    /// progress bar or timed auto-advance. The design itself advances via the
    /// "Get Started" button's onClick rather than a timer, so this mirrors
    /// that: the button tap is the only thing that raises SplashDismissedEvent.
    /// </summary>
    public class SplashScreenController : MonoBehaviour
    {
        private const float SpinDegreesPerSecond = 220f;

        [SerializeField] private Button _continueButton;
        [SerializeField] private VisualElement _spinner;

        private float _spinAngle;

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;

            _continueButton = root.Q<Button>("splash-continue-button");
            _spinner = root.Q<VisualElement>("splash-spinner");

            if (_continueButton != null)
                _continueButton.clicked += OnContinueClicked;
        }

        private void Update()
        {
            if (_spinner == null) return;

            _spinAngle = (_spinAngle + SpinDegreesPerSecond * Time.deltaTime) % 360f;
            _spinner.style.rotate = new StyleRotate(new Rotate(new Angle(_spinAngle, AngleUnit.Degree)));
        }

        private void OnContinueClicked()
        {
            EventBus.Publish(new SplashDismissedEvent());
        }

        private void OnDisable()
        {
            if (_continueButton != null) _continueButton.clicked -= OnContinueClicked;
        }
    }
}
