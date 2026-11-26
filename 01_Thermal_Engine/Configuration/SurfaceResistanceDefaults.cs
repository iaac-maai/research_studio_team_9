namespace ThermalEngine.Configuration;

/// <summary>
/// Represents the direction of heat flow through a building element.
/// </summary>
public enum HeatFlowDirection
{
    Horizontal,
    Upward,
    Downward
}

/// <summary>
/// Stores internal and external surface resistance values.
/// Units: m²·K/W.
/// </summary>
public sealed record SurfaceResistanceValues(
    double InternalResistance,
    double ExternalResistance
);

/// <summary>
/// Provides conventional surface resistance defaults
/// for standard calculation conditions.
/// </summary>
public static class SurfaceResistanceDefaults
{
    // External wall: horizontal heat flow.
    private static readonly SurfaceResistanceValues Wall =
        new(
            InternalResistance: 0.13,
            ExternalResistance: 0.04
        );

    // Roof: upward heat flow.
    private static readonly SurfaceResistanceValues Roof =
        new(
            InternalResistance: 0.10,
            ExternalResistance: 0.04
        );

    // Floor: downward heat flow.
    private static readonly SurfaceResistanceValues Floor =
        new(
            InternalResistance: 0.17,
            ExternalResistance: 0.04
        );

    /// <summary>
    /// Returns the conventional surface resistance values
    /// for the specified heat-flow direction.
    /// </summary>
    public static SurfaceResistanceValues Get(
        HeatFlowDirection direction)
    {
        return direction switch
        {
            HeatFlowDirection.Horizontal => Wall,

            HeatFlowDirection.Upward => Roof,

            HeatFlowDirection.Downward => Floor,

            _ => throw new ArgumentOutOfRangeException(
                nameof(direction),
                direction,
                "Unsupported heat-flow direction."
            )
        };
    }
}