// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal static class WorkspaceEventRepairs
{
    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verifyRepair)
    {
        if (diagnostic.Code == DiagnosticCodes.EventSourceIdInPayload)
        {
            return PayloadCopy(index, revision, diagnostic, verifyRepair);
        }

        var subjects = index.Entries.Where(entry => entry.Handle.Revision == revision &&
            entry.Node is EventSyntax { Id: not null } declaration && declaration.Id == declaration.Name &&
            declaration.DirectiveLocations.GetValueOrDefault("id") == diagnostic.Location).ToArray();
        if (subjects.Length != 1)
        {
            return [];
        }

        var subject = subjects[0];
        var original = (EventSyntax)subject.Node;
        return WorkspaceRepairVerification.Discover(index, new(diagnostic.Code, subject.Handle, [new ReplaceWorkspaceNode(subject.Handle, original, original with { Id = null })]), verifyRepair);
    }

    internal static bool HasConsumers(WorkspaceSyntaxIndex index, WorkspaceDiagnosticRepair repair)
    {
        var mapping = index.Find(repair.Subject)!;
        var production = index.Find(mapping.Parent!)!;
        var declaration = index.Entries.Single(entry => entry.Parent == production.Handle && entry.Node is EventSyntax);
        var references = new WorkspaceReferenceBindings(index);
        return references.Bindings.Any(binding => binding.Reference.Entry.Handle != production.Handle &&
            (binding.Target?.Entry?.Address?.Equals(declaration.Address) == true ||
                binding.Reference.Text.Split('.')[^1] == ((EventSyntax)declaration.Node).Name)) ||
            index.Entries.Any(entry => WorkspaceOpaqueText.Texts(entry.Node) is not null) ||
            !index.Workspace.AttachmentContents.IsEmpty;
    }

    static ImmutableArray<WorkspaceDiagnosticRepair> PayloadCopy(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verifyRepair)
    {
        var subjects = index.Entries.Where(entry => entry.Handle.Revision == revision && entry.Node is PropertyMappingSyntax && entry.Location == diagnostic.Location).ToArray();
        if (subjects.Length != 1 || index.Find(subjects[0].Parent!) is not { Node: ProducesSyntax { InlineEvent: not null } } production)
        {
            return [];
        }

        var subject = subjects[0];
        var mapping = (PropertyMappingSyntax)subject.Node;
        var declaration = index.Entries.Single(entry => entry.Parent == production.Handle && entry.Node is EventSyntax);
        var properties = index.Entries.Where(entry => entry.Parent == declaration.Handle && entry.Node is PropertySyntax property && property.Name == mapping.Property).ToArray();
        if (properties.Length != 1 || properties[0].Address is not { } address)
        {
            return [];
        }

        var property = properties[0];
        var repair = new WorkspaceDiagnosticRepair(diagnostic.Code, subject.Handle, [new RemoveWorkspaceNode(subject.Handle, subject.Node), new RemoveWorkspaceNode(property.Handle, property.Node)])
        {
            Title = "Remove identifier payload copy (changes the event contract)",
            CanFixAll = false,
            RetiredSemanticAddresses = property.SemanticId is null ? [] : [address]
        };
        return WorkspaceRepairVerification.Discover(index, repair, verifyRepair);
    }
}
