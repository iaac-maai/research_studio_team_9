using System;
using System.Collections.Generic;

using SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Contracts;

namespace SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Integration;

public sealed class UnifiedDatasetIntegrationReport
{
    public string IntegrationVersion { get; set; } = "1.0.0";

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    public int TotalRecordsReceived { get; set; }

    public int StructurallyValidRecords { get; set; }

    public int StructurallyInvalidRecords { get; set; }

    public Dictionary<DatasetPartition, int> PartitionCounts { get; set; } = new();

    public int SyntheticRecords { get; set; }

    public int PreliminaryRecords { get; set; }

    public int NonSyntheticRecords { get; set; }

    public int EligibleRecords { get; set; }

    public int RejectedRecords { get; set; }

    public Dictionary<string, int> RejectionReasons { get; set; } = new();

    public List<UnifiedAssemblyRecord> IntegratedRecords { get; set; } = new();

    public List<string> Warnings { get; set; } = new();

    public bool IsIntegrationSuccessful { get; set; }
}