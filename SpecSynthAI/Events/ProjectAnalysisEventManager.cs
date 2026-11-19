using Autodesk.Revit.UI;

using SpecSynthAI.Events;

using System;

namespace SpecSynthAI
{
    public static class ProjectAnalysisEventManager
    {
        private static ProjectAnalysisEventHandler? _handler;

        private static ExternalEvent? _externalEvent;

        public static bool IsInitialized =>
            _handler != null && _externalEvent != null;


        // ==========================================
        // INITIALIZE EXTERNAL EVENT
        // Must be called from a Revit API context.
        // ==========================================

        public static void Initialize()
        {
            if (IsInitialized)
                return;

            _handler = new ProjectAnalysisEventHandler();

            try
            {
                _externalEvent = ExternalEvent.Create(_handler);
            }
            catch
            {
                _handler = null;
                _externalEvent = null;

                throw;
            }
        }


        // ==========================================
        // REQUEST PROJECT ANALYSIS
        // Registers callback and raises event.
        // ==========================================

        public static ExternalEventRequest RequestAnalysis(
            Action<ProjectAnalysisResult> completed)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(
                    "Project analysis event is not initialized. " +
                    "Initialize it from a Revit command first.");
            }

            if (completed == null)
            {
                throw new ArgumentNullException(nameof(completed));
            }

            _handler!.RequestAnalysis(completed);

            return _externalEvent!.Raise();
        }


        // ==========================================
        // CLEANUP
        // ==========================================

        public static void Shutdown()
        {
            _externalEvent?.Dispose();

            _externalEvent = null;
            _handler = null;
        }
    }
}
