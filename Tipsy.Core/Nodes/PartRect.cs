namespace Tipsy.Core.Nodes;

/// <summary>Where an image node's part sits on its texture, in the texture's standard-resolution pixels.</summary>
public readonly record struct PartRect(int U, int V, int Width, int Height);
