// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Mcp.for_McpIndex;

public class when_resolving_aliases_variants_and_global_name_collisions : Specification
{
    McpSnapshot _snapshot = null!;
    McpSyntaxIndex _index = null!;

    void Establish() => _snapshot = new([given.synthetic_model.Document("views", """
        concept Current : String
        module Billing
          feature Accounts
            slice StateView Browse
              event Opened
              readmodel Current
              projection Current => Current
                from Opened
              projection AliasBuilder => Alias
                from Opened
              projection Lifecycle
                variant Active
                  enters on Opened
              query CurrentAccount => Current
              query AliasedAccount => Alias
              query ActiveAccount => Active
            slice StateView Other
              readmodel Current
              query OtherAccount => Current
        """)]);

    void Because() => _index = _snapshot.Index;

    [Fact] void should_prefer_the_scoped_view_over_a_global_concept() => ResolveQuery("CurrentAccount").Single().Address.ShouldEqual("Billing.Accounts.Browse.Current");
    [Fact] void should_not_conflate_the_shape_and_builder() => ResolveQuery("CurrentAccount").Single().IsImplicit.ShouldBeFalse();
    [Fact] void should_resolve_an_alias_after_logical_declarations_are_final() => ResolveQuery("AliasedAccount").Single().Name.ShouldEqual("Alias");
    [Fact] void should_resolve_a_projection_variant() => ResolveQuery("ActiveAccount").Single().Name.ShouldEqual("Active");
    [Fact] void should_select_the_nearest_slice() => ResolveQuery("OtherAccount").Single().Address.ShouldEqual("Billing.Accounts.Other.Current");
    [Fact] void should_not_invent_a_read_model_for_an_alias_builder() => _index.Find("Billing.Accounts.Browse.AliasBuilder", "ReadModel").ShouldBeEmpty();
    [Fact] void should_not_invent_a_read_model_for_a_variant_builder() => _index.Find("Billing.Accounts.Browse.Lifecycle", "ReadModel").ShouldBeEmpty();
    [Fact] void should_preserve_kind_filtering_before_scope_selection() => _index.Resolve(new("Current", ["Concept"], ["Billing", "Accounts", "Browse"], SourceLocation.Start)).Single().Kind.ShouldEqual("Concept");
    [Fact] void should_keep_all_candidates_at_the_nearest_shared_scope() => _index.Resolve(new("Current", ["ReadModel"], ["Billing", "Accounts"], SourceLocation.Start)).Length.ShouldEqual(2);

    McpDeclaration[] ResolveQuery(string name) => _index.Resolve(_index.References.Single(reference => reference.Role == "queryResult" && reference.Owner.Name == name));
}
