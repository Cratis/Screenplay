// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Indexing;

static class SpecificationReferenceRoles
{
    internal static string For(SpecificationSyntax specification, SyntaxNode node, string fallback)
    {
        if (node is SpecificationEventSyntax)
        {
            if (specification.Given.Any(item => ReferenceEquals(item, node)))
            {
                return "givenEvent";
            }

            if (ReferenceEquals(specification.WhenAppended, node))
            {
                return "whenAppendedEvent";
            }

            return "thenEvent";
        }

        if (node is SpecificationReadModelSyntax)
        {
            return (specification.GivenReadModels ?? []).Any(item => ReferenceEquals(item, node)) ? "givenReadModel" : "thenReadModel";
        }

        if (node is SpecificationAbsentReadModelSyntax) return "thenAbsentReadModel";

        return fallback;
    }
}
