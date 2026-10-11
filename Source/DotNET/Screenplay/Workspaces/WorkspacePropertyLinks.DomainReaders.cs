// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspacePropertyLinks
{
    static string? LookupPath(SyntaxNode node) => node switch
    {
        ScreenDataSyntax data => data.By,
        ScreenNavigateSyntax navigate => navigate.By,
        FormPopulateViaQuerySyntax populate => populate.By,
        _ => null
    };

    void IndexNamedReader(WorkspaceSyntaxEntry entry)
    {
        if (LookupPath(entry.Node) is { } path) Add(entry, "key", new(path, SourceMembers(entry, path), Incomplete: NestedSource(entry, path)));
        IndexCaptureReader(entry);
        switch (entry.Node)
        {
            case UniquePropertyConstraintSyntax constraint:
                IndexConstraintReader(entry, constraint);
                break;
            case PropertyResponseSourceSyntax response:
                Add(entry, "response", new(response.Property, SourceMembers(entry, response.Property), Incomplete: NestedSource(entry, response.Property)));
                break;
            case ProducesSyntax { For: { } identity }:
                foreach (var source in ExpressionSources(identity, entry))
                {
                    Add(source.Entry, "key", new(source.Path, source.Candidates, Incomplete: source.Incomplete || source.Candidates.Length != 1));
                }
                break;
        }
    }

    void IndexConstraintReader(WorkspaceSyntaxEntry entry, UniquePropertyConstraintSyntax constraint)
    {
        var events = Resolve(constraint.Event, ["Event"], entry);
        foreach (var name in new[] { constraint.Property }.Concat(constraint.AdditionalProperties))
        {
            Add(entry, "key", new(name, Members(events, name), Incomplete: events.Length != 1 || name.Contains('.')));
        }
    }

    void IndexCaptureReader(WorkspaceSyntaxEntry entry)
    {
        switch (entry.Node)
        {
            case CaptureSyntax { Key: { } captureKey }:
                var keyPath = captureKey.StartsWith("$.", StringComparison.Ordinal) ? captureKey[2..] : captureKey;
                Add(entry, "key", new(captureKey, CaptureMembers(entry, keyPath), Incomplete: keyPath.Contains('.')));
                break;
            case CaptureWhenSyntax captureWhen:
                foreach (var name in captureWhen.Properties)
                {
                    Add(entry, "condition", new(name, CaptureMembers(entry, name), Incomplete: name.Contains('.')));
                }
                break;
        }
    }
}
