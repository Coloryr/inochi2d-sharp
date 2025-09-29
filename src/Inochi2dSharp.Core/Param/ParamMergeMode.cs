namespace Inochi2dSharp.Core.Param;

public enum ParamMergeMode
{
    /// <summary>
    /// Parameters are merged additively
    /// </summary>
    Additive,
    /// <summary>
    /// Parameters are merged with a weighted average
    /// </summary>
    Weighted,
    /// <summary>
    /// Parameters are merged multiplicatively
    /// </summary>
    Multiplicative,
    /// <summary>
    /// Forces parameter to be given value
    /// </summary>
    Forced,
    /// <summary>
    /// Merge mode is passthrough
    /// </summary>
    Passthrough
}

public static class ParamMergeModeHelper
{
    /// <summary>
    /// Gets a parameter merge mode from its string name
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static ParamMergeMode ToMergeMode(this string value)
    {
        return value switch
        {
            "additive" or "Additive" => ParamMergeMode.Additive,
            "weighted" or "Weighted" => ParamMergeMode.Weighted,
            "multiplicative" or "Multiplicative" => ParamMergeMode.Multiplicative,
            "forced" or "Forced" => ParamMergeMode.Forced,
            "passthrough" or "Passthrough" => ParamMergeMode.Passthrough,
            _ => ParamMergeMode.Passthrough,
        };
    }
}