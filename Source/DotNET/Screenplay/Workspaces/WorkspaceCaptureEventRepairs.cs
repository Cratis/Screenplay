// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

internal static class WorkspaceCaptureEventRepairs
{
    internal static bool Matches(WorkspaceSyntaxEntry entry, Diagnostic diagnostic) =>
        entry.Node is SpecificationSyntax specification && Records(specification).Any(record => record.Location == diagnostic.Location);

    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verifyRepair)
    {
        var subjects = index.Entries.Where(entry => entry.Handle.Revision == revision && Matches(entry, diagnostic)).ToArray();
        if (subjects.Length != 1) return [];
        var subject = subjects[0];
        var original = (SpecificationSyntax)subject.Node;
        var record = Records(original).Single(value => value.Location == diagnostic.Location);
        var captures = index.Entries.Select(entry => entry.Node).OfType<CaptureSyntax>().Where(capture => string.Equals(capture.Name, record.Capture.Split('.')[^1], StringComparison.Ordinal)).ToArray();
        if (captures is not [var capture] || capture.Source is not { } source || !CaptureEventsSource.IsEvents(source)) return [];
        var events = CaptureEventsSource.Events(source).ToArray();
        if (events is not [var setting]) return [];
        var replacement = new SpecificationEventSyntax(setting.Value, record.Record, record.Location);
        var candidate = original.WhenCapture == record
            ? original with { WhenCapture = null, WhenAppended = replacement }
            : original with
            {
                GivenCaptures = [.. original.GivenCaptures.Where(value => value != record)],
                Given = [.. original.Given.Concat([replacement]).OrderBy(value => value.Location.Line)]
            };
        var repair = new WorkspaceDiagnosticRepair(diagnostic.Code, subject.Handle, [new ReplaceWorkspaceNode(subject.Handle, original, candidate)])
        {
            Title = $"Present event '{setting.Value}' instead of capture '{capture.Name}'",
            CanFixAll = false
        };
        if (!verifyRepair) return [repair];
        var proposed = WorkspaceRepairVerification.RequireComments(WorkspaceRepairVerification.Propose(index.Workspace, new WorkspaceAuthoringRequest
        {
            ExpectedRevision = index.Workspace.Revision,
            ExpectedCatalogRevision = index.Workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = repair.RequiredFormatting,
            Operations = repair.Operations
        }));
        return proposed.Accepted ? [repair] : [];
    }

    static IEnumerable<SpecificationCaptureSyntax> Records(SpecificationSyntax specification) =>
        specification.GivenCaptures.Concat(specification.WhenCapture is { } when ? [when] : []);
}
