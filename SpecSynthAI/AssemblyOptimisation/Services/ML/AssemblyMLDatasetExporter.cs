using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SpecSynthAI.AssemblyOptimisation.Models.ML;

namespace SpecSynthAI.AssemblyOptimisation.Services.ML;

public sealed class AssemblyMLDatasetExporter
{
    public void ExportCsv(IEnumerable<AssemblyMLRecord> records, string path)
    {
        ArgumentNullException.ThrowIfNull(records);
        ValidatePath(path);

        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var rows = records.ToList();
        var sb = new StringBuilder();
        string[] headers =
        {
            "RecordId","CandidateId","AssemblyName","CandidateStatus","AssemblyFamilyGroup",
            "SubstrateProductId","InsulationProductId","InsulationVariantId","InteriorLiningProductId",
            "NumberOfLayers","TotalThicknessMm","MinConductivityWmK","MaxConductivityWmK",
            "MeanConductivityWmK","InsulationThicknessMm",
            "Layer1ThicknessMm","Layer1ConductivityWmK","Layer2ThicknessMm","Layer2ConductivityWmK",
            "Layer3ThicknessMm","Layer3ConductivityWmK","TotalResistanceM2K_W","UValue_Wm2K",
            "IsThermalCalculationValid","HasCompleteConductivityFeatures",
            "ConductivityProvenanceSummary","ResistanceScopeSummary","SourceIds","QualityWarnings"
        };
        sb.AppendLine(string.Join(",", headers));

        foreach (var r in rows)
        {
            object?[] values =
            {
                r.RecordId,r.CandidateId,r.AssemblyName,r.CandidateStatus,r.AssemblyFamilyGroup,
                r.SubstrateProductId,r.InsulationProductId,r.InsulationVariantId,r.InteriorLiningProductId,
                r.NumberOfLayers,r.TotalThicknessMm,r.MinConductivityWmK,r.MaxConductivityWmK,
                r.MeanConductivityWmK,r.InsulationThicknessMm,
                r.Layer1ThicknessMm,r.Layer1ConductivityWmK,r.Layer2ThicknessMm,r.Layer2ConductivityWmK,
                r.Layer3ThicknessMm,r.Layer3ConductivityWmK,r.TotalResistanceM2K_W,r.UValue_Wm2K,
                r.IsThermalCalculationValid,r.HasCompleteConductivityFeatures,
                r.ConductivityProvenanceSummary,r.ResistanceScopeSummary,
                string.Join("|", r.SourceIds),string.Join("|", r.QualityWarnings)
            };
            sb.AppendLine(string.Join(",", values.Select(FormatCsv)));
        }

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
    }

    public void ExportJson(IEnumerable<AssemblyMLRecord> records, string path)
    {
        ArgumentNullException.ThrowIfNull(records);
        ValidatePath(path);
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        File.WriteAllText(path, JsonSerializer.Serialize(records, options));
    }

    private static string FormatCsv(object? value)
    {
        string text = value switch
        {
            null => "",
            double d => d.ToString("G17", CultureInfo.InvariantCulture),
            float f => f.ToString("G9", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "",
            _ => value.ToString() ?? ""
        };
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    private static void ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Output path cannot be empty.", nameof(path));
    }
}
