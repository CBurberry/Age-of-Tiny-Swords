using System.Collections.Generic;

namespace Utility
{
    /// <summary>
    /// Utility class that allows for events to be raised and bound too for common operations.
    /// Copies signatures from NetworkVariable<T>. Local un-replicated use only.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EventsVariable<T>
    {
        /// <summary>
        /// Delegate type for value changed event
        /// </summary>
        /// <param name="previousValue">The value before the change</param>
        /// <param name="newValue">The new value</param>
        public delegate void OnValueChangedDelegate(T previousValue, T newValue);

        /// <summary>
        /// The callback to be invoked when the value gets changed
        /// </summary>
        public OnValueChangedDelegate OnValueChanged;

        public EventsVariable(T value = default)
        {
            internalValue = value;
        }

        public virtual T Value
        {
            get => internalValue;
            set
            {
                if (Compare(internalValue, value))
                {
                    return;
                }

                T previousValue = internalValue;
                internalValue = value;
                OnValueChanged?.Invoke(previousValue, internalValue);
            }
        }

        protected T internalValue;

        private static bool Compare(T a, T b)
        {
            return EqualityComparer<T>.Default.Equals(a, b);
        }
    }
}