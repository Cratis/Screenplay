// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_describing_an_unbound_handler_with_composite_properties : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("""
        concept Code : String
        type Details
          code Code
        type Order
          details Details
        module Billing
          feature Accounts
            slice StateChange Commands
              command Deposit
                orders Order[]
                handler
                  file Handler.cs
        """);

    [Fact] void should_keep_the_compilation_unbound() => _result.Success.ShouldBeFalse();
    [Fact] void should_include_the_transitive_type_definitions()
    {
        var descriptor = _result.TypedContextDescriptors.Single();
        descriptor.Types.Select(value => value.Name).ShouldContainOnly(["Order", "Details", "Code"]);
        descriptor.Types[0].Properties[0].Type.Target.ShouldEqual(descriptor.Types[1].Id);
        descriptor.Types[1].Properties[0].Type.Target.ShouldEqual(descriptor.Types[2].Id);
        descriptor.Members[0].Type.Properties[0].Type.IsCollection.ShouldBeTrue();
        descriptor.IsWrapperReady.ShouldBeFalse();
    }
}
