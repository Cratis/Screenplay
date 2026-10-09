// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal static class WorkspaceInteractionRepairs
{
    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verifyRepair)
    {
        var subjects = index.Entries.Where(entry => entry.Handle.Revision == revision &&
            (diagnostic.Code == DiagnosticCodes.InlineInteractionAlternative
                ? entry.Node is InteractionAlternativeSyntax && entry.Location == diagnostic.Location
                : entry.Node is InteractionBindingSyntax binding && binding.DirectiveLocations.GetValueOrDefault("where") == diagnostic.Location)).ToArray();
        if (subjects.Length != 1 || diagnostic.Severity == DiagnosticSeverity.Information) return [];
        var subject = subjects[0];
        var source = index.Workspace.Documents.Single(document => document.Id == subject.Handle.Document);
        if (WorkspaceSourceTokenizer.Tokenize(source).Tokens.Any(token => token.Kind == WorkspaceSourceTokenKind.Comment && token.Span.Line == diagnostic.Location.Line)) return [];
        var replacement = subject.Node;
        if (subject.Node is InteractionBindingSyntax original)
        {
            var document = index.Entries.Single(entry => entry.Handle.Document == subject.Handle.Document && entry.Node is ApplicationSyntax);
            var condition = InteractionParser.ParseStrictItemCondition(original.Condition!, diagnostic.Location, ((ApplicationSyntax)document.Node).SourceOptions);
            if (condition is null || original.Alternatives.Any() || original.Otherwise is not null) return [];
            replacement = original with
            {
                Condition = null,
                Actions = [],
                Alternatives = [new InteractionAlternativeSyntax(condition, original.Actions, diagnostic.Location)],
                DirectiveLocations = original.DirectiveLocations.Where(entry => entry.Key != "where").ToDictionary()
            };
        }

        var repair = new WorkspaceDiagnosticRepair(diagnostic.Code, subject.Handle, [new ReplaceWorkspaceNode(subject.Handle, subject.Node, replacement)])
        {
            Title = diagnostic.Code == DiagnosticCodes.LegacyInteractionWhere ? "Replace opaque where with an item-condition alternative" : "Expand interaction alternative to block form",
            CanFixAll = false
        };
        return WorkspaceRepairVerification.Discover(index, repair, verifyRepair);
    }
}
