namespace Inochi2dSharp.View;

public record ModelPart
{
    public string Name { get; init; }
    public uint UUID { get; init; }
    public string Type { get; init; }
    public float ZSort { get; init; }
    public List<ModelPart> Children { get; init; }
}
