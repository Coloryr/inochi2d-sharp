namespace Inochi2dSharp.Core.Nodes;

[TypeId("Tmp")]
public class TmpNode : Node
{
    public override string TypeId()
    {
        return "Tmp";
    }

    public TmpNode() : this(null)
    {

    }

    public TmpNode(Node? parent = null) : base(parent)
    {

    }
}
