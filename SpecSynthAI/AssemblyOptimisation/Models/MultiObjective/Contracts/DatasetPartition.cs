namespace SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Contracts;

/// <summary>
/// Identifies the original source partition. A record must retain
/// its partition as it moves through the integration pipeline.
/// </summary>
public enum DatasetPartition
{
    Unknown = 0,
    VerifiedProductCatalogue = 1,
    SyntheticProductCatalogue = 2,
    ThermalEngineBaseline = 3,
    SyntheticMultiObjectiveScenario = 4,
    PreliminaryEPD = 5
}
