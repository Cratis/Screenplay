// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property.with_an_identifier_payload_copy;

public class and_opaque_code_reads_the_copy : given.an_identifier_payload_copy_workspace
{
    const string Validation = "        validate\n          ```csharp\n          return projectId != Guid.Empty;\n          ```\n";

    void Establish() => Create(InlineSource.Replace("        produces event Renamed\n", Validation + "        produces event Renamed\n", StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(Request());

    [Fact] void should_refuse_for_the_unprovable_code() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.OpaquePropertyReference);
    [Fact] void should_retain_the_code_location() => Result.Conflicts.Single().Candidates.ShouldNotBeEmpty();
    [Fact] void should_extract_nothing() => Result.PropertyReport!.ExtractedInlineEvents.ShouldBeEmpty();
    [Fact] void should_not_plan_a_write() => Result.WritePlan.ShouldBeNull();
}
