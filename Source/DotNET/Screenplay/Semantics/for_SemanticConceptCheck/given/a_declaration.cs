// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticConceptCheck.given;

public class a_declaration : Specification
{
    protected static ConceptSyntax Concept(string primitive, GenerationSyntax? generation = null, params ValidationRuleSyntax[] rules) =>
        new("Reference", primitive, [], [], SourceLocation.Start, [new DeclarativeValidateSyntax(rules, SourceLocation.Start)]) { Generation = generation };

    protected static ValidationRuleSyntax Rule(ValidationRuleKind kind, object? value = null) =>
        new(ValidationRuleSyntax.ConceptValue, kind, value is null ? null : new LiteralExpressionSyntax(value, SourceLocation.Start), null, SourceLocation.Start);
}
