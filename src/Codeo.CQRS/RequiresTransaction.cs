using System;

namespace Codeo.CQRS
{
    /// <summary>
    /// By decorating your command or query with this
    /// attribute, you can enforce transaction validation
    /// without having to call ValidateTransaction. This
    /// is a style preference - both methods are fine,
    /// so it depends on your preference for declarative
    /// vs procedural programming.
    /// </summary>
    public class RequiresTransaction : Attribute
    {
    }
}