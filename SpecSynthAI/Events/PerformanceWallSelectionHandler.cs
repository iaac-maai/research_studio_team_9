using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using SpecSynthAI.Models;
using SpecSynthAI.Services;

using ThermalEngine.Models;

using System;
using System.IO;

namespace SpecSynthAI.Events
{
    public class PerformanceWallSelectionHandler : IExternalEventHandler
    {
        // ==========================================
        // CALLBACK
        // ==========================================

        private Action<PerformanceWallSelectionResult>? _completed;

        private readonly RevitWallExtractionService _extractionService;

        public PerformanceWallSelectionHandler()
        {
            _extractionService = new RevitWallExtractionService();
        }


        // ==========================================
        // REQUEST SELECTION
        // Called by the WPF dashboard.
        // ==========================================

        public void RequestSelection(
            Action<PerformanceWallSelectionResult> completed)
        {
            _completed = completed ??
                throw new ArgumentNullException(nameof(completed));
        }


        // ==========================================
        // EXECUTE
        // Revit calls this through ExternalEvent.
        // ==========================================

        public void Execute(UIApplication application)
        {
            PerformanceWallSelectionResult result;

            try
            {
                UIDocument? uiDocument =
                    application.ActiveUIDocument;

                if (uiDocument == null)
                {
                    result = PerformanceWallSelectionResult.Failed(
                        "No active Revit document is available.");

                    Complete(result);
                    return;
                }

                Document document = uiDocument.Document;


                // ======================================
                // 1. LOAD PRODUCT CATALOGUE
                // ======================================

                string assemblyDirectory =
                    Path.GetDirectoryName(
                        typeof(PerformanceWallSelectionHandler)
                            .Assembly.Location) ?? "";

                string catalogPath = Path.Combine(
                    assemblyDirectory,
                    "products.json"
                );

                if (!File.Exists(catalogPath))
                {
                    result = PerformanceWallSelectionResult.Failed(
                        "Product catalogue not found:\n" + catalogPath);

                    Complete(result);
                    return;
                }

                ProductCatalog catalog =
                    ProductCatalog.Load(catalogPath);


                // ======================================
                // 2. SELECT WALL
                // ======================================

                Reference reference =
                    uiDocument.Selection.PickObject(
                        ObjectType.Element,
                        "Select a wall for thermal performance analysis."
                    );

                Element? selectedElement =
                    document.GetElement(reference);

                if (selectedElement is not Wall wall)
                {
                    result = PerformanceWallSelectionResult.Failed(
                        "The selected element is not a wall.");

                    Complete(result);
                    return;
                }


                // ======================================
                // 3. EXTRACT WALL ASSEMBLY
                // ======================================

                WallAssembly assembly =
                    _extractionService.ExtractAssembly(
                        document,
                        wall,
                        catalog
                    );


                // ======================================
                // 4. PREPARE RESULT
                // ======================================

                result = new PerformanceWallSelectionResult
                {
                    Success = true,

                    WallId = wall.Id.Value.ToString(),

                    WallTypeId = wall.WallType.Id.Value.ToString(),

                    WallName = wall.Name,

                    WallTypeName = wall.WallType.Name,

                    Assembly = assembly,

                    Message = "Wall extraction completed successfully."
                };
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                result = PerformanceWallSelectionResult.Cancelled();
            }
            catch (Exception ex)
            {
                result = PerformanceWallSelectionResult.Failed(
                    ex.Message);
            }

            Complete(result);
        }


        // ==========================================
        // COMPLETE REQUEST
        // ==========================================

        private void Complete(
            PerformanceWallSelectionResult result)
        {
            Action<PerformanceWallSelectionResult>? callback =
                _completed;

            _completed = null;

            callback?.Invoke(result);
        }


        // ==========================================
        // EVENT NAME
        // ==========================================

        public string GetName()
        {
            return "SpecSynth AI - Performance Wall Selection";
        }
    }


    // ==============================================
    // SELECTION RESULT MODEL
    // ==============================================

    public class PerformanceWallSelectionResult
    {
        public bool Success { get; set; }

        public bool WasCancelled { get; set; }

        public string Message { get; set; } = "";

        public string WallId { get; set; } = "";

        public string WallTypeId { get; set; } = "";

        public string WallName { get; set; } = "";

        public string WallTypeName { get; set; } = "";

        public WallAssembly? Assembly { get; set; }


        public static PerformanceWallSelectionResult Failed(
            string message)
        {
            return new PerformanceWallSelectionResult
            {
                Success = false,
                Message = message
            };
        }


        public static PerformanceWallSelectionResult Cancelled()
        {
            return new PerformanceWallSelectionResult
            {
                Success = false,
                WasCancelled = true,
                Message = "Wall selection was cancelled."
            };
        }
    }
}