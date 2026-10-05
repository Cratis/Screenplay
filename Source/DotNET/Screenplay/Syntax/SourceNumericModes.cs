// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Syntax;

/// <summary>Checks numeric provenance at complete-document and programmatic boundaries.</summary>
internal static class SourceNumericModes
{
    static readonly Dictionary<Type, SyntaxDescriptor> _descriptors = SyntaxKinds.All.ToDictionary(descriptor => descriptor.Type);

    internal static IReadOnlyList<Diagnostic> Errors(SyntaxNode root, NumericMode? owningMode = null)
    {
        var errors = new List<Diagnostic>();
        Visit(root, owningMode, 0, errors);
        return errors;
    }

    internal static void Validate(SyntaxNode root, NumericMode? owningMode = null)
    {
        var errors = Errors(root, owningMode);
        if (errors.Count > 0) throw new InvalidSyntaxJson(errors[0].Message);
    }

    internal static SourceOptions Consensus(IReadOnlyList<ApplicationSyntax> applications, Action<Diagnostic> report)
    {
        var asserted = applications.Where(application => application.SourceOptions != SourceOptions.Legacy || HasDeclaration(application)).ToArray();
        var options = asserted.FirstOrDefault()?.SourceOptions ?? SourceOptions.Legacy;
        var invalid = asserted.Any(application => application.SourceOptions is null || !Enum.IsDefined(application.SourceOptions.NumericMode));
        foreach (var application in asserted.Where(application => application.SourceOptions != options))
        {
            invalid = true;
            report(Diagnostic.Error(DiagnosticCodes.MixedNumericModes, "Declaration-bearing documents and marked import barrels must independently select the same numeric mode.", application.Location));
        }

        return invalid ? new((NumericMode)(-1)) : options;
    }

    internal static IEnumerable<SyntaxNode> Children(SyntaxNode node)
    {
        if (!_descriptors.TryGetValue(node.GetType(), out var descriptor)) yield break;
        foreach (var member in descriptor.Members)
        {
            var value = member.Property.GetValue(node);
            if (value is SyntaxNode child)
            {
                yield return child;
            }
            else if (member.ElementType is not null && value is IEnumerable items)
            {
                foreach (var item in items.OfType<SyntaxNode>()) yield return item;
            }
        }
    }

    static bool HasDeclaration(SyntaxNode node)
    {
        if (node is ModuleSyntax { IsPlacement: true } placed &&
            placed.DirectiveLocations.Keys.Any(key => key.StartsWith(DirectiveLocationKeys.PlacementHeaderPrefix, StringComparison.Ordinal)))
        {
            return true;
        }

        if (node is not (ApplicationSyntax or FileImportSyntax or ImportSyntax or ModuleSyntax { IsPlacement: true } or FeatureSyntax { IsPlacement: true }))
        {
            return true;
        }

        return Children(node).Any(HasDeclaration);
    }

    static void Visit(SyntaxNode node, NumericMode? mode, int depth, List<Diagnostic> errors)
    {
        if (depth > 96 && mode == NumericMode.Exact)
        {
            errors.Add(Diagnostic.Error(DiagnosticCodes.IncompatibleNumericSource, "Numeric source syntax exceeds the supported depth of 96.", node.Location));
            return;
        }

        if (node is ISourceSyntax source)
        {
            if (source.SourceOptions is null || !Enum.IsDefined(source.SourceOptions.NumericMode) ||
                (mode is { } owner && source.SourceOptions.NumericMode != owner))
            {
                errors.Add(Diagnostic.Error(DiagnosticCodes.IncompatibleNumericSource, "Malformed or conflicting source numeric options.", node.Location));
                return;
            }

            mode = source.SourceOptions.NumericMode;
        }

        if (mode == NumericMode.Exact && node is RawExpressionSyntax raw && ExactNumber.IsToken(raw.Text))
        {
            errors.Add(Diagnostic.Error(DiagnosticCodes.IncompatibleNumericSource, "An exact numeric operand must be an explicit ExactNumber literal, not opaque numeric text.", raw.Location));
        }

        if (node is LiteralExpressionSyntax literal &&
            ((mode == NumericMode.Legacy && literal.Value is ExactNumber) ||
             (mode == NumericMode.Exact && literal.Value is not (null or string or bool or ExactNumber))))
        {
            errors.Add(Diagnostic.Error(DiagnosticCodes.IncompatibleNumericSource, "Numeric literal provenance disagrees with its source mode; construct an explicit ExactNumber for exact authoring.", literal.RawLocation ?? literal.Location));
        }

        foreach (var child in Children(node)) Visit(child, mode, depth + 1, errors);
    }
}
