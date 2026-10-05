using System;
using UnityEngine;

// Shared notification safety; no persistence or manager-instance ownership.
internal static class GameFactoryEvents
{
    private const int MaxNotificationDepth = 16;
    private static int notificationDepth;

    internal static void Raise(Action listeners, UnityEngine.Object context)
    {
        if (listeners == null || notificationDepth >= MaxNotificationDepth) return;
        notificationDepth++;
        try
        {
            foreach (Action listener in listeners.GetInvocationList())
            {
                try { listener(); }
                catch (Exception exception) { Debug.LogException(exception, context); }
            }
        }
        finally { notificationDepth--; }
    }

    internal static void Raise<T>(Action<T> listeners, T value, UnityEngine.Object context)
    {
        if (listeners == null || notificationDepth >= MaxNotificationDepth) return;
        notificationDepth++;
        try
        {
            foreach (Action<T> listener in listeners.GetInvocationList())
            {
                try { listener(value); }
                catch (Exception exception) { Debug.LogException(exception, context); }
            }
        }
        finally { notificationDepth--; }
    }
}
