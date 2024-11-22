using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
