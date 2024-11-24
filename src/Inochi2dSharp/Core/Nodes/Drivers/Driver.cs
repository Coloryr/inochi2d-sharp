using Inochi2dSharp.Core.Param;

namespace Inochi2dSharp.Core.Nodes.Drivers;

[TypeId("Driver")]
public abstract class Driver : Node
{
    protected Driver()
    {

    }

    /// <summary>
    /// Constructs a new Driver node
    /// </summary>
    /// <param name="uuid"></param>
    /// <param name="parent"></param>
    protected Driver(uint uuid, Node? parent = null) : base(uuid, parent)
    {

    }


    public override void BeginUpdate()
    {
        base.BeginUpdate();
    }

    public override void Update()
    {
        base.Update();
    }

    public virtual Parameter[] GetAffectedParameters()
    {
        return [];
    }

    public bool AffectsParameter(Parameter param)
    {
        foreach (var p in GetAffectedParameters())
        {
            if (p.UUID == param.UUID) return true;
        }
        return false;
    }

    public abstract void UpdateDriver();

    public abstract void Reset();

    public virtual void DrawDebug()
    {

    }
}
