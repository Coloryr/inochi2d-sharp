using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.View;

public record ModelAnimation
{
    public string Name { get; init; }
    public bool IsRun { get; init; }
    public int Length { get; init; }
    public int LeadIn { get; init; }
    public int LeadOut { get; init; }
}
