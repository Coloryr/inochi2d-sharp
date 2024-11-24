namespace Inochi2dSharp.Core.Nodes;

public class TmpNode : Node
{
    protected override string TypeId()
    {
        return "Tmp";
    }

    public TmpNode(Node parent) : base(parent)
    {

    }
}
