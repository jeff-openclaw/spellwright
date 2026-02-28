using System;
using System.Collections.Generic;

namespace Spellwright.Core
{
    /// <summary>
    /// Lightweight generic publish/subscribe event bus for decoupled communication
    /// between game systems. All subscriptions are type-keyed.
    /// </summary>
    /// <remarks>
    /// Thread-safety: this implementation is NOT thread-safe. All calls should
    /// happen on the main thread (Unity's default). If background-thread publishing
    /// is needed, wrap Publish in a main-thread dispatcher.
    /// </remarks>
    public class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

        /// <summary>Singleton instance for convenience. Can also be injected.</summary>
        // TODO: Consider making this a MonoBehaviour singleton or using a DI container in Unity.
        private static EventBus _instance;
        public static EventBus Instance => _instance ??= new EventBus();

        /// <summary>Subscribe a handler for events of type <typeparamref name="T"/>.</summary>
        public void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _subscribers[type] = list;
            }
            list.Add(handler);
        }

        /// <summary>Unsubscribe a previously registered handler.</summary>
        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;

            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var list))
            {
                list.Remove(handler);
            }
        }

        /// <summary>Publish an event to all subscribers of type <typeparamref name="T"/>.</summary>
        public void Publish<T>(T evt)
        {
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list)) return;

            // Iterate over a snapshot to allow subscribe/unsubscribe during dispatch.
            var snapshot = list.ToArray();
            foreach (var del in snapshot)
            {
                try
                {
                    ((Action<T>)del).Invoke(evt);
                }
                catch (Exception ex)
                {
                    // TODO: Replace with Unity's Debug.LogException(ex) when integrated.
                    System.Diagnostics.Debug.WriteLine($"[EventBus] Handler threw for {type.Name}: {ex}");
                }
            }
        }

        /// <summary>Remove all subscribers. Useful for scene transitions or test teardown.</summary>
        public void Clear()
        {
            _subscribers.Clear();
        }

        /// <summary>Remove all subscribers for a specific event type.</summary>
        public void Clear<T>()
        {
            _subscribers.Remove(typeof(T));
        }
    }
}
