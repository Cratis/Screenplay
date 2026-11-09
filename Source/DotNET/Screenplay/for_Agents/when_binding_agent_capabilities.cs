// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Agents;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_Agents;

public class when_binding_agent_capabilities : given.an_agent_model
{
    static string BindableModel => "concept InvoiceId : Uuid\n" + Model.Replace("        id String", "        invoiceId InvoiceId", StringComparison.Ordinal);

    [Fact]
    void should_admit_explicit_exposure_as_v44()
    {
        var result = Bind(BindableModel + "\n" + Allowlist);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Value!.Model.LanguageVersion.ShouldEqual(LanguageVersion.V44);
        result.Value.Model.SemanticVersion.ShouldEqual(SemanticVersion.V44);
        var capabilities = AgentCapabilities.From(result)!;
        capabilities.TokenKind.ShouldEqual(AgentTokenKind.Personal);
        capabilities.Operations.Count.ShouldEqual(2);
        capabilities.Operations.Single(operation => operation.Kind == AgentOperationKind.Query).QualifiedName.ShouldEqual("Sales.Invoices.Listing.All");
        capabilities.Operations.Single(operation => operation.Kind == AgentOperationKind.Command).Scope.ShouldEqual(AgentScope.Write);
    }

    [Fact]
    void should_expose_nothing_without_a_block()
    {
        var result = Bind(BindableModel);
        result.Success.ShouldBeTrue();
        result.Value!.Model.Application.Agents.ShouldBeNull();
        AgentCapabilities.From(result).ShouldBeNull();
        (result.Value.Model.SemanticVersion == SemanticVersion.V44).ShouldBeFalse();
    }

    [Fact]
    void should_keep_a_valid_empty_block_distinct_from_absence()
    {
        var result = Bind(BindableModel + "\nagents\n  tokens personal");
        result.Success.ShouldBeTrue();
        AgentCapabilities.From(result)!.Operations.Count.ShouldEqual(0);
    }

    [Theory]
    [InlineData("agents\n  tokens oauth")]
    [InlineData("agents\n  tokens personal\n  query Missing\n    scope read\n    description \"Missing\"")]
    [InlineData("agents\n  tokens personal\n  query Sales.Invoices.Listing.All\n    scope read")]
    void should_not_return_a_valid_subset_after_any_agent_error(string block)
    {
        var result = Bind(BindableModel + "\n" + block);
        result.Success.ShouldBeFalse();
        AgentCapabilities.From(result).ShouldBeNull();
    }

    [Fact]
    void should_not_return_capabilities_after_an_unrelated_error()
    {
        var result = Bind("unknown root\n" + BindableModel + "\n" + Allowlist);
        result.Success.ShouldBeFalse();
        AgentCapabilities.From(result).ShouldBeNull();
    }

    [Fact]
    void should_project_observable_delivery_from_the_bound_query()
    {
        var result = Bind(BindableModel.Replace("=> Invoice[]", "=> observable Invoice[]", StringComparison.Ordinal) + "\n" + Allowlist);
        result.Success.ShouldBeTrue();
        AgentCapabilities.From(result)!.Operations.Single(operation => operation.Kind == AgentOperationKind.Query).IsObservable.ShouldBeTrue();
    }

    [Fact]
    void should_round_trip_the_portable_allowlist()
    {
        var model = Bind(BindableModel + "\n" + Allowlist).Value!.Model;
        var bytes = SemanticModelSerializer.Serialize(model);
        var restored = SemanticModelSerializer.Deserialize(bytes);
        restored.Application.Agents!.Operations.SequenceEqual(model.Application.Agents!.Operations).ShouldBeTrue();
        SemanticModelSerializer.Serialize(restored).SequenceEqual(bytes).ShouldBeTrue();
        restored.Revision.ShouldEqual(model.Revision);
    }

    [Fact]
    void should_refuse_the_new_metadata_in_v41()
    {
        var model = Bind(BindableModel + "\n" + Allowlist).Value!.Model;
        Assert.Throws<InvalidSemanticContract>(() => ExecutableSemanticModel.Create(LanguageVersion.V41, SemanticVersion.V41, model.Application));
        Assert.Throws<InvalidSemanticContract>(() => EsmSchemaV43Support.EnsureSupported(LanguageVersion.V44, SemanticVersion.V44));
    }

    [Fact]
    void should_reject_a_dangling_or_mismatched_target()
    {
        var model = Bind(BindableModel + "\n" + Allowlist).Value!.Model;
        var operation = model.Application.Agents!.Operations[0];
        var invalid = model.Application with { Agents = model.Application.Agents with { Operations = [operation with { Kind = operation.Kind == SemanticAgentOperationKind.Query ? SemanticAgentOperationKind.Command : SemanticAgentOperationKind.Query }] } };
        Assert.Throws<InvalidSemanticContract>(() => ExecutableSemanticModel.Create(LanguageVersion.V44, SemanticVersion.V44, invalid));
    }

    [Fact]
    void should_ignore_a_bound_value_when_the_complete_result_contains_an_error()
    {
        var result = Bind(BindableModel + "\n" + Allowlist);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var rejected = new CompilationResult<SemanticCompilation>(result.Value,
        [
            .. result.Diagnostics,
            new(DiagnosticSeverity.Error, DiagnosticCodes.InvalidAgentsDeclaration, "Rejected source", new(1, 1))
        ]);
        AgentCapabilities.From(rejected).ShouldBeNull();
    }

    [Fact]
    void should_canonicalize_the_allowlist_independently_of_source_order()
    {
        var result = Bind(BindableModel + "\n" + Allowlist);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = result.Value!.Model;
        var application = model.Application with { Agents = model.Application.Agents! with { Operations = [.. model.Application.Agents!.Operations.Reverse()] } };
        var reordered = ExecutableSemanticModel.Create(LanguageVersion.V44, SemanticVersion.V44, application);
        reordered.Revision.ShouldEqual(model.Revision);
        SemanticModelSerializer.Serialize(reordered).SequenceEqual(SemanticModelSerializer.Serialize(model)).ShouldBeTrue();
    }

    [Fact]
    void should_pin_the_v44_canonical_golden()
    {
        var model = Semantics.Serialization.given.canonical_serialization_golden_vectors.CreateSemanticModelV44();
        SemanticModelSerializer.Serialize(model).SequenceEqual(Semantics.Serialization.given.canonical_serialization_golden_vectors.EsmV44Bytes).ShouldBeTrue();
    }

    static CompilationResult<SemanticCompilation> Bind(string source)
    {
        var identities = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Agents"));
        var document = SemanticSourceDocument.Create(identities.ResolveDocument("application"), "application", "application.play", source);
        return new SemanticModelCompiler().Compile("Agents", SemanticDocumentSet.Create([document], identities));
    }
}
