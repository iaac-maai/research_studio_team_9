# AGENTS.md: SpecSynth AI system constitution

This file sets the rules for any AI agent working in this repository. Read it before every session.

## Project context

- Read `README.md` and `spec/constitution/` (mission, roadmap, tech stack) before planning work.
- SpecSynth AI extracts product data from manufacturer documents, validates it against project requirements, and links every value to its source.
- The current prototype is a C# / .NET Revit 2027 add-in with a local `products.json` catalogue and deterministic rules. AI-assisted matching is planned, not implemented.

## Modes

- **Plan mode:** read specs, propose a plan, and wait for approval. Do not edit code.
- **Build mode:** implement only what the approved plan and specs describe.

## Rules

- Every extracted value must keep a link to its evidence (file, page, and the text it came from).
- If a value cannot be found or verified, return `CANNOT VERIFY`. Never guess or fill in a typical value.
- Compliance results are only `PASS`, `FAIL` or `CANNOT VERIFY`.
- Units must be explicit and converted consistently (e.g. λ in W/(m·K), U-value in W/(m²·K)).
- Every feature in `src/` needs tests in `tests/`, including a comparison against the manual (non-AI) method.

## Do not

- Do not invent product data, test results, standards or citations.
- Do not change requirement thresholds without a source.
- Do not present illustrative demo products (e.g. `DEMO_INSULATION_BOARD_100`) as real manufacturer data.
- `PASS` is not a certification and `CANNOT VERIFY` does not mean non-compliant; keep this wording in all outputs.
- Do not commit API keys, credentials or client data.
- Do not edit files in `spec/constitution/` without the team's approval.

## Context hygiene

Keep project knowledge in these files, not in chat history. Reset long sessions (`/compact`, `/clear`) and re-read this file.
