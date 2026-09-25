// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Contexts;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_describing_an_unbound_handler : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("""
        module Billing
          feature Accounts
            slice StateChange Commands
              command Deposit
                id Uuid identifier
                amount Decimal?
                handler
                  file Handler.cs
        """);

    [Fact] void should_keep_the_handler_unbound() => _result.Success.ShouldBeFalse();
    [Fact] void should_expose_the_shape_of_command_context_v1()
    {
        var descriptor = _result.TypedContextDescriptors.Single();
        descriptor.ContextVersion.ShouldEqual(1u);
        descriptor.Members[0].Type.Properties.Select(value => value.Name).ShouldContainOnly(["id", "amount"]);
        descriptor.Members[0].Type.Properties[1].Type.IsOptional.ShouldBeTrue();
        descriptor.Members.Select(value => value.Name).ShouldContainOnly(["Command", "Tenant", "Identity", "CausedBy", "Causation", "Occurred"]);
    }
    [Fact] void should_hold_the_runtime_command_context_to_the_vector() =>
        given.context_contract_surface.AssertMatches(typeof(CommandContext), _result.TypedContextDescriptors.Single());
    [Fact] void should_report_an_incomplete_compilation() => _result.TypedContextDescriptors.Single().IsWrapperReady.ShouldBeFalse();
    [Fact] void should_pin_the_handler_vector()
    {
        var bytes = SemanticTypedContextSerializer.Serialize(_result.TypedContextDescriptors);
        if (Environment.GetEnvironmentVariable("SCREENPLAY_WRITE_HANDLER_VECTOR") is { } destination) File.WriteAllBytes(destination, bytes);
        using var stream = typeof(when_describing_an_unbound_handler).Assembly.GetManifestResourceStream("Cratis.Screenplay.Semantics.Serialization.Golden.unbound-handler-context-v1.json")!;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        bytes.SequenceEqual(buffer.ToArray()).ShouldBeTrue();
    }
}
