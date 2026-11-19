using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using SpecSynthAI.Models;
using SpecSynthAI.Services;
using System;
using System.Diagnostics;
using System.Linq;

namespace SpecSynthAI.Events
{
    public static class ComplianceSelectionHandler
    {
        private static UIApplication? _uiApplication;

        public static bool IsListening { get; private set; }

        // ==========================================
        // START SELECTION MONITORING
        // ==========================================

        public static void Start(UIApplication uiApplication)
        {
            if (uiApplication == null || IsListening)
                return;

            _uiApplication = uiApplication;

            _uiApplication.SelectionChanged += OnSelectionChanged;

            IsListening = true;

            Debug.WriteLine(
                "SpecSynth AI: Selection monitoring started.");
        }

        // ==========================================
        // STOP SELECTION MONITORING
        // ==========================================

        public static void Stop()
        {
            if (_uiApplication == null || !IsListening)
                return;

            _uiApplication.SelectionChanged -= OnSelectionChanged;

            _uiApplication = null;
            IsListening = false;

            ComplianceModePaneProvider.UpdateSelection(
                "Compliance Mode is inactive.");

            // Clear previous inspection results.
            ComplianceModePaneProvider.UpdateInspection(null);

            Debug.WriteLine(
                "SpecSynth AI: Selection monitoring stopped.");
        }

        // ==========================================
        // SELECTION CHANGED EVENT
        // ==========================================

        private static void OnSelectionChanged(
            object? sender,
            SelectionChangedEventArgs args)
        {
            if (!IsListening ||
                !ComplianceHeatmapSession.IsActive)
                return;

            try
            {
                // Get the document associated with the selection.
                Document selectedDocument = args.GetDocument();

                Document? activeDocument =
                    ComplianceHeatmapSession.ActiveDocument;

                if (activeDocument == null ||
                    !activeDocument.Equals(selectedDocument))
                {
                    ComplianceModePaneProvider.UpdateSelection(
                        "Selected element is not part of the active heatmap.");

                    ComplianceModePaneProvider.UpdateInspection(null);

                    return;
                }

                // Get selected element IDs.
                var selectedIds = args.GetSelectedElements();

                // ==================================
                // NO ELEMENT SELECTED
                // ==================================

                if (selectedIds.Count == 0)
                {
                    ComplianceModePaneProvider.UpdateSelection(
                        "No element selected.");

                    ComplianceModePaneProvider.UpdateInspection(null);

                    return;
                }

                // ==================================
                // MULTIPLE ELEMENTS SELECTED
                // ==================================

                if (selectedIds.Count > 1)
                {
                    ComplianceModePaneProvider.UpdateSelection(
                        "Select one heatmapped wall at a time.");

                    ComplianceModePaneProvider.UpdateInspection(null);

                    return;
                }

                // Get the single selected element.
                ElementId selectedId = selectedIds.First();

                // ==================================
                // CHECK HEATMAP MEMBERSHIP
                // ==================================

                var heatmapIds =
                    ComplianceHeatmapSession.GetModifiedElementIds();

                bool belongsToHeatmap =
                    heatmapIds.Any(id => id.Equals(selectedId));

                if (!belongsToHeatmap)
                {
                    ComplianceModePaneProvider.UpdateSelection(
                        "Selected element is not part of the active heatmap.");

                    ComplianceModePaneProvider.UpdateInspection(null);

                    return;
                }

                // ==================================
                // INSPECT SELECTED WALL
                // ==================================

                WallInspectionData inspection =
                    ComplianceInspectionService.InspectWall(
                        selectedDocument,
                        selectedId);

                // ==================================
                // DEBUG OUTPUT
                // ==================================

                Debug.WriteLine("");
                Debug.WriteLine("==================================");
                Debug.WriteLine("SPECSYNTH AI - WALL INSPECTION");
                Debug.WriteLine("==================================");

                Debug.WriteLine(
                    $"Element ID: {inspection.ElementId}");

                Debug.WriteLine(
                    $"Wall Name: {inspection.WallName}");

                Debug.WriteLine(
                    $"Wall Type: {inspection.WallTypeName}");

                Debug.WriteLine(
                    $"Inspection Message: {inspection.Message}");

                Debug.WriteLine(
                    $"Products Loaded: {inspection.ProductsLoaded}");

                Debug.WriteLine(
                    $"Maximum Thermal Conductivity: " +
                    $"{inspection.MaximumThermalConductivity} " +
                    $"{inspection.ThermalUnit}");

                foreach (InsulationLayerInspectionData layer
                    in inspection.InsulationLayers)
                {
                    Debug.WriteLine("");
                    Debug.WriteLine("--- INSULATION LAYER ---");

                    Debug.WriteLine(
                        $"Material: {layer.MaterialName}");

                    Debug.WriteLine(
                        $"Installed Thickness: {layer.InstalledThicknessMm} mm");

                    Debug.WriteLine(
                        $"Product Mapping: {layer.ProductMapping}");

                    Debug.WriteLine(
                        $"Thickness Match: {layer.ThicknessMatch}");

                    Debug.WriteLine(
                        $"Thermal Result: {layer.ThermalResult}");

                    Debug.WriteLine(
                        $"Manufacturer: {layer.Manufacturer}");

                    Debug.WriteLine(
                        $"Product: {layer.ProductName}");

                    Debug.WriteLine(
                        $"Product ID: {layer.ProductId}");

                    Debug.WriteLine(
                        $"Verification: {layer.VerificationStatus}");

                    Debug.WriteLine(
                        $"Thermal Conductivity: " +
                        $"{layer.ThermalConductivityWmK}");

                    Debug.WriteLine(
                        $"Layer Compliance: {layer.ComplianceStatusLabel}");

                    Debug.WriteLine(
                        $"Layer Explanation: {layer.ComplianceExplanation}");

                    Debug.WriteLine(
                        $"Source Document: {layer.SourceDocument}");

                    Debug.WriteLine(
                        $"Source URL: {layer.SourceUrl}");
                }

                Debug.WriteLine("");
                Debug.WriteLine(
                    $"Overall Compliance: {inspection.ComplianceStatusLabel}");

                Debug.WriteLine(
                    $"Overall Explanation: {inspection.ComplianceExplanation}");

                Debug.WriteLine("==================================");
                Debug.WriteLine("INSPECTION COMPLETED");
                Debug.WriteLine("==================================");

                // ==================================
                // UPDATE COMPLIANCE PANEL
                // ==================================

                ComplianceModePaneProvider.UpdateSelection(
                    $"Inspection completed: {inspection.WallName}");

                ComplianceModePaneProvider.UpdateInspection(
                    inspection);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "SpecSynth AI Inspection Error: " + ex);

                ComplianceModePaneProvider.UpdateSelection(
                    "Inspection failed: " + ex.Message);

                // Clear previous inspection results after an error.
                ComplianceModePaneProvider.UpdateInspection(null);
            }
        }
    }
}