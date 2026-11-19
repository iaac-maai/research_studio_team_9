namespace SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Contracts;

/// <summary>
/// Declares the intended use of a dataset record.
/// Eligibility is decided by DatasetProvenanceGuard.
/// </summary>
public enum DataUsePurpose
{
    ProductPropertyAnalysis = 1,
    BaselineExperiment = 2,
    PipelineValidation = 3,
    SensitivityAnalysis = 4,
    OptimisationDemonstration = 5,
    PreliminaryEvidenceReview = 6,
    VerifiedProductRelease = 7,
    EmpiricalModelTraining = 8
}
