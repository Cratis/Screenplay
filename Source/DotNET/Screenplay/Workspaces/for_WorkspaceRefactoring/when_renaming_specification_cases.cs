// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_specification_cases : given.a_refactoring_workspace
{
    const string Source = """
        module M
          feature F
            slice StateChange S
              command Record
                amount Int
                produces Recorded
                  amount = amount
              event Recorded
                amount Int
              specification Recording
                parameter amount Int
                case Small amount = 10
                case Large amount = 100
                when Record amount = case.amount
                then Recorded amount = case.amount
        """;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Records", [Document("records", "application.play", Source)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Records")));

    [Theory]
    [InlineData("table", "Recording", "RecordingAmounts")]
    [InlineData("case", "Small", "Tiny")]
    [InlineData("parameter", "amount", "value")]
    void should_preserve_derived_identities(string kind, string before, string after)
    {
        var request = kind switch
        {
            "table" => Rename<SpecificationSyntax>(before, after),
            "case" => Rename<SpecificationCaseSyntax>(before, after),
            _ => Rename<SpecificationParameterSyntax>(before, after)
        };
        var result = Workspace.ProposeRename(request);
        Assert.True(result.Accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
        var original = Workspace.IdentityCatalog.Semantics.Where(assignment => assignment.Address.Kind == SemanticKind.Specification).Select(assignment => assignment.Id).ToArray();
        result.Workspace.IdentityCatalog.Semantics.Where(assignment => assignment.Address.Kind == SemanticKind.Specification).Select(assignment => assignment.Id).ShouldContainOnly(original);
        if (kind == "parameter") result.Workspace.Documents.Single().Text.ShouldContain("case.value");
    }
}
