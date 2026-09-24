// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>validate</c> blocks - declarative rule sets and <c>validate csharp</c> code blocks - shared
/// by commands and concepts.
/// </summary>
internal static class ValidateParser
{
    /// <summary>
    /// Parses a validate block from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the <c>validate</c> header.</param>
    /// <param name="impliedSubject">Whether the rules omit their property subject - the form used on concepts,
    /// where the concept's own value is implied as <see cref="ValidationRuleSyntax.ConceptValue"/>.</param>
    /// <returns>The parsed <see cref="ValidateSyntax"/>, or <c>null</c> when the block is malformed.</returns>
    public static ValidateSyntax? Parse(ParserContext context, SourceLine line, bool impliedSubject = false)
    {
        if (line.Content == "validate")
        {
            if (context.TryPeekChild(line.Indent, out var fence) && fence.Content.StartsWith("```", StringComparison.Ordinal))
            {
                context.Reader.TakeSignificant();
                if (fence.Content != "```csharp")
                {
                    context.Error(DiagnosticCodes.ExpectedCodeFence, "Expected '```csharp' after 'validate'", fence.Location);
                    return null;
                }

                var implementation = CodeBlockParser.Parse(context, fence);
                return implementation is null ? null : new CodeValidateSyntax(implementation, line.Location);
            }

            var rules = new List<ValidationRuleSyntax>();
            var requirements = new List<RequirementSyntax>();
            while (context.TryPeekChild(line.Indent, out var child))
            {
                context.Reader.TakeSignificant();

                // 'require' states a rule about the whole artifact, so it is a directive rather than a
                // property subject - a property actually named 'require' cannot be ruled on declaratively.
                if (LineText.FirstWord(child.Content) == "require")
                {
                    if (RequirementParser.Parse(context, child) is { } requirement)
                    {
                        requirements.Add(requirement);
                    }

                    continue;
                }

                var rule = impliedSubject
                    ? ValidationRuleParser.ParseImpliedSubject(context, child)
                    : ValidationRuleParser.Parse(context, child);
                if (rule is not null)
                {
                    rules.Add(rule);
                }
            }

            return new DeclarativeValidateSyntax(rules, line.Location, requirements);
        }

        if (line.Content == "validate csharp")
        {
            context.Warning(DiagnosticCodes.LegacyInlineCodeFence, "'validate csharp' is deprecated - use 'validate' followed by '```csharp' instead", line.Location);
            var implementation = CodeBlockParser.ParseFencedBody(context, "csharp", line);
            return implementation is null ? null : new CodeValidateSyntax(implementation, line.Location);
        }

        context.Error(DiagnosticCodes.InvalidValidateDeclaration, $"Invalid validate declaration '{line.Content}' - expected 'validate' or 'validate csharp'", line.Location);
        context.SkipBlock(line.Indent);
        return null;
    }
}
