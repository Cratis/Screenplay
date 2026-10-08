// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_composite_member_in_examples : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        var document = Document("records", "application.play", """
        type Address
          street String
        module Records
          feature Entries
            slice StateChange Record
              command RecordItem
                address Address
              example Input : RecordItem
                address = { "street": "first" }
              specification Recording
                when Input
                  address = { "street": "second" }
        """);
        Workspace = ScreenplayWorkspace.Create("Records", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Records")));
    }

    void Because() => _result = Workspace.ProposeRename(Rename<PropertySyntax>("street", "road"));

    [Fact] void should_accept() => Assert.True(_result.Accepted, string.Join("; ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_rename_the_example_body_and_the_override() => _result.Workspace.Documents.Single().Text.Split("\"road\":", StringSplitOptions.None).Length.ShouldEqual(3);
    [Fact] void should_preserve_values() => _result.Workspace.Documents.Single().Text.ShouldContain("\"second\"");
}
