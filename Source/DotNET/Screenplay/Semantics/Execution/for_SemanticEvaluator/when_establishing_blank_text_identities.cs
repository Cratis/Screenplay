// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_establishing_blank_text_identities : Specification
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange Register
              command RegisterProject
                code String identifier
                produces ProjectRegistered
                  for code
              event ProjectRegistered
        """;

    SemanticExecutionPlan _plan = null!;
    SemanticFact _fact = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("blank-text-identities"), "blank-text-identities", "Projects.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var @event = _plan.Events.Values.Single(value => value.Name == "ProjectRegistered");
        var source = SemanticValue.Text("PRJ-1");
        var type = _plan.Commands.Values.Single().Properties.Single().Type;
        _fact = new(@event.Id, source, []) { Context = new(new(type, source)) };
    }

    [Fact] void should_reject_empty_text_identity_in_v2() => AssertRejected(_fact with { Destination = SemanticValue.Text(""), Context = new(_fact.Context!.EventSource with { Value = SemanticValue.Text("") }) });
    [Fact] void should_reject_blank_text_identity_in_v2() => AssertRejected(_fact with { Destination = SemanticValue.Text("   "), Context = new(_fact.Context!.EventSource with { Value = SemanticValue.Text("   ") }) });
    [Fact] void should_accept_nonblank_text_identity() => new SemanticEvaluator().EstablishWorld(_plan, [_fact]).ShouldBeOfExactType<SemanticAccepted>();

    void AssertRejected(SemanticFact fact)
    {
        var result = new SemanticEvaluator().EstablishWorld(_plan, [fact]);
        result.ShouldBeOfExactType<SemanticRejected>();
        ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Contract);
        ReferenceEquals(result.World, SemanticWorld.Empty).ShouldBeTrue();
    }
}
