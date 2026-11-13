// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Withdraws the parser's unknown <c>$context.&lt;root&gt;</c> warning for a root that a <c>provide</c> in scope names, once the whole
/// application is assembled and every level's provided values are known.
/// </summary>
internal sealed class ContextPathValidator(ParserContext context) : ScreenplaySyntaxWalker
{
    readonly Stack<IReadOnlySet<string>> _scopes = new();
    HashSet<string> _everywhere = [];
    string? _gateOwner;

    /// <summary>
    /// Gets the roots a <c>$context.</c> path may start with besides <see cref="ContextExpressionSyntax.KnownRoots"/>.
    /// </summary>
    internal static IEnumerable<string> ReservedRoots => ["audiences"];

    /// <inheritdoc/>
    public override void VisitApplication(ApplicationSyntax syntax)
    {
        _scopes.Push(ContextConditions.Names(syntax.Provides));
        _everywhere = new ProvidedNames().Collect(syntax);
        base.VisitApplication(syntax);
        _scopes.Pop();
    }

    /// <inheritdoc/>
    public override void VisitModule(ModuleSyntax syntax)
    {
        _scopes.Push(ContextConditions.Names(syntax.Provides));
        base.VisitModule(syntax);
        _scopes.Pop();
    }

    /// <inheritdoc/>
    public override void VisitFeature(FeatureSyntax syntax)
    {
        _scopes.Push(ContextConditions.Names(syntax.Provides));
        base.VisitFeature(syntax);
        _scopes.Pop();
    }

    /// <inheritdoc/>
    public override void VisitSlice(SliceSyntax syntax)
    {
        _scopes.Push(ContextConditions.Names(syntax.Provides));
        base.VisitSlice(syntax);
        _scopes.Pop();
    }

    /// <inheritdoc/>
    public override void VisitAvailable(AvailableSyntax syntax)
    {
        _gateOwner = "available when";
        base.VisitAvailable(syntax);
        _gateOwner = null;
    }

    /// <inheritdoc/>
    public override void VisitAvailability(AvailabilitySyntax syntax)
    {
        // A reusable block is read wherever it is used, so a value it reads has to be provided somewhere.
        _gateOwner = $"availability '{syntax.Name}'";
        base.VisitAvailability(syntax);
        _gateOwner = null;
    }

    /// <inheritdoc/>
    public override void VisitAvailabilityRequirement(AvailabilityRequirementSyntax syntax)
    {
        base.VisitAvailabilityRequirement(syntax);
        if (syntax is ConditionAvailabilityRequirementSyntax condition)
        {
            foreach (var comparison in ContextConditions.Comparisons(condition.Condition))
            {
                CheckLeft(comparison);
            }
        }
    }

    /// <inheritdoc/>
    public override void VisitContextExpression(ContextExpressionSyntax syntax)
    {
        base.VisitContextExpression(syntax);
        if (ContextExpressionSyntax.KnownRoots.Contains(syntax.Root))
        {
            return;
        }

        // The parser warned about every root it did not know; a root a provide in scope names is withdrawn here, once every level is known.
        if (ReservedRoots.Contains(syntax.Root) || _scopes.Any(scope => scope.Contains(syntax.Root)) || (_gateOwner?.StartsWith("availability", StringComparison.Ordinal) == true && _everywhere.Contains(syntax.Root)))
        {
            context.Withdraw(DiagnosticCodes.UnknownContextPath, syntax.Location, $"'{syntax.Path}'");
            return;
        }

        if (_gateOwner is not null)
        {
            context.Withdraw(DiagnosticCodes.UnknownContextPath, syntax.Location, $"'{syntax.Path}'");
            ReportUnprovided(syntax.Root, syntax.Path, syntax.Location);
        }
    }

    void CheckLeft(ComparisonConditionSyntax comparison)
    {
        const string prefix = "$context.";
        if (!comparison.Left.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        var path = comparison.Left[prefix.Length..];
        var root = path.Split('.')[0];
        if (!ContextExpressionSyntax.KnownRoots.Contains(root) && !ReservedRoots.Contains(root) && !(_scopes.Any(scope => scope.Contains(root)) || (_gateOwner?.StartsWith("availability", StringComparison.Ordinal) == true && _everywhere.Contains(root))))
        {
            ReportUnprovided(root, path, comparison.Location);
        }
    }

    void ReportUnprovided(string root, string path, SourceLocation location)
    {
        if (_gateOwner is null)
        {
            return;
        }

        // A condition that reads a value nothing provides can never be decided, and an undecided gate stays closed.
        context.Warning(
            DiagnosticCodes.UnprovidedAvailabilityValue,
            $"'{_gateOwner}' reads '$context.{path}' but nothing provides '{root}' {(_gateOwner.StartsWith("availability", StringComparison.Ordinal) ? "anywhere in the application" : "here")} - declare it with 'provide {root} <Type> from <source>'; until then the gate stays closed",
            location);
    }

    static class ContextConditions
    {
        public static IEnumerable<ComparisonConditionSyntax> Comparisons(ConditionSyntax condition) => condition switch
        {
            ComparisonConditionSyntax comparison => [comparison],
            LogicalConditionSyntax logical => Comparisons(logical.Left).Concat(Comparisons(logical.Right)),
            _ => []
        };

        public static HashSet<string> Names(IEnumerable<ProvideSyntax> provides) => [.. provides.Select(provide => provide.Name)];
    }

    sealed class ProvidedNames : ScreenplaySyntaxWalker
    {
        readonly HashSet<string> _names = [];

        public HashSet<string> Collect(ApplicationSyntax application)
        {
            VisitApplication(application);
            return _names;
        }

        public override void VisitProvide(ProvideSyntax syntax)
        {
            _names.Add(syntax.Name);
            base.VisitProvide(syntax);
        }
    }
}
