// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    internal static bool UsesExactNumberLiterals(SemanticApplication application, SemanticVersion version)
    {
        // Reuse reference validation's value walk, including its route exclusions. Only retained
        // values count; admission-only binding of examples and case values never reaches this walk.
        var context = new ValidationContext(version, application);
        context.RegisterApplication(application);
        context.ValidateReferences(application);

        return context.UsesExactNumberLiterals;
    }

    internal static bool DiffersFromLegacyLowering(decimal value)
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

    static bool ContainsExactNumberLiteral(SemanticValue value) => value switch
    {
        SemanticNumberValue number => DiffersFromLegacyLowering(number.Value),
        SemanticArrayValue array => array.Values.Any(ContainsExactNumberLiteral),
        SemanticCompositeValue composite => composite.Properties.Any(property => ContainsExactNumberLiteral(property.Value)),
        _ => false
    };

    private sealed partial class ValidationContext
    {
        internal bool UsesExactNumberLiterals { get; private set; }
    }
}
