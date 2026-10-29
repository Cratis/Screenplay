// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public static partial class SemanticConceptCheck
{
    // Private anchors map binder diagnostics to node identity without relying on caller locations,
    // generated source lines or pattern-column arithmetic. No anchor escapes the public result.
    static (ConceptSyntax Concept, Dictionary<int, (int Index, PortableMatchCheck? Pattern)> Targets) Normalize(ConceptSyntax concept)
    {
        var targets = new Dictionary<int, (int Index, PortableMatchCheck? Pattern)>();
        var index = 0;
        SourceLocation Anchor(PortableMatchCheck? pattern = null)
        {
            var line = index + 2;
            targets.Add(line, (index++, pattern));

            return SourceLocation.Start with { Line = line };
        }
        ValidationRuleSyntax Rule(ValidationRuleSyntax rule)
        {
            var pattern = rule.Rule == ValidationRuleKind.Matches ? rule.Value switch
            {
                LiteralExpressionSyntax { Value: string text } => PortableMatchPattern.Check(text),
                PathExpressionSyntax { Path: "email" } => PortableMatchPattern.Check(SemanticMatchPattern.Email),
                _ => null
            } : null;
            var anchor = Anchor(pattern);

            return rule with { Location = anchor, File = rule.File is { } file ? file with { Location = anchor } : null, Code = rule.Code is { } code ? code with { Location = anchor } : null };
        }
        ValidateSyntax Validation(ValidateSyntax validation)
        {
            if (validation is DeclarativeValidateSyntax declarative)
            {
                return declarative with
                {
                    Rules = [.. declarative.Rules.Select(Rule)],
                    Requirements = declarative.Requirements?.Select(requirement => requirement with { Location = Anchor() }).ToArray()
                };
            }
            if (validation is CodeValidateSyntax code)
            {
                var anchor = Anchor();

                return code with
                {
                    Location = anchor,
                    Code = code.Code with { Location = anchor }
                };
            }

            if (validation is FileValidateSyntax file)
            {
                var anchor = Anchor();
                return file with { Location = anchor, File = file.File with { Location = anchor } };
            }

            return validation;
        }
        var generation = concept.Generation is { } source ? source with
        {
            Location = SourceLocation.Start,
            File = source.File is { } file ? file with { Location = SourceLocation.Start } : null,
            Code = source.Code is { } code ? code with { Location = SourceLocation.Start } : null
        } : null;
        var normalized = concept with { Location = SourceLocation.Start, Attributes = [.. concept.Attributes.Select(attribute => attribute with { Location = SourceLocation.Start })], Generation = generation, Validations = concept.Validations?.Select(Validation).ToArray() };

        return (normalized, targets);
    }
}
