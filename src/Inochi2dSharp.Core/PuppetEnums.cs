namespace Inochi2dSharp.Core;

public abstract record PuppetEnum(string Data)
{
    public string Data { get; init; } = Data;
}