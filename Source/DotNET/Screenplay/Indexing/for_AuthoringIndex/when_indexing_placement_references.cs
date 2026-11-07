// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Indexing.for_AuthoringIndex;

/// <summary>
/// The Screenplay 6 placement forms name declarations: a hook its slice, a lower level the outlet it changes, a picker
/// its query and a slot the screen it shows. Finding references sees each of them.
/// </summary>
public class when_indexing_placement_references : Specification
{
    const string Source = """
        layout AppShell
          category Shell
          content
          outlet topbarActions of actions
            default register title "Register" slice Shop.Customers.RegisterCustomer
          arrangement flow
            row
              content grow

        module Shop
          screen template ListPage
            category Lists
            main
            detail pane routed by customerId optional
            arrangement flow
              row
                main grow
                detail width 320

          form RegisterCustomerForm for RegisterCustomer
            field segment
              choose from query AllSegments show label

          feature Customers
            outlet topbarActions extends
              button rename title "Rename" slice RenameCustomer

            slice StateChange RegisterCustomer
              command RegisterCustomer
                customerId Uuid identifier
                segment String
                produces CustomerRegistered
                  segment = segment
              event CustomerRegistered
                segment String
              screen RegisterCustomer
                title "Register a customer"

            slice StateChange RenameCustomer
              command RenameCustomer
                customerId Uuid identifier
                name String
                produces CustomerRenamed
                  name = name
              event CustomerRenamed
                name String
              screen RenameCustomer
                title "Rename a customer"

            slice StateView Segments
              readmodel Segment
                label String
              query AllSegments => Segment[]

            slice StateView CustomerList
              readmodel CustomerRow
                customerId Uuid
              query AllCustomers => CustomerRow[]
              screen CustomerList
                data CustomerRow[] via query AllCustomers
                template ListPage
                  main
                    table customers
                      column customerId
                  detail
                    show CustomerDetails by customerId from customers.selection

            slice StateView CustomerDetails
              readmodel CustomerDetail
                customerId Uuid
              query CustomerById => CustomerDetail
                by customerId Uuid
              screen CustomerDetails
                data CustomerDetail via query CustomerById by customerId
        """;

    AuthoringIndex _index = null!;

    void Because() => _index = new AuthoringSnapshot(new Dictionary<string, string> { ["application.play"] = Source }).Index;

    string Resolved(string role) => string.Join(" | ", _index.ResolvedReferences.Where(edge => edge.Reference.Role == role)
        .Select(edge => string.Join(',', edge.Candidates.Select(candidate => candidate.Kind + ":" + candidate.Address))));

    [Fact] void should_resolve_hooks_to_their_slices() => Resolved("hook").ShouldEqual("Slice:Shop.Customers.RegisterCustomer | Slice:Shop.Customers.RenameCustomer");
    [Fact] void should_resolve_an_outlet_change_to_the_outlet_it_changes() => Resolved("changesOutlet").ShouldEqual("ContributionPoint:topbarActions");
    [Fact] void should_resolve_a_picker_to_its_query() => Resolved("pickerQuery").ShouldEqual("Query:Shop.Customers.Segments.AllSegments");
    [Fact] void should_resolve_what_a_slot_shows_to_the_screen() => Resolved("show").ShouldEqual("Screen:Shop.Customers.CustomerDetails.CustomerDetails");
}
