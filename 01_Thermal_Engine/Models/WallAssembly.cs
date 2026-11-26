using ThermalEngine.Models;

namespace ThermalEngine.Models;

public class WallAssembly
{
    // Unique name identifying this wall assembly.
    public string AssemblyName { get; set; } = "";

    // Material layers making up this wall.
    public List<ThermalLayer> Layers { get; set; } = new();
}