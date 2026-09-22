// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.Mcp.for_McpConnection;

public class when_resolving_logical_read_models : given.a_connection
{
    McpSnapshot _snapshot = null!;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), """
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
        """);

    void Because() => _snapshot = new(Root.Read());

    [Fact] void should_not_confuse_a_shape_with_its_same_named_builder() => Resolve("Current").Length.ShouldEqual(1);
    [Fact] void should_resolve_the_declared_shape() => Resolve("Current").Single().IsImplicit.ShouldBeFalse();
    [Fact] void should_resolve_the_output_alias() => Resolve("Alias").Single().Address.ShouldEqual("Billing.Accounts.Browse.Alias");
    [Fact] void should_resolve_the_variant() => Resolve("Active").Single().Address.ShouldEqual("Billing.Accounts.Browse.Active");
    [Fact] void should_not_invent_a_view_for_the_variant_builder() => _snapshot.Index.Declarations.Any(declaration => declaration.Kind == "ReadModel" && declaration.Name == "Lifecycle").ShouldBeFalse();
    [Fact] void should_not_invent_a_view_for_an_aliased_builder() => _snapshot.Index.Declarations.Any(declaration => declaration.Kind == "ReadModel" && declaration.Name == "AliasBuilder").ShouldBeFalse();
    [Fact] void should_keep_the_variant_builder_reference() => _snapshot.Index.References.Single(reference => reference.Role == "buildsVariant").Owner.Name.ShouldEqual("Lifecycle");

    McpDeclaration[] Resolve(string name) => _snapshot.Index.Resolve(_snapshot.Index.References.Single(reference => reference.Name == name && reference.Role == "queryResult"));
}
