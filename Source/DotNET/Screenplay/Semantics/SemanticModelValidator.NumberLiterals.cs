// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    static bool ContainsExactNumberLiteral(SemanticValue value) => value switch
    {
        SemanticNumberValue number => DiffersFromLegacyLowering(number.Value),
        SemanticArrayValue array => array.Values.Any(ContainsExactNumberLiteral),
        SemanticCompositeValue composite => composite.Properties.Any(property => ContainsExactNumberLiteral(property.Value)),
        _ => false
    };

    static bool DiffersFromLegacyLowering(decimal value)
    {
        try
        {
            return Convert.ToDecimal((double)value, CultureInfo.InvariantCulture) != value;
        }
        catch (OverflowException)
        {
            // A decimal at the boundary can round outside the decimal range as a double;
            // it cannot have been produced by the legacy lowering either.
            return true;
        }
    }

    private sealed partial class ValidationContext
    {
        internal bool UsesExactNumberLiterals { get; private set; }
    }
}
