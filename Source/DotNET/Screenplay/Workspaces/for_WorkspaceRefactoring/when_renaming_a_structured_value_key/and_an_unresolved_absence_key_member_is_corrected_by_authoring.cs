// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_a_structured_value_key;

public class and_an_unresolved_absence_key_member_is_corrected_by_authoring : given.a_workspace_with_an_absent_read_model_key
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => CreateWith(Source.Replace("\"part\":\"old\"", "\"missing\":\"old\"", StringComparison.Ordinal));

    void Because()
    {
        var entry = WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Node is ObjectMemberSyntax member && member.Name == "missing");
        _result = Workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.PreserveTrivia,
            Operations = [new ReplaceWorkspaceNode(entry.Handle, entry.Node, ((ObjectMemberSyntax)entry.Node) with { Name = "part" })]
        });
    }

    [Fact] void should_refuse_even_an_explicit_repair_until_binding_is_isolated() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_not_supply_a_write_plan() => _result.WritePlan.ShouldBeNull();
}
