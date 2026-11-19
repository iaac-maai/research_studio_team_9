# Tech stack

Current prototype (see `README.md` for details):

| Layer | Choice | Notes |
|---|---|---|
| Language | C# / .NET (`net10.0-windows`, WPF) | Revit add-in |
| BIM | Autodesk Revit 2027 API | Wall types, compound structures, layers, materials |
| Product catalogue | JSON (`products.json`, System.Text.Json) | Local, hand-entered |
| Excel export | ClosedXML | Thermal schedule workbook |
| Compliance logic | Deterministic rules | PASS / FAIL / CANNOT VERIFY |

To decide:

| Layer | Choice | Notes |
|---|---|---|
| PDF data-sheet extraction | TODO (OCR / computer vision / LLM) | |
| AI-assisted product matching | TODO | Planned, not implemented |
| Agent harness | TODO | Plan and Build modes |
| MCP connections | TODO | |
| Interface beyond Revit | TODO (Web UI / Streamlit / Obsidian) | |
| Testing | TODO | Must include the manual baseline comparison |
