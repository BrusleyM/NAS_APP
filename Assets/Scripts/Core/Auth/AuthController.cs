using UnityEngine;
using NAS.Core.Events;
using NAS.Core.Models;

namespace NAS.Core.Auth
{
    /// <summary>
    /// Owns all authentication business logic. It subscribes to login/register
    /// requests from whichever UI card raised them and publishes the result.
    ///
    /// Nothing about the UI knows this class exists, and this class knows
    /// nothing about UIElements, VisualTreeAssets, or screen flow. Swapping in
    /// a real backend (Cognito, custom API, etc.) only touches this file.
    ///
    /// Attach to a persistent object (e.g. alongside GameManager) so it's
    /// alive for the whole session.
    /// </summary>
    public class AuthController : MonoBehaviour
    {
        private void OnEnable()
        {
            EventBus.Subscribe<LoginRequestedEvent>(HandleLoginRequested);
            EventBus.Subscribe<RegisterRequestedEvent>(HandleRegisterRequested);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LoginRequestedEvent>(HandleLoginRequested);
            EventBus.Unsubscribe<RegisterRequestedEvent>(HandleRegisterRequested);
        }

        private void HandleLoginRequested(LoginRequestedEvent evt)
        {
            if (string.IsNullOrWhiteSpace(evt.Email) || string.IsNullOrWhiteSpace(evt.Password))
            {
                EventBus.Publish(new AuthFailedEvent("Email and password are required."));
                return;
            }

            // TODO: replace with a real auth call (Cognito, custom backend, etc).
            var user = new User { email = evt.Email, displayName = evt.Email };
            EventBus.Publish(new AuthSucceededEvent(user));
        }

        private void HandleRegisterRequested(RegisterRequestedEvent evt)
        {
            if (string.IsNullOrWhiteSpace(evt.Email) || string.IsNullOrWhiteSpace(evt.Password))
            {
                EventBus.Publish(new AuthFailedEvent("Email and password are required."));
                return;
            }

            if (evt.Password != evt.ConfirmPassword)
            {
                EventBus.Publish(new AuthFailedEvent("Passwords do not match."));
                return;
            }

            // TODO: replace with a real registration call.
            var user = new User { email = evt.Email, displayName = evt.Email };
            EventBus.Publish(new AuthSucceededEvent(user));
        }
    }
}
