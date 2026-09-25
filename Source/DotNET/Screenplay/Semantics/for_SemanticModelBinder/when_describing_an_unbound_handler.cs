// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
        typeof(CommandContext).GetProperties().Select(property => property.Name).ShouldContainOnly(_result.TypedContextDescriptors.Single().Members.Select(member => member.Name));
    [Fact] void should_pin_the_handler_vector()
    {
        var descriptor = _result.TypedContextDescriptors.Single();
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            descriptor.RequirementId,
            descriptor.Role,
            descriptor.ContextVersion,
            OperationId = descriptor.OperationId?.ToString(),
            ModelRevision = descriptor.ModelRevision?.ToString(),
            Members = descriptor.Members.Select(member => new
            {
                member.Name,
                member.IsNullable,
                member.IsDerived,
                member.Type.Kind,
                Shape = member.Type.Shape?.ToString(),
                member.Type.RuntimeToken,
                Properties = member.Type.Properties.Select(property => new { property.Name, Id = property.Id.ToString(), Type = property.Type.Kind.ToString(), Target = property.Type.Target.ToString(), property.Type.IsOptional }),
                SourceKind = member.Source.Kind,
                SemanticId = member.Source.SemanticId?.ToString(),
                member.Source.Path
            })
        }));
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant().ShouldEqual("db26867396b739e4fdddef401341355f565c65cb83ac7ee1d38a525a842c0634");
    }
}
