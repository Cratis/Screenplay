// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_a_structured_value_key;

public class and_a_nested_mapping_member_is_renamed_without_absence : given.a_workspace_with_an_absent_read_model_key
{
    WorkspaceAuthoringResult _intermediate = null!;
    WorkspaceAuthoringResult _leaf = null!;

    void Establish() => CreateWith(Source.Replace(
        Assertion,
        "given readmodel InvoiceView\n          invoiceId = {\"id\":\"first\",\"detail\":{\"part\":\"old\"}}\n        then readmodel InvoiceView\n          invoiceId = {\"id\":\"first\",\"detail\":{\"part\":\"old\"}}",
        StringComparison.Ordinal));

    void Because()
    {
        _intermediate = Workspace.ProposeRename(Rename<PropertySyntax>("detail", "details"));
        _leaf = Workspace.ProposeRename(Rename<PropertySyntax>("part", "segment"));
    }

    [Fact] void should_accept_the_intermediate_rename() => _intermediate.Accepted.ShouldBeTrue();
    [Fact] void should_rewrite_both_intermediate_mapping_members() => Text(_intermediate).Split("{\"id\":\"first\",\"details\":{\"part\":\"old\"}}").Length.ShouldEqual(3);
    [Fact] void should_accept_the_leaf_rename() => _leaf.Accepted.ShouldBeTrue();
    [Fact] void should_rewrite_both_leaf_mapping_members() => Text(_leaf).Split("{\"id\":\"first\",\"detail\":{\"segment\":\"old\"}}").Length.ShouldEqual(3);
}
