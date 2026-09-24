// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_a_structured_value_key;

public class and_the_key_names_the_property : given.a_workspace_with_a_structured_value
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Workspace.ProposeRename(Rename<PropertySyntax>("sku", "stockCode"));

    [Fact] void should_accept_the_property_rename() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_rewrite_both_keys() => _result.Workspace!.Documents.Single(document => document.Id == Order.Id).Text.Split("\"stockCode\"", StringSplitOptions.None).Length.ShouldEqual(3);
    [Fact] void should_keep_the_other_key() => _result.Workspace!.Documents.Single(document => document.Id == Order.Id).Text.Contains("\"quantity\":2", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_comment() => _result.Workspace!.Documents.Single(document => document.Id == Order.Id).Text.Contains("// @public command PlaceOrder", StringComparison.Ordinal).ShouldBeTrue();
}
