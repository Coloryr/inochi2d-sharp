using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core.Nodes;

public record MaskBinding
{
    public uint maskSrcUUID;
    public MaskingMode mode;
    public Drawable maskSrc;
}
