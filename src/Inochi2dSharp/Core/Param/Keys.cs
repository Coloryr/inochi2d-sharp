namespace Inochi2dSharp.Core.Param;

public class ParamMergeMode
{
    /// <summary>
    /// Parameters are merged additively
    /// </summary>
    public const string Additive = "Additive";

    /// <summary>
    /// Parameters are merged with a weighted average
    /// </summary>
    public const string Weighted = "Weighted";

    /// <summary>
    /// Parameters are merged multiplicatively
    /// </summary>
    public const string Multiplicative = "Multiplicative";

    /// <summary>
    /// Forces parameter to be given value
    /// </summary>
    public const string Forced = "Forced";

    /// <summary>
    /// Merge mode is passthrough
    /// </summary>
    public const string Passthrough = "Passthrough";
}
