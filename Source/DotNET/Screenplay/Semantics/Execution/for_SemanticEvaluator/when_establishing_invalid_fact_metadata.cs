// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_establishing_invalid_fact_metadata : Specification
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange Register
              command RegisterProject
                projectId Uuid identifier
                produces ProjectRegistered
                  for projectId
              event ProjectRegistered
              event ProjectUnproduced
        """;

    SemanticExecutionPlan _plan = null!;
    SemanticFact _fact = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("invalid-fact-metadata"), "invalid-fact-metadata", "Projects.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var @event = _plan.Events.Values.Single(value => value.Name == "ProjectRegistered");
        var source = SemanticValue.Text("00000000-0000-0000-0000-000000000101");
        var type = _plan.Commands.Values.Single().Properties.Single().Type;
        _fact = new(@event.Id, source, []) { Context = new(new(type, source)) };
    }

    [Fact] void should_reject_missing_typed_source_in_v2() => AssertRejected(_fact with { Context = null });
    [Fact] void should_reject_null_identity_without_context_in_v2() => AssertRejected(_fact with { Destination = SemanticValue.Null, Context = null });
    [Fact] void should_reject_null_identity_in_v2() => AssertRejected(_fact with { Destination = SemanticValue.Null, Context = new(_fact.Context!.EventSource with { Value = SemanticValue.Null }) });
    [Fact] void should_reject_empty_identity_in_v2() => AssertRejected(_fact with { Destination = SemanticValue.Text(""), Context = new(_fact.Context!.EventSource with { Value = SemanticValue.Text("") }) });
    [Fact] void should_reject_blank_identity_in_v2() => AssertRejected(_fact with { Destination = SemanticValue.Text("   "), Context = new(_fact.Context!.EventSource with { Value = SemanticValue.Text("   ") }) });
    [Fact] void should_reject_mismatched_declared_source_type() => AssertRejected(_fact with { Context = new(_fact.Context!.EventSource with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text) }) });
    [Fact] void should_reject_event_with_no_declared_producer() => AssertRejected(_fact with { EventContract = _plan.Events.Values.Single(value => value.Name == "ProjectUnproduced").Id });
    [Fact] void should_reject_null_tag() => AssertRejected(_fact with { Tags = [null!] });
    [Fact] void should_reject_blank_tag() => AssertRejected(_fact with { Tags = ["  "] });
    [Fact] void should_accept_valid_typed_source() => new SemanticEvaluator().EstablishWorld(_plan, [_fact]).ShouldBeOfExactType<SemanticAccepted>();

    void AssertRejected(SemanticFact fact)
    {
        var result = new SemanticEvaluator().EstablishWorld(_plan, [fact]);
        result.ShouldBeOfExactType<SemanticRejected>();
        ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Contract);
        ReferenceEquals(result.World, SemanticWorld.Empty).ShouldBeTrue();
    }
}
