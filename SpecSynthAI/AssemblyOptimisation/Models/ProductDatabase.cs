using System.Collections.Generic;
using System.Text.Json;

namespace SpecSynthAI.AssemblyOptimisation.Models;

/// <summary>
/// Root object for the SpecSynth open product database JSON.
/// </summary>
public sealed class ProductDatabase
{
    public ProductDatabaseMetadata DatasetMetadata { get; set; } = new();
    public List<ProductRecord> Products { get; set; } = new();

    // Source records are retained as JSON because dataset v1.1.0
    // contains more than one source-record shape.
    public List<JsonElement> Sources { get; set; } = new();
}
