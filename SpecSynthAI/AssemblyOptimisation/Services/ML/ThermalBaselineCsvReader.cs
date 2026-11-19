using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

using SpecSynthAI.AssemblyOptimisation.Models.ML;

namespace SpecSynthAI.AssemblyOptimisation.Services.ML;

/// <summary>
/// Reads the ThermalEngine-generated baseline CSV.
///
/// This reader imports existing calculated results.
/// It does not recalculate thermal performance.
/// </summary>
public sealed class ThermalBaselineCsvReader
{
    private static readonly string[] RequiredColumns =
    {
        "RecordId",
        "CandidateId",
        "AssemblyName",
        "CandidateStatus",
        "AssemblyFamilyGroup",
        "SubstrateProductId",
        "InsulationProductId",
        "InsulationVariantId",
        "InteriorLiningProductId",
        "NumberOfLayers",
        "TotalThicknessMm",
        "MinConductivityWmK",
        "MaxConductivityWmK",
        "MeanConductivityWmK",
        "InsulationThicknessMm",
        "Layer1ThicknessMm",
        "Layer1ConductivityWmK",
        "Layer2ThicknessMm",
        "Layer2ConductivityWmK",
        "Layer3ThicknessMm",
        "Layer3ConductivityWmK",
        "UValue_Wm2K",
        "IsThermalCalculationValid",
        "HasCompleteConductivityFeatures",
        "ConductivityProvenanceSummary",
        "ResistanceScopeSummary",
        "SourceIds",
        "QualityWarnings",
        "TargetProvenance",
        "TrainingUse"
    };

    public List<AssemblyMLRecord> Read(string csvPath)
    {
        if (string.IsNullOrWhiteSpace(csvPath))
            throw new ArgumentException(
                "CSV path cannot be empty.",
                nameof(csvPath));

        if (!File.Exists(csvPath))
            throw new FileNotFoundException(
                "Thermal baseline CSV was not found.",
                csvPath);

        var records = new List<AssemblyMLRecord>();

        using var reader = new StreamReader(
            csvPath,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);

        string? headerLine = reader.ReadLine();

        if (string.IsNullOrWhiteSpace(headerLine))
            throw new InvalidDataException(
                "Thermal baseline CSV is empty.");

        List<string> headers = ParseCsvLine(headerLine);

        ValidateHeaders(headers);

        var columnIndexes = headers
            .Select((name, index) => new { name, index })
            .ToDictionary(
                x => x.name,
                x => x.index,
                StringComparer.OrdinalIgnoreCase);

        int lineNumber = 1;

        while (!reader.EndOfStream)
        {
            string? line = reader.ReadLine();

            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                List<string> values = ParseCsvLine(line);

                if (values.Count != headers.Count)
                {
                    throw new InvalidDataException(
                        $"Expected {headers.Count} columns, " +
                        $"but received {values.Count}.");
                }

                string Get(string column)
                {
                    return values[columnIndexes[column]].Trim();
                }

                records.Add(new AssemblyMLRecord
                {
                    // Identification
                    RecordId = Get("RecordId"),
                    CandidateId = Get("CandidateId"),
                    AssemblyName = Get("AssemblyName"),
                    CandidateStatus = Get("CandidateStatus"),
                    AssemblyFamilyGroup = Get("AssemblyFamilyGroup"),

                    // Products
                    SubstrateProductId = Get("SubstrateProductId"),
                    InsulationProductId = Get("InsulationProductId"),
                    InsulationVariantId = Get("InsulationVariantId"),
                    InteriorLiningProductId = Get("InteriorLiningProductId"),

                    // Assembly properties
                    NumberOfLayers = ParseInt(
                        Get("NumberOfLayers")),

                    TotalThicknessMm = ParseDouble(
                        Get("TotalThicknessMm")),

                    MinConductivityWmK = ParseNullableDouble(
                        Get("MinConductivityWmK")),

                    MaxConductivityWmK = ParseNullableDouble(
                        Get("MaxConductivityWmK")),

                    MeanConductivityWmK = ParseNullableDouble(
                        Get("MeanConductivityWmK")),

                    InsulationThicknessMm = ParseNullableDouble(
                        Get("InsulationThicknessMm")),

                    // Layer 1
                    Layer1ThicknessMm = ParseNullableDouble(
                        Get("Layer1ThicknessMm")),

                    Layer1ConductivityWmK = ParseNullableDouble(
                        Get("Layer1ConductivityWmK")),

                    // Layer 2
                    Layer2ThicknessMm = ParseNullableDouble(
                        Get("Layer2ThicknessMm")),

                    Layer2ConductivityWmK = ParseNullableDouble(
                        Get("Layer2ConductivityWmK")),

                    // Layer 3
                    Layer3ThicknessMm = ParseNullableDouble(
                        Get("Layer3ThicknessMm")),

                    Layer3ConductivityWmK = ParseNullableDouble(
                        Get("Layer3ConductivityWmK")),

                    // Thermal performance
                    UValue_Wm2K = ParseNullableDouble(
                        Get("UValue_Wm2K")),

                    IsThermalCalculationValid = ParseBool(
                        Get("IsThermalCalculationValid")),

                    HasCompleteConductivityFeatures = ParseBool(
                        Get("HasCompleteConductivityFeatures")),

                    // Provenance
                    ConductivityProvenanceSummary =
                        Get("ConductivityProvenanceSummary"),

                    ResistanceScopeSummary =
                        Get("ResistanceScopeSummary"),

                    TargetProvenance =
                        Get("TargetProvenance"),

                    TrainingUse =
                        Get("TrainingUse"),

                    // JSON arrays stored inside CSV fields
                    SourceIds = ParseStringList(
                        Get("SourceIds")),

                    QualityWarnings = ParseStringList(
                        Get("QualityWarnings"))
                });
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(
                    $"Error reading thermal baseline at CSV line {lineNumber}: " +
                    ex.Message,
                    ex);
            }
        }

        return records;
    }

    // ---------------------------------------------------------
    // HEADER VALIDATION
    // ---------------------------------------------------------

    private static void ValidateHeaders(List<string> headers)
    {
        var missingColumns = RequiredColumns
            .Where(required => !headers.Contains(
                required,
                StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (missingColumns.Count > 0)
        {
            throw new InvalidDataException(
                "Thermal baseline CSV is missing required columns: " +
                string.Join(", ", missingColumns));
        }
    }

    // ---------------------------------------------------------
    // VALUE CONVERSION
    // ---------------------------------------------------------

    private static int ParseInt(string value)
    {
        return int.Parse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture);
    }

    private static double ParseDouble(string value)
    {
        double result = double.Parse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture);

        if (!double.IsFinite(result))
            throw new FormatException("Value must be finite.");

        return result;
    }

    private static double? ParseNullableDouble(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return ParseDouble(value);
    }

    private static bool ParseBool(string value)
    {
        return bool.Parse(value);
    }

    private static List<string> ParseStringList(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new List<string>();

        value = value.Trim();

        // Format 1: Standard JSON array
        if (value.StartsWith("["))
        {
            try
            {
                List<string>? jsonResult =
                    JsonSerializer.Deserialize<List<string>>(value);

                if (jsonResult != null)
                    return jsonResult;
            }
            catch (JsonException)
            {
                // Continue to Python-style list parsing.
            }

            // Format 2: Python-style list
            // Example: ['SOURCE_001', 'SOURCE_002']

            if (value.EndsWith("]"))
            {
                string content = value.Substring(1, value.Length - 2);

                var results = new List<string>();
                var current = new StringBuilder();

                char quote = '\0';
                bool escaped = false;

                foreach (char c in content)
                {
                    if (escaped)
                    {
                        current.Append(c);
                        escaped = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escaped = true;
                        continue;
                    }

                    if (quote != '\0')
                    {
                        if (c == quote)
                        {
                            results.Add(current.ToString());
                            current.Clear();
                            quote = '\0';
                        }
                        else
                        {
                            current.Append(c);
                        }

                        continue;
                    }

                    if (c == '\'' || c == '"')
                    {
                        quote = c;
                    }
                    else if (c != ',' && !char.IsWhiteSpace(c))
                    {
                        throw new FormatException(
                            "Unexpected character in list: " + c);
                    }
                }

                if (quote != '\0' || escaped)
                {
                    throw new FormatException(
                        "Incomplete quoted value in list.");
                }

                return results;
            }
        }

        // Format 3: Pipe-separated values
        // Example: SOURCE_001|SOURCE_002

        return value
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .ToList();
    }

    // ---------------------------------------------------------
    // CSV LINE PARSER
    // ---------------------------------------------------------

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();

        bool insideQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char character = line[i];

            if (character == '"')
            {
                if (insideQuotes &&
                    i + 1 < line.Length &&
                    line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }
            }
            else if (character == ',' && !insideQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        if (insideQuotes)
            throw new InvalidDataException(
                "Unclosed quotation mark in CSV row.");

        fields.Add(current.ToString());

        return fields;
    }
}