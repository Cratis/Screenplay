// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal enum ValidationOwnerKind
{
    Command,
    Concept
}

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
    /// <param name="owner">The enclosing declaration kind. Only commands admit named-rule intent wrappers.</param>
    /// <returns>The parsed <see cref="ValidateSyntax"/>, or <c>null</c> when the block is malformed.</returns>
    public static ValidateSyntax? Parse(ParserContext context, SourceLine line, ValidationOwnerKind owner)
    {
        if (line.Content == "validate")
        {
            if (TryParseImperative(context, line, out var imperative))
            {
                return imperative;
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

                var rule = owner == ValidationOwnerKind.Concept
                    ? ValidationRuleParser.ParseImpliedSubject(context, child)
                    : ValidationRuleParser.Parse(context, child, owner);
                if (rule is not null)
                {
                    rules.Add(rule);
                }
            }

            return new DeclarativeValidateSyntax(rules, line.Location, requirements);
        }

        if (owner == ValidationOwnerKind.Concept && LineText.FirstWord(line.Content["validate".Length..].TrimStart()) == "checksum")
        {
            var ruleLine = line with { Content = line.Content["validate".Length..].TrimStart() };
            var rule = ValidationRuleParser.ParseImpliedSubject(context, ruleLine);
            return rule is null ? null : new DeclarativeValidateSyntax([rule], line.Location);
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

    static bool IsFileBody(ParserContext context, SourceLine header, SourceLine body)
    {
        if (!FileReferenceParser.IsDirectiveAmongProperties(body) || ValidationRuleParser.HasRuleOperand(body)) return false;
        var lookahead = context.Reader.Fork();
        lookahead.TakeSignificant();
        var next = lookahead.PeekSignificant();
        return next is null || next.Indent <= header.Indent;
    }

    static bool TryParseImperative(ParserContext context, SourceLine line, out CodeValidateSyntax? validation)
    {
        validation = null;
        if (!context.TryPeekChild(line.Indent, out var body)) return false;
        if (IsFileBody(context, line, body))
        {
            context.Reader.TakeSignificant();
            validation = new CodeValidateSyntax(null, line.Location) { File = FileReferenceParser.Parse(context, body) };
            return true;
        }

        if (!body.Content.StartsWith("```", StringComparison.Ordinal)) return false;
        context.Reader.TakeSignificant();
        if (body.Content is not ("```csharp" or "```java"))
        {
            context.Error(DiagnosticCodes.ExpectedCodeFence, "Expected '```csharp' or '```java' after 'validate'", body.Location);
            return true;
        }

        var implementation = CodeBlockParser.Parse(context, body);
        validation = implementation is null ? null : new CodeValidateSyntax(implementation, line.Location);
        return true;
    }
}
