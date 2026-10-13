// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>Reports why an event revision may or may not change in place.</summary>
/// <param name="Basis">noBaseline, notInBaseline or baselined.</param>
/// <param name="ReleaseBaselineRevision">The baseline revision, null only without a baseline.</param>
/// <param name="AdmittedIn">The original release label for a frozen revision.</param>
public sealed record WorkspaceEventMutability(string Basis, string? ReleaseBaselineRevision, string? AdmittedIn)
{
    /// <summary>Classifies a catalog event revision against a readable release baseline.</summary>
    /// <param name="baseline">The independently read baseline, or null for legacy callers.</param>
    /// <param name="eventId">The event contract identity.</param>
    /// <param name="revision">The selected revision.</param>
    /// <returns>The mutability evidence; unreadable baselines cannot classify revisions.</returns>
    public static WorkspaceEventMutability For(ReleaseBaselineRead? baseline, EventContractId? eventId, uint revision)
    {
        if (baseline is null || baseline.Status == "absent") return new("noBaseline", null, null);
        if (baseline.Status == "unreadable") throw new InvalidWorkspaceAuthoring("UnreadableReleaseBaseline: event mutability cannot be determined.");
        var entry = baseline.Document!.Entries.SingleOrDefault(entry => entry.Event == eventId?.ToString() && entry.Revision == revision);
        return new(entry is null ? "notInBaseline" : "baselined", baseline.Revision, entry?.AdmittedIn);
    }
}

/// <summary>Reports an event rename's mutability and newly inserted stored-name pin.</summary>
/// <param name="Mutability">The revision's mutability evidence.</param>
/// <param name="PinAdded">Whether the rename added a pin.</param>
public sealed record WorkspaceEventRenameReport(WorkspaceEventMutability Mutability, bool PinAdded);
