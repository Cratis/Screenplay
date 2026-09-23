// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_a_structured_value_key;

public class and_a_producer_uses_the_key : given.a_workspace_with_a_structured_value
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        Order = Document(
            "order",
            "Shop/Orders/PlaceOrder/PlaceOrder.play",
            OrderSource.Replace("{value}", StructuredValue, StringComparison.Ordinal)
                .Replace("lines = lines", "lines = [{\"sku\":\"A-1\",\"quantity\":2}]", StringComparison.Ordinal));
        Workspace = ScreenplayWorkspace.Create("Repro", [Concepts, Order], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Repro")));
    }

    void Because() => _result = Workspace.ProposeRename(Rename<PropertySyntax>("sku", "stockCode"));

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_rewrite_the_producer_and_both_specification_keys() => _result.Workspace!.Documents.Single(document => document.Id == Order.Id).Text.Split("\"stockCode\"", StringSplitOptions.None).Length.ShouldEqual(4);
}
