# Mission

**Stop non-compliant products reaching the spec.**

## Problem

Product data in BIM models is often generic or unverified, while the real values sit in manufacturer PDFs. Checking them by hand against project requirements is slow, and errors carry through to tender and site.

## Users

TODO: name the primary user (e.g. specifier, BIM coordinator) and describe the moment they use the tool.

## What SpecSynth AI does

1. Extracts product properties from manufacturer data sheets into a structured material library.
2. Validates each product against project requirements (PASS / FAIL / CANNOT VERIFY).
3. Links every value to its evidence in the source document.
4. Checks multi-layer assemblies (e.g. wall U-value) inside the BIM model.
5. Exports schedules across many wall types.

## Current status

The prototype (a Revit add-in) maps wall insulation materials to a local product catalogue, checks thermal conductivity against a fixed limit and exports a thermal schedule. Mappings and rules are hardcoded; AI-assisted extraction and matching are next.

## Why AI

TODO: explain why AI is needed rather than a rule, a script or a skilled person (e.g. reading PDFs with varied layouts).

## Out of scope

TODO
