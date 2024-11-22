using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Core.Param;

namespace Inochi2dSharp.Core.Nodes.Drivers;

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


    public override void beginUpdate()
    {
        base.beginUpdate();
    }

    public override void update()
    {
        base.update();
    }

    public Parameter[] getAffectedParameters()
    {
        return [];
    }

    public bool affectsParameter(Parameter param)
    {
        foreach (var p in getAffectedParameters())
        {
            if (p.uuid == param.uuid) return true;
        }
        return false;
    }

    public abstract void updateDriver();

    public abstract void reset();

    public void drawDebug()
    {
    }
}
