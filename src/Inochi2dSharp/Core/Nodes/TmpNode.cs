namespace Inochi2dSharp.Core.Nodes;

[TypeId("Tmp")]
public class TmpNode(I2dCore core, Node? parent = null) : Node(core, parent)
{
    public override string TypeId()
    {
        return "Tmp";
    }
}
