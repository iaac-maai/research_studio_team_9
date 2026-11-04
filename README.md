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

Operable Futures: AI Systems for AEC · Research Studio: Smart Construction · MaAI01 · IAAC 2026/27 · **Team 9**

**Stop non-compliant products reaching the spec.** SpecSynth AI extracts manufacturer data into material schedules, validates it against project requirements and links every value to its evidence.

## Team

- Fawad Saleem
- Soumyajyoti Datta
- Kazuko Nakao

## AEC stage

**Procurement & Off-Site Manufacturing** (materials, suppliers, prefabrication, logistics)

## Problem brief

BIM models are geometrically precise but are often populated with unverified, disconnected product data. A Revit wall can carry "Generic Wall Insulation, U-value: undefined" while the manufacturer's PDF states a specific product with λ = 0.020 W/(m·K). Checking these values by hand against project requirements is slow and error-prone, and specification errors are a major driver of design rework.

- **User:** architects, specifiers and BIM coordinators checking product data before tender.
- **Moment in the project:** product selection and approval, before the specification is issued.

## Data brief

| Source | Format | What it gives us | Gaps / limits |
|---|---|---|---|
| Manufacturer data sheets | PDF | Thermal conductivity (λ), fire rating, thickness | Layout varies by manufacturer; values may be missing or ambiguous |
| Product catalogue | JSON (`products.json`) | Structured product records with source URL and verification status | Currently small and hand-entered |
| BIM model | Revit | Wall types, compound-structure layers, materials, thicknesses | Materials often generic |
| Project requirements | Hardcoded threshold (0.022 W/(m·K)) | Target limit for the thermal check | Not yet configurable; TODO: name the code or project source |

## Heilmeier Catechism

1. **What are you trying to do?** TODO
2. **How is it done today, and what are the limits of current practice?** TODO
3. **What is new in your approach and why do you think it will be successful?** TODO
4. **Who cares? If you succeed, what difference will it make?** TODO
5. **What are the risks?** TODO
6. **How much will it cost?** TODO
7. **How long will it take?** TODO
8. **What are the mid-term and final "exams" to check for success?** TODO

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

This repository follows the studio's Spec-Driven Development layout:

``` text
research_studio_team_9/
├── AGENTS.md                 # System constitution for the agent harness
├── README.md                 # Problem & data brief, team, how to run
├── spec/
│   └── constitution/
│       ├── mission.md
│       ├── roadmap.md
│       └── tech_stack.md
├── src/                      # Features (Revit add-in code goes here)
├── tests/                    # Tests, including the non-AI baseline comparison
└── docs/
    ├── BLOG_POST/            # IAAC blog post draft
    ├── PITCH_DECK/           # Pitch deck and promo video
    └── DEMO_OUTPUTS/         # Example Excel exports from the prototype
```

The Revit add-in source is planned to live under `src/` with this layout:

``` text
src/SpecSynthAI/
├── App.cs
├── Commands/
│   ├── InspectElementCommand.cs
│   ├── ExportScheduleCommand.cs
│   └── ExportThermalScheduleCommand.cs
├── Models/
├── Requirements/
├── products.json
├── InspectElement.png
└── ExportThermalSchedule.png
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
    `src/SpecSynthAI/`.
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
