// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>policy</c> declarations with their <c>require</c> conditions or inline code.
/// </summary>
internal static partial class PolicyParser
{
    static readonly LogicalConditionDiagnostics _diagnostics = new(
        DiagnosticCodes.UnexpectedTokenInPolicyCondition,
        DiagnosticCodes.UnclosedPolicyConditionGroup,
        "policy condition");

    /// <summary>
    /// Parses a policy from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>policy</c> header.</param>
    /// <returns>The parsed <see cref="PolicySyntax"/>.</returns>
    public static PolicySyntax Parse(ParserContext context, SourceLine header)
    {
        var name = HeaderRegex().Match(header.Content);
        if (!name.Success)
        {
            context.Error(DiagnosticCodes.InvalidPolicyDeclaration, $"Invalid policy declaration '{header.Content}' - expected 'policy <Name>'", header.Location);
        }

        PolicyConditionSyntax? condition = null;
        var hasRequire = false;
        var depthExceeded = false;
        CodeBlockSyntax? code = null;
        FileReferenceSyntax? file = null;
        string? description = null;
        var locations = new Dictionary<string, SourceLocation>();
        var reads = new List<ReadsSyntax>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (DescriptionParser.TryParse(context, line, ref description, $"Policy '{name.Groups[1].Value}'", locations)) continue;
            if (LineText.FirstWord(line.Content) == "reads")
            {
                if (ReadsParser.Parse(context, line) is { } read) reads.Add(read);
                continue;
            }

            if (LineText.FirstWord(line.Content) == "require")
            {
                var text = line.Content["require".Length..].Trim();
                while (context.TryPeekChild(line.Indent, out var continuation))
                {
                    context.Reader.TakeSignificant();
                    text += $" {continuation.Content}";
                }

                var budget = new ConditionDepthBudget(context, line.Location);
                var parsed = ParseCondition(context, text, line.Location, budget);
                depthExceeded |= budget.Exceeded;
                ValidateAnonymous(context, parsed, name.Groups[1].Value, line.Location);
                if (hasRequire)
                {
                    context.Error(DiagnosticCodes.RepeatedPolicyRequirement, $"Policy '{name.Groups[1].Value}' has more than one require line; combine the conditions with and/or in one require", line.Location);
                }
                else
                {
                    condition = parsed;
                    hasRequire = true;
                }
            }
            else if (FileReferenceParser.IsDirective(line))
            {
                var parsed = FileReferenceParser.Parse(context, line);
                if (code is not null || file is not null)
                {
                    context.Error(DiagnosticCodes.UnknownPolicyDirective, $"Policy '{name.Groups[1].Value}' must have only one implementation - a file reference or an inline code block, not both", line.Location);
                }
                else
                {
                    file = parsed;
                }
            }
            else if (CodeBlockParser.IsCodeLine(context, line))
            {
                var parsed = CodeBlockParser.Parse(context, line);
                if (file is not null || code is not null)
                {
                    context.Error(DiagnosticCodes.UnknownPolicyDirective, $"Policy '{name.Groups[1].Value}' must have only one implementation - a file reference or an inline code block, not both", line.Location);
                }
                else
                {
                    code = parsed;
                }
            }
            else
            {
                context.Error(DiagnosticCodes.UnknownPolicyDirective, $"Unexpected '{line.Content}' in policy body - expected 'reads ...', 'require ...', 'file <path>' or an inline code block", line.Location);
                context.SkipBlock(line.Indent);
            }
        }

        if (condition is not null && (code is not null || file is not null))
        {
            context.Error(DiagnosticCodes.MixedPolicyImplementation, $"Policy '{name.Groups[1].Value}' cannot combine 'require' with a file or inline code block", header.Location);
        }

        if (condition is null && code is null && file is null && !depthExceeded)
        {
            context.Error(DiagnosticCodes.PolicyWithoutRequirement, $"Policy '{name.Groups[1].Value}' must declare a 'require' condition, a file reference or an inline code block", header.Location);
        }

        return new(name.Groups[1].Value, condition, code, header.Location) { File = file, Description = description, DirectiveLocations = locations, Reads = reads.Count == 0 ? null : reads };
    }

    static PolicyConditionSyntax? ParseCondition(ParserContext context, string text, SourceLocation location, ConditionDepthBudget budget) =>
        LogicalConditionParser.Parse<PolicyConditionSyntax>(
            context,
            Tokenize(text, context.SourceOptions.NumericMode),
            location,
            ParseOperand,
            static (left, @operator, right, location) => new LogicalPolicyConditionSyntax(left, @operator, right, location),
            _diagnostics,
            static (operand, location) => new NotPolicyConditionSyntax(operand, location),
            budget);

    static PolicyConditionSyntax? ParseOperand(ParserContext context, IReadOnlyList<string> tokens, ref int position, SourceLocation location)
    {
        if (position >= tokens.Count)
        {
            context.Error(DiagnosticCodes.ExpectedPolicyCondition, "Expected a policy condition", location);
            return null;
        }

        switch (tokens[position])
        {
            case "authenticated":
                position++;
                return new AuthenticatedConditionSyntax(location);

            case "anonymous":
                position++;
                return new AnonymousConditionSyntax(location);

            case "role":
                position++;
                if (position >= tokens.Count || !IsString(tokens[position]))
                {
                    context.Error(DiagnosticCodes.ExpectedRoleName, "Expected a quoted role name after 'role'", location);
                    return null;
                }

                return new RoleConditionSyntax(Unquote(tokens[position++]), location);

            case "claim":
                position++;
                if (position >= tokens.Count || !IsString(tokens[position]))
                {
                    context.Error(DiagnosticCodes.ExpectedClaimName, "Expected a quoted claim name after 'claim'", location);
                    return null;
                }

                var claim = Unquote(tokens[position++]);
                if (position >= tokens.Count || tokens[position] != "matches")
                {
                    context.Error(DiagnosticCodes.ExpectedClaimMatches, "Expected 'matches' after the claim name", location);
                    return null;
                }

                position++;
                if (position >= tokens.Count)
                {
                    context.Error(DiagnosticCodes.ExpectedClaimMatchTarget, "Expected 'subject' or a value after 'matches'", location);
                    return null;
                }

                var target = tokens[position++];
                return target == "subject"
                    ? new ClaimConditionSyntax(claim, true, null, location)
                    : new ClaimConditionSyntax(claim, false, ExpressionParser.ParseMappingSource(context, target, location), location);

            default:
                if (IsStatePath(tokens[position]) && position + 1 < tokens.Count && ConditionParser.ParseOperator(tokens, position + 1, out _) is not null)
                {
                    return ConditionParser.ParseComparison(context, tokens, ref position, location) is ComparisonConditionSyntax comparison
                        ? new StateConditionSyntax(comparison, location)
                        : null;
                }

                context.Error(DiagnosticCodes.UnexpectedTokenInPolicyCondition, $"Unexpected '{tokens[position]}' in policy condition", location);
                return null;
        }
    }

    static void ValidateAnonymous(ParserContext context, PolicyConditionSyntax? condition, string policy, SourceLocation location)
    {
        if (condition is null or AnonymousConditionSyntax || !ContainsAnonymous(condition))
        {
            return;
        }

        if (NegatesAnonymous(condition))
        {
            context.Error(DiagnosticCodes.NegatedAnonymousPolicy, $"Policy '{policy}' negates 'anonymous'; a policy that refuses callers without an identity says 'require authenticated'", location);
            return;
        }

        context.Error(DiagnosticCodes.AnonymousPolicyCombined, $"Policy '{policy}' combines 'anonymous' with another condition; 'anonymous' admits callers without an identity and must be the whole 'require' condition", location);
    }

    static bool ContainsAnonymous(PolicyConditionSyntax condition) => condition switch
    {
        AnonymousConditionSyntax => true,
        NotPolicyConditionSyntax not => ContainsAnonymous(not.Operand),
        LogicalPolicyConditionSyntax logical => ContainsAnonymous(logical.Left) || ContainsAnonymous(logical.Right),
        _ => false
    };

    static bool NegatesAnonymous(PolicyConditionSyntax condition) => condition switch
    {
        NotPolicyConditionSyntax not => ContainsAnonymous(not.Operand),
        LogicalPolicyConditionSyntax logical => NegatesAnonymous(logical.Left) || NegatesAnonymous(logical.Right),
        _ => false
    };

    static bool IsStatePath(string token) => token.Length > 0 && (char.IsLetter(token[0]) || token[0] is '_' or '$');

    static bool IsString(string token) => token.Length >= 2 && token.StartsWith('"') && token.EndsWith('"');

    static string Unquote(string token) => StringLiteral.Unescape(token[1..^1]);

    static List<string> Tokenize(string text, NumericMode mode) =>
        [.. ConditionParser.Tokenize(mode == NumericMode.Exact ? ExactTokenRegex() : TokenRegex(), text)];

    [GeneratedRegex("\"" + StringLiteral.BodyPattern + "\"|==|!=|>=|<=|>|<|\\(|\\)|(?<number>-?[0-9]+(?:\\.[0-9]+)?(?:[eE][+-]?[0-9]+)?)(?:$|[\\s()])|[\\w.$-]+|[^\\s]", RegexOptions.NonBacktracking)]
    private static partial Regex ExactTokenRegex();

    [GeneratedRegex(@"^policy\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex("\"" + StringLiteral.BodyPattern + "\"|==|!=|>=|<=|>|<|\\(|\\)|[\\w.$]+", RegexOptions.NonBacktracking)]
    private static partial Regex TokenRegex();
}
