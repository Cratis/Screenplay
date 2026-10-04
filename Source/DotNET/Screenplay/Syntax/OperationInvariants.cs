// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Syntax;

internal static class OperationInvariants
{
    internal static void Validate(SyntaxNode node)
    {
        if (node is ProducesSyntax production)
        {
            if (production.InlineEvent is not null && production.InlineOperation is not null)
                throw new InvalidSyntaxJson("A production cannot declare both an event and an operation.");
            if (production.InlineOperation is { } operation)
            {
                if (production.When is not null || production.Event != operation.Name || production.For is not null || (production.Tags ?? []).Any())
                    throw new InvalidSyntaxJson("An inline operation requires a matching unconditional target without event metadata.");
                var inputs = operation.Inputs.ToArray();
                var mappings = production.Mappings.ToArray();
                if (inputs.Length != mappings.Length || !inputs.Select(input => input.Name).SequenceEqual(mappings.Select(mapping => mapping.Property), StringComparer.Ordinal))
                    throw new InvalidSyntaxJson("Inline operation inputs and mappings must correspond in order.");
            }
        }
        if (node is OperationPhaseSyntax phase)
        {
            if (phase.File is not null && phase.Code is not null) throw new InvalidSyntaxJson("An operation phase has at most one file or inline payload.");
            if (phase.Implementation is not null) ImplementationInvariants.Validate(phase.Implementation);
        }
    }
}
