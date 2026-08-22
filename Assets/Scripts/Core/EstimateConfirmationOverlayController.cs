using UnityEngine;
using UnityEngine.UIElements;
using NAS.Core.Events;

namespace NAS.Core
{
    // Lives on a child of the persistent "Game Manager" GameObject
    // (DontDestroyOnLoad), same placement as LoadingOverlayController -
    // "Send to Dealer" is only ever tapped from the Estimator card in "Main
    // App", but keeping this here (rather than scoped to that one card)
    // means it isn't torn down by ParentPageController.RemoveCardControllers()
    // mid-submission, and its "Back to Car Selection" button can reach
    // ParentPageController purely through EventBus, same as everything else.
    public class EstimateConfirmationOverlayController : MonoBehaviour
    {
        private const float SpinDegreesPerSecond = 220f;

        private VisualElement _root;
        private VisualElement _spinner;
        private VisualElement _checkmark;
        private Label _messageLabel;
        private Button _doneButton;
        private float _spinAngle;
        private bool _spinnerVisible;

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;
            _root = root.Q<VisualElement>("estimate-confirmation-root");
            _spinner = root.Q<VisualElement>("estimate-confirmation-spinner");
            _checkmark = root.Q<VisualElement>("estimate-confirmation-checkmark");
            _messageLabel = root.Q<Label>("estimate-confirmation-message");
            _doneButton = root.Q<Button>("estimate-confirmation-done-button");

            if (_doneButton != null)
                _doneButton.clicked += OnDoneClicked;

            SetVisible(false);

            EventBus.Subscribe<EstimateSubmissionStartedEvent>(OnSubmissionStarted);
            EventBus.Subscribe<EstimateSubmittedEvent>(OnSubmitted);
            EventBus.Subscribe<EstimateSubmissionFailedEvent>(OnSubmissionFailed);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EstimateSubmissionStartedEvent>(OnSubmissionStarted);
            EventBus.Unsubscribe<EstimateSubmittedEvent>(OnSubmitted);
            EventBus.Unsubscribe<EstimateSubmissionFailedEvent>(OnSubmissionFailed);

            if (_doneButton != null)
                _doneButton.clicked -= OnDoneClicked;
        }

        private void Update()
        {
            if (!_spinnerVisible || _spinner == null) return;
            _spinAngle = (_spinAngle + SpinDegreesPerSecond * Time.deltaTime) % 360f;
            _spinner.style.rotate = new StyleRotate(new Rotate(new Angle(_spinAngle, AngleUnit.Degree)));
        }

        private void OnSubmissionStarted(EstimateSubmissionStartedEvent evt)
        {
            SetVisible(true);
            _spinnerVisible = true;
            SetElementVisible(_spinner, true);
            SetElementVisible(_checkmark, false);
            if (_messageLabel != null)
                _messageLabel.text = "Sending to dealer...";
            if (_doneButton != null)
                _doneButton.SetEnabled(false);
        }

        private void OnSubmitted(EstimateSubmittedEvent evt)
        {
            _spinnerVisible = false;
            SetElementVisible(_spinner, false);
            SetElementVisible(_checkmark, true);
            if (_messageLabel != null)
                _messageLabel.text = "Sent to dealer!";
            if (_doneButton != null)
                _doneButton.SetEnabled(true);
        }

        // Best-effort UI only - EstimatorCardController's own inline error
        // label is the actual error message; this just gets the overlay out
        // of the way so that label is visible again.
        private void OnSubmissionFailed(EstimateSubmissionFailedEvent evt) => SetVisible(false);

        private void OnDoneClicked()
        {
            SetVisible(false);
            EventBus.Publish(new ReturnToCarSelectionRequestedEvent());
        }

        private void SetVisible(bool visible)
        {
            if (_root != null)
                _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
                _spinnerVisible = false;
        }

        private static void SetElementVisible(VisualElement element, bool visible)
        {
            if (element != null)
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
