namespace Inochi2dSharp.Core;

[AttributeUsage(AttributeTargets.Class)]
public class TypeIdAttribute(string id) : Attribute
{
    public string Id { get; } = id;
}