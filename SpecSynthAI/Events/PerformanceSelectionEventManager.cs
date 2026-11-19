using Autodesk.Revit.UI;
using SpecSynthAI.Events;
using System;

namespace SpecSynthAI
{
    public static class PerformanceSelectionEventManager
    {
        private static PerformanceWallSelectionHandler? _handler;
        private static ExternalEvent? _externalEvent;

        public static bool IsInitialized =>
            _handler != null && _externalEvent != null;

        // ==========================================
        // INITIALISE EXTERNAL EVENT
        // Called from a Revit ExternalCommand.
        // ==========================================

        public static void Initialize()
        {
            if (IsInitialized)
                return;

            _handler = new PerformanceWallSelectionHandler();

            _externalEvent = ExternalEvent.Create(_handler);
        }

        // ==========================================
        // REGISTER RESULT CALLBACK
        // ==========================================

        public static void SetCompletionCallback(
            Action<PerformanceWallSelectionResult> callback)
        {
            if (_handler == null)
                throw new InvalidOperationException(
                    "Performance selection event is not initialized.");

            _handler.RequestSelection(callback);
        }

        // ==========================================
        // RAISE EXTERNAL EVENT
        // Called by the WPF dashboard.
        // ==========================================

        public static ExternalEventRequest Raise()
        {
            if (_externalEvent == null)
                throw new InvalidOperationException(
                    "Performance selection event is not initialized.");

            return _externalEvent.Raise();
        }
    }
}