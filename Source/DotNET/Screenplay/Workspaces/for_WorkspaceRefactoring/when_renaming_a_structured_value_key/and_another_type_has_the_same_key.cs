// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_a_structured_value_key;

public class and_another_type_has_the_same_key : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        Concepts = Document("application", "application.play", "type Line\n  sku String\ntype Other\n  sku String");
        Registration = Document("order", "order.play", "module Shop\n  feature Orders\n    slice StateChange Place\n      command Place\n        lines Line[]\n        others Other[]\n      specification Placing\n        when Place\n          lines = [{\"sku\":\"A-1\"}]\n          others = [{\"sku\":\"B-1\"}]");
        Workspace = ScreenplayWorkspace.Create("Shop", [Concepts, Registration], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));
    }

    void Because() => _result = Workspace.ProposeRename(Rename<PropertySyntax>("sku", "stockCode"));

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_rewrite_only_the_owned_key() => _result.Workspace!.Documents.Single(document => document.Id == Registration.Id).Text.Contains("lines = [{\"stockCode\":\"A-1\"}]", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_leave_the_other_type_alone() => _result.Workspace!.Documents.Single(document => document.Id == Registration.Id).Text.Contains("others = [{\"sku\":\"B-1\"}]", StringComparison.Ordinal).ShouldBeTrue();
}
