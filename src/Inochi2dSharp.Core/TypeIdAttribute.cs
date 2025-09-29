namespace Inochi2dSharp.Core;

[AttributeUsage(AttributeTargets.Class)]
public class TypeIdAttribute(string sid, uint nid, bool isAbstract = false) : Attribute
{
    public string Sid { get; init; } = sid;
    public uint Nid { get; init; } = nid;
    public bool IsAbstract { get; init; } = isAbstract;
}