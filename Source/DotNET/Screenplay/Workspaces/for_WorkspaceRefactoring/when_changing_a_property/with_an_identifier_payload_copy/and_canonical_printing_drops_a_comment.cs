// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property.with_an_identifier_payload_copy;

// Extraction keeps every comment; the property change then prints the touched document canonically,
// which discloses a dropped trailing comment exactly as it does for a standalone event.
public class and_canonical_printing_drops_a_comment : given.an_identifier_payload_copy_workspace
{
    void Establish() => Create(InlineSource.Replace("name String = \"something\"", "name String = \"something\" // why this matters", StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(Request());

    [Fact] void should_accept_with_the_formatting_consent() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_still_extract_the_event() => Result.PropertyReport!.ExtractedInlineEvents.Single().Name.ShouldEqual("Renamed");
    [Fact] void should_disclose_the_dropped_comment() => WorkspaceDroppedComments.In(Result.WritePlan!).Single().Text.ShouldEqual("// why this matters");
    [Fact] void should_warn_about_canonical_printing() => Result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.AuthoringSourceNormalization).ShouldBeTrue();
}
