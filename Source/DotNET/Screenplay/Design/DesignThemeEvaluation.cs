// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Design;

/// <summary>
/// Evaluates one theme of a design system in one mode: follows every reference through the palette and other tokens,
/// detecting cycles and unresolved names, then resolves the component parts.
/// </summary>
internal sealed class DesignThemeEvaluation
{
    const string PaletteCategory = "palette";

    readonly DesignSystemSyntax _designSystem;
    readonly ThemeSyntax _theme;
    readonly string? _mode;
    readonly Dictionary<string, DesignAssignmentSyntax> _palette;
    readonly Dictionary<string, DesignAssignmentSyntax> _assignments;
    readonly HashSet<string> _declared;
    readonly Dictionary<string, string?> _resolved = new(StringComparer.Ordinal);
    readonly List<DesignProblem> _problems = [];

    public DesignThemeEvaluation(DesignSystemSyntax designSystem, ThemeSyntax theme, string? mode)
    {
        _designSystem = designSystem;
        _theme = theme;
        _mode = mode;
        _palette = theme.Palette.GroupBy(entry => entry.Name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        _assignments = theme.Values.GroupBy(entry => entry.Name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        if (mode is not null && theme.ModeValues.FirstOrDefault(block => block.Mode == mode) is { } modeBlock)
        {
            foreach (var entry in modeBlock.Values)
            {
                _assignments[entry.Name] = entry;
            }
        }

        _declared = designSystem.Tokens.SelectMany(category => category.Tokens.Select(token => $"{category.Category}.{token.Name}")).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the problems found while resolving.
    /// </summary>
    public IReadOnlyList<DesignProblem> Problems => _problems;

    /// <summary>
    /// Gets the resolved token map: every assigned token that resolved to a value.
    /// </summary>
    public Dictionary<string, string> Tokens { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets a value indicating whether the theme gives a token a value in this mode.
    /// </summary>
    /// <param name="address">The token address.</param>
    /// <returns><see langword="true"/> when assigned.</returns>
    public bool HasValue(string address) => _assignments.ContainsKey(address);

    /// <summary>
    /// Resolves every assigned token and every component part.
    /// </summary>
    /// <returns>The resolved components.</returns>
    public IReadOnlyList<ResolvedDesignComponent> Evaluate()
    {
        foreach (var (address, assignment) in _assignments)
        {
            if (!_declared.Contains(address))
            {
                continue;
            }

            if (ResolveToken(address, assignment, []) is { } value)
            {
                Tokens[address] = value;
            }
        }

        return [.. _designSystem.Components.Select(ResolveComponent)];
    }

    ResolvedDesignComponent ResolveComponent(DesignComponentSyntax component) =>
        new(component.Name, [.. component.Variants.Select(variant =>
        {
            var parts = ResolveParts(variant.Parts);
            var states = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
            foreach (var state in variant.States)
            {
                var merged = new Dictionary<string, string>(parts, StringComparer.Ordinal);
                foreach (var (part, value) in ResolveParts(state.Parts))
                {
                    merged[part] = value;
                }

                states[state.Name] = merged;
            }

            return new ResolvedDesignVariant(variant.Name, parts, states);
        })]);

    Dictionary<string, string> ResolveParts(IEnumerable<DesignAssignmentSyntax> parts)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in parts)
        {
            if (Tokens.TryGetValue(part.Value, out var value))
            {
                result[part.Name] = value;
            }
        }

        return result;
    }

    string? ResolveToken(string address, DesignAssignmentSyntax assignment, List<string> path)
    {
        if (_resolved.TryGetValue(address, out var known))
        {
            return known;
        }

        if (path.Contains(address, StringComparer.Ordinal))
        {
            Report(DiagnosticCodes.DesignReferenceCycle, $"Theme '{_theme.Name}' resolves '{address}' through itself ({string.Join(" -> ", path.SkipWhile(step => step != address).Append(address))})", assignment.Location);
            return null;
        }

        path.Add(address);
        var value = assignment.IsLiteral ? assignment.Value : ResolveReference(assignment, path);
        path.RemoveAt(path.Count - 1);
        _resolved[address] = value;
        return value;
    }

    string? ResolveReference(DesignAssignmentSyntax assignment, List<string> path)
    {
        var reference = assignment.Value;
        if (reference.StartsWith(PaletteCategory + ".", StringComparison.Ordinal))
        {
            if (!_palette.TryGetValue(reference[(PaletteCategory.Length + 1)..], out var entry))
            {
                return Unresolved(assignment, $"palette entry '{reference}' is not declared in theme '{_theme.Name}'");
            }

            return entry.IsLiteral ? entry.Value : ResolveToken(reference, entry, path);
        }

        if (!_declared.Contains(reference))
        {
            return Unresolved(assignment, $"'{reference}' is not a token of design system '{_designSystem.Name}'");
        }

        return _assignments.TryGetValue(reference, out var target)
            ? ResolveToken(reference, target, path)
            : Unresolved(assignment, $"token '{reference}' has no value in theme '{_theme.Name}'{(_mode is null ? string.Empty : $" in mode '{_mode}'")}");
    }

    string? Unresolved(DesignAssignmentSyntax assignment, string reason)
    {
        Report(DiagnosticCodes.UnresolvedDesignReference, $"'{assignment.Name} = {assignment.Value}' cannot be resolved: {reason}", assignment.Location);
        return null;
    }

    void Report(string code, string message, SourceLocation location)
    {
        if (!_problems.Exists(problem => problem.Code == code && problem.Location == location))
        {
            _problems.Add(new(code, message, location));
        }
    }
}

/// <summary>
/// A problem found while resolving a theme.
/// </summary>
/// <param name="Code">The diagnostic code.</param>
/// <param name="Message">The message.</param>
/// <param name="Location">The location the problem is reported at.</param>
internal sealed record DesignProblem(string Code, string Message, SourceLocation Location);
