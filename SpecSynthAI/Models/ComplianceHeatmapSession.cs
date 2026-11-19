using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpecSynthAI.Models
{
    /// <summary>
    /// Keeps the original element graphic overrides for one active
    /// compliance-heatmap session so they can be restored later.
    /// </summary>
    public static class ComplianceHeatmapSession
    {
        private static readonly Dictionary<long, OverrideGraphicSettings>
            OriginalOverrides = new();

        public static bool IsActive { get; private set; }
        public static Document? ActiveDocument { get; private set; }
        public static ElementId? ActiveViewId { get; private set; }

        public static void BeginSession(Document document, View view)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (view == null)
                throw new ArgumentNullException(nameof(view));

            // The command obtains this view from document.ActiveView.
            // Avoid comparing Revit API wrapper objects with != because
            // separate wrappers can represent the same underlying document.
            OriginalOverrides.Clear();
            ActiveDocument = document;
            ActiveViewId = view.Id;
            IsActive = true;
        }

        public static void SaveOriginalOverride(
            View view,
            ElementId elementId)
        {
            if (!IsActive)
                throw new InvalidOperationException(
                    "A compliance heatmap session has not been started.");

            if (ActiveDocument == null || ActiveViewId == null)
                throw new InvalidOperationException(
                    "The active heatmap session is incomplete.");

            // The active view ID is the reliable session check here.
            if (!view.Id.Equals(ActiveViewId))
                throw new InvalidOperationException(
                    "The supplied view does not match the active heatmap session.");

            long key = elementId.Value;

            // Save the original settings only once, before the first override.
            if (!OriginalOverrides.ContainsKey(key))
            {
                OverrideGraphicSettings original =
                    view.GetElementOverrides(elementId);

                OriginalOverrides.Add(key, original);
            }
        }

        public static bool TryGetOriginalOverride(
            ElementId elementId,
            out OverrideGraphicSettings? originalSettings)
        {
            bool found = OriginalOverrides.TryGetValue(
                elementId.Value,
                out OverrideGraphicSettings? settings);

            originalSettings = settings;
            return found;
        }

        public static IReadOnlyList<ElementId> GetModifiedElementIds()
        {
            return OriginalOverrides.Keys
                .Select(id => new ElementId(id))
                .ToList();
        }

        public static void EndSession()
        {
            OriginalOverrides.Clear();
            ActiveDocument = null;
            ActiveViewId = null;
            IsActive = false;
        }
    }
}
