// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Completes a partial key only when every missing part has one same-named compatible source.
/// </summary>
internal static class WorkspaceReadModelKeyRepairs
{
    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verify)
    {
        var subjects = index.Entries.Where(entry => entry.Handle.Revision == revision && entry.Location == diagnostic.Location && entry.Node is ReadsSyntax).ToArray();
        if (subjects.Length != 1) return [];
        var subject = subjects[0];
        var applications = index.Entries.Select(entry => entry.Node).OfType<ApplicationSyntax>().Select(application => new CompilationResult<ApplicationSyntax>(application, [])).ToArray();
        var application = PlayFolderMerge.Merge(applications).Value!;
        var declarations = new ConsistencyDeclarations(application, [.. ScreenplayValidator.ScopedSlices(application)]);
        var owner = declarations.Slices.Where(entry => entry.Slice.Commands.Any(command => (command.Reads ?? []).Any(read => read.Location == subject.Location))).ToArray();
        if (owner.Length != 1) return [];
        var reads = (ReadsSyntax)subject.Node;
        var key = declarations.ViewKey(reads.ReadModel, owner[0].Scope);
        if (key.Length < 2 || key.Select(part => part.Name).Distinct(StringComparer.Ordinal).Count() != key.Length) return [];
        var command = owner[0].Slice.Commands.Single(command => (command.Reads ?? []).Any(read => read.Location == subject.Location));
        var parts = reads.ByParts.ToList();
        if (reads.By is { } by)
        {
            var part = key.SingleOrDefault(part => part.Name == by);
            var source = command.Properties.Where(property => property.Name == by).ToArray();
            if (part is null || source.Length != 1 || declarations.Compatible(source[0].Type, part.Type) != true) return [];
            parts.Add(new(by, new PathExpressionSyntax(by, reads.Location), reads.Location));
        }
        foreach (var missing in key.Where(part => !parts.Exists(mapping => mapping.Property == part.Name)))
        {
            var source = command.Properties.Where(property => property.Name == missing.Name && !property.Type.IsOptional && !property.Type.IsCollection && declarations.Compatible(property.Type, missing.Type) == true).ToArray();
            if (source.Length != 1) return [];
            parts.Add(new(missing.Name, new PathExpressionSyntax(missing.Name, reads.Location), reads.Location));
        }
        var replacement = reads with { By = null, ByParts = parts };
        var repair = new WorkspaceDiagnosticRepair(diagnostic.Code, subject.Handle, [new ReplaceWorkspaceNode(subject.Handle, subject.Node, replacement)])
        {
            Title = "Supply every read-model key part by name"
        };

        return WorkspaceRepairVerification.Discover(index, repair, verify);
    }
}
