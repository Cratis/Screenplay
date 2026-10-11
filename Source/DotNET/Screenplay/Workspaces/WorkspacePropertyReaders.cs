// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.Workspaces;

sealed class WorkspacePropertyReaders(WorkspaceSyntaxIndex index, string? newName, Action<WorkspacePropertyMutation> apply)
{
    internal void Rewrite(WorkspaceSyntaxEntry occurrence, WorkspacePropertyLink link)
    {
        var expressions = Expressions(occurrence, link).ToArray();
        if (expressions.Length > 0)
        {
            foreach (var expression in expressions) RewriteExpression(expression);
            return;
        }
        if (RewriteCollection(occurrence, link)) return;
        var member = ReaderMember(occurrence.Node) ?? throw new InvalidWorkspaceAuthoring($"UnsupportedPropertySpan: {occurrence.Kind} at {link.Reference.Location} cannot be rewritten safely.");
        apply(new(occurrence.Handle, member, RenamedPath(link.Reference.Name)));
    }

    static string? SourcePath(SyntaxNode node) => node switch
    {
        PathExpressionSyntax expression => expression.Path,
        ContextExpressionSyntax expression when expression.Path.StartsWith("command.", StringComparison.Ordinal) => expression.Path[8..],
        SourceItemExpressionSyntax expression => $"$.{expression.Path}",
        _ => null
    };

    static string ExpressionPath(SyntaxNode node) => node switch
    {
        PathExpressionSyntax value => value.Path,
        ContextExpressionSyntax value => value.Path,
        SourceItemExpressionSyntax value => value.Path,
        _ => string.Empty
    };

    static string? ReaderMember(SyntaxNode node) => node switch
    {
        ComparisonConditionSyntax => "left",
        ValidationRuleSyntax or PropertyResponseSourceSyntax or UniquePropertyConstraintSyntax => "property",
        QueryParameterSyntax => "name",
        CaptureSyntax => "key",
        _ => null
    };

    IEnumerable<WorkspaceSyntaxEntry> Expressions(WorkspaceSyntaxEntry occurrence, WorkspacePropertyLink link) => index.Entries.Where(entry => SourcePath(entry.Node) == link.Reference.Name &&
        (entry.Handle == occurrence.Handle || WorkspacePropertyPlan.Parents(entry, index).Any(parent => parent.Handle == occurrence.Handle)));

    void RewriteExpression(WorkspaceSyntaxEntry expression) => apply(new(expression.Handle, "path", RenamedPath(ExpressionPath(expression.Node))));

    bool RewriteCollection(WorkspaceSyntaxEntry occurrence, WorkspacePropertyLink link)
    {
        switch (occurrence.Node)
        {
            case CaptureWhenSyntax capture:
                RewriteNames(new(occurrence.Handle, "properties", capture.Properties, link.Reference.Name));
                return true;
            case UniquePropertyConstraintSyntax constraint when constraint.Property != link.Reference.Name:
                RewriteNames(new(occurrence.Handle, "additionalProperties", constraint.AdditionalProperties, link.Reference.Name));
                return true;
            default:
                return false;
        }
    }

    string RenamedPath(string path) => path[..(path.LastIndexOf('.') + 1)] + newName;

    void RewriteNames(NameCollection collection)
    {
        foreach (var (_, ordinal) in collection.Names.Select((name, ordinal) => (name, ordinal)).Where(value => value.name == collection.Expected))
        {
            apply(new(collection.Target, $"{collection.Member}/{ordinal}", newName!));
        }
    }

    sealed record NameCollection(WorkspaceNodeHandle Target, string Member, IEnumerable<string> Names, string Expected);
}

internal sealed record WorkspacePropertyMutation(WorkspaceNodeHandle Target, string Member, string Value);
