// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAbsenceKeyBindings;

public class when_binding_flat_key_parts : for_WorkspaceRefactoring.given.a_workspace_with_an_absent_read_model_key
{
    WorkspaceAbsenceKeyObligation[] _obligations;

    void Because()
    {
        CreateWith("""
            module Billing
              feature Invoices
                slice StateView InvoiceLookup
                  readmodel InvoiceView
                    invoice String key
                    method String key
                    total Int
                  query Invoices => InvoiceView[]
                  specification NoInvoice
                    then no readmodel InvoiceView for {"method":"card","invoice":"first"}
            """);
        var index = WorkspaceSyntaxIndex.Create(Workspace);
        _obligations = [.. new WorkspaceAbsenceKeyBindings(index, new WorkspaceReferenceBindings(index)).Obligations];
    }

    [Fact] void should_resolve_the_assertion_and_each_part() => _obligations.Length.ShouldEqual(3);
    [Fact] void should_have_no_unresolved_obligation() => _obligations.All(obligation => obligation.Target is not null).ShouldBeTrue();
    [Fact] void should_bind_the_key_to_its_read_model() => _obligations.Single(obligation => obligation.IsKey).Target!.Node.ShouldBeOfExactType<ReadModelSyntax>();
    [Fact] void should_bind_members_only_to_the_declared_key_properties() => _obligations.Where(obligation => !obligation.IsKey).Select(obligation => ((PropertySyntax)obligation.Target!.Node).Name).ShouldContainOnly("method", "invoice");
}
