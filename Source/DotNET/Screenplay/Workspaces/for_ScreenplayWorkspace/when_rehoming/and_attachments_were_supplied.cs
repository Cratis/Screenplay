// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.when_rehoming;

public class and_attachments_were_supplied : given.a_workspace_with_assigned_events
{
    ScreenplayWorkspace _rehomed = null!;
    ScreenplayWorkspace _withAttachments = null!;

    void Establish() => Original = Original.WithAttachmentContents(ImmutableDictionary<string, string>.Empty.Add("notes.md", "Host attachment"), []);

    void Because()
    {
        _rehomed = Original.Rehome(Split);
        _withAttachments = _rehomed.WithAttachmentContents(Original.AttachmentContents, Original.AttachmentDiagnostics);
    }

    [Fact] void should_drop_attachments() => _rehomed.AttachmentContents.ShouldBeEmpty();
    [Fact] void should_allow_the_host_to_reapply_attachments() => _withAttachments.AttachmentContents["notes.md"].ShouldEqual("Host attachment");
    [Fact] void should_preserve_the_rehomed_revision_when_reapplying() => _withAttachments.Revision.ShouldEqual(_rehomed.Revision);
}
