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
    /// </summary>
    public class SplashScreenController : MonoBehaviour
    {
        [SerializeField] private Button _continueButton;

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;

            _continueButton = root.Q<Button>("splash-continue-button");

            if (_continueButton != null)
                _continueButton.clicked += OnContinueClicked;
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
