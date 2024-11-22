using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core;

[AttributeUsage(AttributeTargets.Class)]
public class TypeIdAttribute(string id) : Attribute
{
    public string Id { get; } = id;
}