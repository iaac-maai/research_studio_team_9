# research_studio_team_9
Fawad, Soumya, Kazuko

# SpecSynth AI

**A Revit add-in prototype for mapping BIM insulation materials to
manufacturer product data and checking thermal-conductivity
requirements.**

SpecSynth AI is being developed as part of the IAAC Research Studio. The
current prototype connects Revit wall-layer information to a local JSON
product catalogue, applies an explicit material-to-product mapping,
checks a product property against a configured threshold, and exports a
thermal schedule to Excel.

> **Prototype status:** This is a research/demo prototype, not a
> certified compliance tool. The current workflow uses hardcoded
> mappings and deterministic rules; AI-assisted product matching is
> planned, not implemented.

## What it does

-   **Inspect Element** --- select a wall in Revit and inspect its
    insulation layer, mapped product information, and thermal-check
    result.
-   **Export Thermal Schedule** --- export insulation-layer information
    from wall types used by placed wall instances into an Excel
    workbook.
-   **Product catalogue** --- read product records from `products.json`.
-   **Rule-based thermal check** --- compare a mapped product's thermal
    conductivity with a configured maximum of `0.022 W/(m·K)`.
-   **Three demo outcomes** --- `PASS`, `FAIL`, and `CANNOT VERIFY`.

## Demo outcomes

  ---------------------------------------------------------------------------------------------------------
  Scenario      Revit material           Catalogue product                   Conductivity Result
  ------------- ------------------------ ------------------------------ ----------------- -----------------
  Within        `SSAI_PIR_Insulation`    `MAGON_MASTER_BASIC_PIR_100`     `0.020 W/(m·K)` `PASS`
  configured                             (Magon MASTER-BASIC)                             
  threshold                                                                               

  Above         `SSAI_Demo_Insulation`   `DEMO_INSULATION_BOARD_100`      `0.028 W/(m·K)` `FAIL`
  configured                             (illustrative demo product)                      
  threshold                                                                               

  No product    `Rigid insulation`       No match                                 Unknown `CANNOT VERIFY`
  mapping                                                                                 
  ---------------------------------------------------------------------------------------------------------

**Demo-data note:** `DEMO_INSULATION_BOARD_100` uses illustrative values
created for demonstration only. It is not a verified manufacturer
product or real manufacturer specification. Do not present its values as
factual product data.

## Current workflow

``` text
Revit wall and compound-structure layers
                 |
                 v
       Read material and thickness
                 |
                 v
      Explicit material-name mapping
                 |
                 v
       Local products.json catalogue
                 |
                 v
 Compare conductivity to configured limit
                 |
                 v
          PASS / FAIL / CANNOT VERIFY
                 |
                 v
          Export thermal schedule to Excel
```

## Technology stack

-   **C# / .NET** --- Revit add-in implementation
-   **Autodesk Revit API** --- read wall types, compound structures,
    layers, and materials
-   **JSON / System.Text.Json** --- local product catalogue
-   **ClosedXML** --- Excel workbook generation
-   **WPF imaging** --- Revit ribbon button icons

## Repository structure

The wider project is organized as follows:

``` text
SpecSynth_AI/
├── 00_Project_Documentation/
├── 01_Revit/
├── 02_RevitPlugin/
│   └── SpecSynthAI/
│       └── SpecSynthAI/
│           ├── App.cs
│           ├── Commands/
│           │   ├── InspectElementCommand.cs
│           │   ├── ExportScheduleCommand.cs
│           │   └── ExportThermalScheduleCommand.cs
│           ├── Models/
│           ├── Requirements/
│           ├── products.json
│           ├── InspectElement.png
│           └── ExportThermalSchedule.png
├── 03_Product_Document_Extraction/
├── 04_Product_Dataset/
└── 05_Research_Experiments/
```

`ExportScheduleCommand.cs` is reserved for future generic
schedule-export functionality. The current demo uses
`ExportThermalScheduleCommand.cs`.

## Requirements

-   Autodesk Revit 2027
-   Visual Studio with a compatible .NET SDK and Windows desktop/WPF
    targeting support
-   Revit 2027 API assemblies (`RevitAPI.dll` and `RevitAPIUI.dll`)
-   NuGet package: ClosedXML
-   A Revit model containing wall types and compound-structure layers
    for testing

The current project targets `net10.0-windows` with WPF enabled. Confirm
framework and API compatibility in the development environment before
building or deploying elsewhere.

## Setup and run

1.  Clone or download this repository.
2.  Open the C# solution/project under
    `02_RevitPlugin/SpecSynthAI/SpecSynthAI/`.
3.  Confirm the project references the Revit 2027 API assemblies
    installed on your machine.
4.  Restore NuGet packages and build the project.
5.  Ensure `products.json`, `InspectElement.png`, and
    `ExportThermalSchedule.png` are copied to the same output directory
    as `SpecSynthAI.dll`.
6.  Configure the Revit add-in manifest to point to the built
    `SpecSynthAI.dll` and the correct application entry point,
    `SpecSynthAI.App`.
7.  Start or restart Revit 2027 and open a test model.
8.  Use **SpecSynth AI → Inspection → Inspect Element** to inspect a
    wall.
9.  Use **SpecSynth AI → Inspection → Export Thermal Schedule** to
    create an Excel workbook.

Paths to Revit API assemblies and the add-in manifest are
machine-specific and may need to be updated.

## Product catalogue

The catalogue is stored in `products.json`. Each product record
currently supports fields such as:

-   `product_id`
-   `manufacturer`
-   `product_name`
-   `category`
-   `thickness_mm`
-   `thermal_conductivity_W_mK`
-   `compressive_strength_kPa`
-   `compressive_strength_tolerance_kPa`
-   `reaction_to_fire`
-   `source_url`
-   `verification_status`

Example catalogue records include a Magon product and an explicitly
labelled illustrative demo product. Review all catalogue data and source
documentation before using it for real project decisions.

## Important limitations

-   Material-to-product mappings are hardcoded in the current C#
    commands; arbitrary materials are not matched automatically.
-   The maximum conductivity threshold (`0.022 W/(m·K)`) is hardcoded in
    the current command.
-   The current checker evaluates catalogue conductivity only. It does
    not verify installed product identity, independently validate
    manufacturer documentation, assess the full wall assembly, or
    establish regulatory compliance.
-   `CANNOT VERIFY` means the current system lacks a usable product
    mapping/value. It does not mean the material is non-compliant.
-   `PASS` means the stored conductivity value is at or below the
    configured threshold; it is not a certification.
-   The thermal exporter focuses on compound-structure layers classified
    as `Insulation`. Curtain-wall panels and other construction systems
    may require separate handling.
-   Excel-to-Revit updates are not implemented.
-   AI-assisted product matching and automated document verification are
    future work.

## Roadmap

1.  Replace hardcoded material-to-product mappings with data-driven
    configuration.
2.  Make project requirements configurable.
3.  Improve source traceability and evidence tracking for product
    properties.
4.  Distinguish missing catalogue values from actual zero values.
5.  Explore AI-assisted product candidate matching while keeping
    compliance rules explainable.
6.  Design a controlled Excel-to-Revit update workflow with validation,
    preview, and user approval.
7.  Expand support for curtain-wall panels and other construction
    systems.

## Safety and intended use

This repository is intended for research, prototyping, and
demonstration. Do not rely on its output as the sole basis for
procurement, construction, code compliance, or safety-critical
decisions. Product data and requirements must be checked against
current, authoritative manufacturer documentation and applicable project
requirements by a qualified professional.

## License

MIT License
