// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_a_structured_value_key;

public class and_an_absence_key_and_a_mapping_share_an_intermediate_member : given.a_workspace_with_an_absent_read_model_key
{
    const string Mapping =
        "\n      specification KeptInvoice" +
        "\n        given readmodel InvoiceView" +
        "\n          invoiceId = {\"id\":\"second\",\"detail\":{\"part\":\"kept\"}}" +
        "\n        then readmodel InvoiceView" +
        "\n          invoiceId = {\"id\":\"second\",\"detail\":{\"part\":\"kept\"}}";

    WorkspaceAuthoringResult _result = null!;

    void Establish() => CreateWith(Source + Mapping);

    void Because() => _result = Workspace.ProposeRename(Rename<PropertySyntax>("detail", "details"));

    [Fact] void should_accept_the_rename() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_rename_the_declaration() => Text(_result).ShouldContain("  details InvoicePart");
    [Fact] void should_rewrite_the_absence_key_and_keep_its_nested_member() => Text(_result).ShouldContain("{\"id\":\"first\",\"details\":{\"part\":\"old\"}}");
    [Fact] void should_rewrite_both_nested_mappings() => Text(_result).Split("{\"id\":\"second\",\"details\":{\"part\":\"kept\"}}").Length.ShouldEqual(3);
}
