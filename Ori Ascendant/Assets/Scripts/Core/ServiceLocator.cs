using System;
using System.Collections.Generic;

namespace OriAscendant.Core
{
    /// <summary>
    /// Central service registry (TECH_DESIGN §4). Systems call
    /// <see cref="Register{T}"/> from Awake() and resolve dependencies with
    /// <see cref="Get{T}"/> from Start() or later — never in Awake(), because
    /// sibling registration order within a frame is not guaranteed.
    /// Owns NO game state. Plain C# so it is EditMode-testable.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        /// <summary>Registers (or replaces) the instance serving type T.</summary>
        public static void Register<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            Services[typeof(T)] = instance;
        }

        /// <summary>Resolves the registered instance; throws if missing (programmer error).</summary>
        public static T Get<T>() where T : class
        {
            if (Services.TryGetValue(typeof(T), out object service)) return (T)service;
            throw new InvalidOperationException(
                $"ServiceLocator: no service registered for {typeof(T).Name}. " +
                "Register in Awake(), resolve in Start().");
        }

        /// <summary>Non-throwing resolve for optional services.</summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out object found))
            {
                service = (T)found;
                return true;
            }
            service = null;
            return false;
        }

        /// <summary>Removes a registration if it is still the given instance (scene teardown).</summary>
        public static void Unregister<T>(T instance) where T : class
        {
            if (Services.TryGetValue(typeof(T), out object found) && ReferenceEquals(found, instance))
            {
                Services.Remove(typeof(T));
            }
        }

        /// <summary>Clears all registrations (tests and full restarts only).</summary>
        public static void Clear() => Services.Clear();
    }
}
