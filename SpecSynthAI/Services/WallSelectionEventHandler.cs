using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using System;
using System.Collections.Generic;

namespace SpecSynthAI.Services
{
    // =============================================
    // WALL SELECTION EVENT HANDLER
    // =============================================

    public class WallSelectionEventHandler : IExternalEventHandler
    {
        // Callback used to return the result to the UI.
        public Action<WallSelectionResult>? Completed { get; set; }


        // =============================================
        // EXECUTE
        // Revit calls this method when ExternalEvent
        // is raised from the modeless dashboard.
        // =============================================

        public void Execute(UIApplication application)
        {
            WallSelectionResult result;

            try
            {
                UIDocument? uiDoc =
                    application.ActiveUIDocument;

                if (uiDoc == null)
                {
                    result = WallSelectionResult.Failed(
                        "No active Revit document is available.");

                    Completed?.Invoke(result);
                    return;
                }

                Document document = uiDoc.Document;


                // =====================================
                // 1. ASK USER TO SELECT A WALL
                // =====================================

                Reference reference = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select a wall for thermal performance analysis."
                );

                Element? element =
                    document.GetElement(reference);


                // =====================================
                // 2. VALIDATE SELECTED ELEMENT
                // =====================================

                if (element is not Wall wall)
                {
                    result = WallSelectionResult.Failed(
                        "The selected element is not a wall.");

                    Completed?.Invoke(result);
                    return;
                }


                // =====================================
                // 3. GET WALL TYPE
                // =====================================

                WallType wallType = wall.WallType;

                CompoundStructure? structure =
                    wallType.GetCompoundStructure();

                if (structure == null)
                {
                    result = WallSelectionResult.Failed(
                        "The selected wall type has no compound structure.");

                    Completed?.Invoke(result);
                    return;
                }


                // =====================================
                // 4. EXTRACT WALL INFORMATION
                // =====================================

                var wallData = new WallSelectionResult
                {
                    Success = true,

                    WallId = wall.Id.Value.ToString(),

                    WallTypeId = wallType.Id.Value.ToString(),

                    WallName = wall.Name,

                    WallTypeName = wallType.Name,

                    LayerCount = structure.GetLayers().Count
                };


                // =====================================
                // 5. EXTRACT MATERIAL LAYERS
                // =====================================

                IList<CompoundStructureLayer> layers =
                    structure.GetLayers();

                foreach (CompoundStructureLayer layer in layers)
                {
                    Material? material = null;

                    if (layer.MaterialId != ElementId.InvalidElementId)
                    {
                        material =
                            document.GetElement(layer.MaterialId)
                            as Material;
                    }

                    double thicknessMm =
                        UnitUtils.ConvertFromInternalUnits(
                            layer.Width,
                            UnitTypeId.Millimeters
                        );

                    var layerData = new WallSelectionLayer
                    {
                        LayerName = material?.Name ?? "Unassigned material",

                        MaterialId = layer.MaterialId.Value.ToString(),

                        ThicknessMm = thicknessMm,

                        Function = layer.Function.ToString(),

                        IsStructuralMaterial =
                            layer.Function ==
                            MaterialFunctionAssignment.Structure,

                        IsInsulation =
                            layer.Function ==
                            MaterialFunctionAssignment.Insulation
                    };

                    wallData.Layers.Add(layerData);
                }


                // =====================================
                // 6. RETURN RESULT
                // =====================================

                result = wallData;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                result = WallSelectionResult.Cancelled();
            }
            catch (Exception ex)
            {
                result = WallSelectionResult.Failed(
                    ex.Message);
            }

            Completed?.Invoke(result);
        }


        // =============================================
        // EVENT NAME
        // =============================================

        public string GetName()
        {
            return "SpecSynth AI - Wall Selection Event";
        }
    }


    // =============================================
    // WALL SELECTION RESULT
    // =============================================

    public class WallSelectionResult
    {
        public bool Success { get; set; }

        public bool WasCancelled { get; set; }

        public string Message { get; set; } = "";

        public string WallId { get; set; } = "";

        public string WallTypeId { get; set; } = "";

        public string WallName { get; set; } = "";

        public string WallTypeName { get; set; } = "";

        public int LayerCount { get; set; }

        public List<WallSelectionLayer> Layers { get; set; } = new();


        public static WallSelectionResult Failed(string message)
        {
            return new WallSelectionResult
            {
                Success = false,
                Message = message
            };
        }


        public static WallSelectionResult Cancelled()
        {
            return new WallSelectionResult
            {
                Success = false,
                WasCancelled = true,
                Message = "Wall selection was cancelled."
            };
        }
    }


    // =============================================
    // WALL SELECTION LAYER
    // =============================================

    public class WallSelectionLayer
    {
        public string LayerName { get; set; } = "";

        public string MaterialId { get; set; } = "";

        public double ThicknessMm { get; set; }

        public string Function { get; set; } = "";

        public bool IsStructuralMaterial { get; set; }

        public bool IsInsulation { get; set; }
    }
}