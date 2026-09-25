// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Contexts;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_describing_typed_contexts : given.a_semantic_binder
{
    const string Source = """
        concept Code : String
          validate
            rule ValidCode
              file Rules/ValidCode.cs
          validate csharp
            ```csharp
            return true;
            ```
        policy Access
          ```csharp
          return true;
          ```
        policy Unused
          ```csharp
          return true;
          ```
        module Billing
          feature Accounts
            slice StateView Balances
              event Deposited
                amount Decimal
              event Deposited generation 2
                current String
              event Withdrawn
                reason String
              readmodel Balance
                id Uuid
              reducer Fold => Balance
                on Deposited
                  file Reducers/Deposited.cs
                on Withdrawn
                  file Reducers/Withdrawn.cs
              query ById => Balance?
                by id Uuid
                authorize Access
            slice StateChange Commands
              command Deposit
                code Code
                authorize Access
                validate csharp
                  ```csharp
                  return true;
                  ```
                validate
                  code rule ValidCode
                    ```csharp
                    return true;
                    ```
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_and_publish_the_independent_contract_revision()
    {
        _result.Success.ShouldBeTrue();
        SemanticTypedContextDescriptor.ContractRevision.ShouldEqual(1u);
    }
    [Fact] void should_pin_context_members_and_order()
    {
        var vector = string.Join('|', _result.TypedContextDescriptors.Select(descriptor =>
            $"{descriptor.Role}:{descriptor.Members[0].Name}/{descriptor.Members[0].Type.Kind}/{descriptor.Members[0].Type.Properties.Length}:{string.Join(',', descriptor.Members.Select(member => member.Name))}"));
        vector.ShouldEqual("RulePredicate:Artifact/model/0:Artifact,Value,Property,Tenant,CausedBy,Occurred,IsWholeArtifact|ConceptValidation:Artifact/model/0:Artifact,Value,Property,Tenant,CausedBy,Occurred,IsWholeArtifact|PolicyPredicate:Artifact/shape/1:Artifact,Subject,Identity,Tenant,Occurred|PolicyPredicate:Artifact/shape/1:Artifact,Subject,Identity,Tenant,Occurred|ReducerTransition:State/shape/1:State,Event,Key,Tenant,Occurred,SequenceNumber,IsFirst|ReducerTransition:State/shape/1:State,Event,Key,Tenant,Occurred,SequenceNumber,IsFirst|CommandValidation:Artifact/shape/1:Artifact,Value,Property,Tenant,CausedBy,Occurred,IsWholeArtifact|RulePredicate:Artifact/shape/1:Artifact,Value,Property,Tenant,CausedBy,Occurred,IsWholeArtifact");
    }
    [Fact] void should_pin_member_types_identities_sources_and_context_versions()
    {
        var bytes = Encoding.UTF8.GetBytes(Vector(_result));
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant().ShouldEqual("72c947430bfb7e0c59c7a368b45b53288a11f40e77d14c7ba91b05b362343e7c");
    }
    [Fact] void should_hold_the_runtime_context_data_surfaces_to_the_vector()
    {
        foreach (var (type, role) in new[]
        {
            (typeof(RuleContext), SemanticImplementationRole.RulePredicate),
            (typeof(PolicyContext), SemanticImplementationRole.PolicyPredicate),
            (typeof(ReducerContext), SemanticImplementationRole.ReducerTransition)
        })
        {
            var actual = type.GetProperties().Select(property => property.Name);
            var expected = _result.TypedContextDescriptors.First(value => value.Role == role).Members.Select(member => member.Name);
            actual.ShouldContainOnly(expected);
        }
    }
    [Fact] void should_pair_policy_use_sites_without_describing_unreferenced_policy()
    {
        var policy = _result.ImplementationRequirements.Single(value => value.Role == SemanticImplementationRole.PolicyPredicate && _result.ContextsFor(value.RequirementId).Length != 0);
        _result.ContextsFor(policy.RequirementId).Length.ShouldEqual(2);
        _result.TypedContextDescriptors.Count(value => value.Role == SemanticImplementationRole.PolicyPredicate).ShouldEqual(2);
        _result.ImplementationRequirements.Count(value => value.Role == SemanticImplementationRole.PolicyPredicate).ShouldEqual(2);
        _result.ContextsFor(policy.RequirementId).Any(value => value.Members[1].Source.Kind == "query-key").ShouldBeTrue();
    }
    [Fact] void should_source_a_command_policy_subject_from_its_identifier()
    {
        var withIdentifier = Bind(Source.Replace("      command Deposit\n        code Code", "      command Deposit\n        id Uuid identifier\n        code Code", StringComparison.Ordinal));
        var policy = withIdentifier.TypedContextDescriptors.Single(value => value.Role == SemanticImplementationRole.PolicyPredicate &&
            value.Members[0].Type.Properties.Any(property => property.Name == "code"));
        policy.Members[1].Source.Kind.ShouldEqual("command-identifier");
        policy.Members[1].Source.SemanticId.ShouldEqual(policy.Members[0].Type.Properties.Single(property => property.Name == "id").Id);
    }
    [Fact] void should_describe_each_transition_with_its_current_event_and_nullable_state()
    {
        var transitions = _result.TypedContextDescriptors.Where(value => value.Role == SemanticImplementationRole.ReducerTransition).ToArray();
        transitions.Length.ShouldEqual(2);
        transitions.All(value => value.Members[0].IsNullable && value.Members[1].Type.Properties.Length == 1).ShouldBeTrue();
        transitions.Select(value => value.Members[1].Type.Properties[0].Name).ShouldContainOnly(["current", "reason"]);
        transitions.All(value => value.Members[^1].IsDerived).ShouldBeTrue();
    }
    [Fact] void should_keep_inline_and_file_context_shapes_identical()
    {
        var inline = Bind(Source.Replace("file Reducers/Deposited.cs", "```csharp\n          return null;\n          ```", StringComparison.Ordinal));
        Vector(inline).ShouldEqual(Vector(_result));
    }
    [Fact] void should_keep_the_vector_across_path_moves_and_repeated_compilation()
    {
        Vector(Bind(Source, displayPath: "moved.play")).ShouldEqual(Vector(_result));
        Vector(Bind(Source)).ShouldEqual(Vector(_result));
    }
    [Fact] void should_keep_the_vector_across_cultures()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            Vector(Bind(Source)).ShouldEqual(Vector(_result));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
    static string Vector(CompilationResult<SemanticCompilation> compilation) => JsonSerializer.Serialize(compilation.TypedContextDescriptors.Select(descriptor => new
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
            ModelType = Type(member.Type.ModelType),
            Shape = member.Type.Shape?.ToString(),
            member.Type.RuntimeToken,
            Properties = member.Type.Properties.Select(property => new { property.Name, Id = property.Id.ToString(), Type = Type(property.Type) }),
            SourceKind = member.Source.Kind,
            SemanticId = member.Source.SemanticId?.ToString(),
            member.Source.Path
        })
    }));

    static object? Type(SemanticTypeReference? type) => type is null ? null : new
    {
        type.Kind,
        type.Primitive,
        Target = type.Target.ToString(),
        type.IsOptional,
        type.IsCollection
    };

    [Fact] void should_leave_v4_canonical_bytes_and_revision_untouched_by_the_sidecar()
    {
        var model = _result.Value!.Model;
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(model)))
            .SequenceEqual(SemanticModelSerializer.Serialize(model)).ShouldBeTrue();
        model.Revision.ShouldEqual(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application).Revision);
        _result.TypedContextDescriptors.All(value => value.ModelRevision == model.Revision).ShouldBeTrue();
    }
}
