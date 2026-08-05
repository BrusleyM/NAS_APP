using System;
using System.Collections.Generic;

namespace NAS.Core.Events
{
    /// <summary>
    /// Lightweight, static, type-safe publish/subscribe event bus.
    ///
    /// Publishers (e.g. a UI card) and subscribers (e.g. the router, GameManager,
    /// AuthController) never hold a reference to each other. A publisher just does
    /// EventBus.Publish(new SomeEvent(...)); any number of unrelated systems can
    /// react without the publisher knowing or caring who's listening.
    ///
    /// NOTE: this is process-wide and NOT scene-scoped. Always pair every
    /// Subscribe in OnEnable with an Unsubscribe in OnDisable, or destroyed
    /// MonoBehaviours will keep receiving events (and leak).
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _subscribers = new Dictionary<Type, Delegate>();

        public static void Subscribe<TEvent>(Action<TEvent> handler)
        {
            var type = typeof(TEvent);
            _subscribers[type] = _subscribers.TryGetValue(type, out var existing)
                ? Delegate.Combine(existing, handler)
                : handler;
        }

        public static void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            var type = typeof(TEvent);
            if (!_subscribers.TryGetValue(type, out var existing)) return;

            var result = Delegate.Remove(existing, handler);
            if (result == null)
                _subscribers.Remove(type);
            else
                _subscribers[type] = result;
        }

        public static void Publish<TEvent>(TEvent evt)
        {
            var type = typeof(TEvent);
            if (_subscribers.TryGetValue(type, out var existing) && existing is Action<TEvent> action)
            {
                action.Invoke(evt);
            }
        }

        /// <summary>Drops every subscription. Useful between PlayMode tests or on a hard app reset.</summary>
        public static void Clear() => _subscribers.Clear();
    }
}
