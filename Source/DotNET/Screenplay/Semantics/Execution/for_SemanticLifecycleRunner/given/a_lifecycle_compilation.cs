// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_lifecycles.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.given;

public class a_lifecycle_compilation : Specification
{
    protected ExecutableSemanticModel Model = null!;
    protected SemanticExecutionPlan Plan = null!;
    protected SemanticStateMachine Machine = null!;
    protected ImmutableArray<SemanticFact> Facts;
    protected SemanticValue Key = null!;

    void Establish() => Bind(a_lifecycle_source.Source);

    protected void Bind(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        const string key = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "application.play", source);
        var bound = new SemanticModelBinder(2026).Bind("Billing", new ScreenplayCompiler().Parse(source).Value!, SemanticDocumentSet.Create([document], catalog));
        Model = bound.Value!.Model;
        Plan = SemanticExecutionPlan.CompileForSpecifications(Model).Plan!;
        Machine = Model.Application.StateMachines.Single();
        Facts = [.. Machine.Specifications[0].Given.Select(fact => new SemanticFact(fact.EventContract, fact.EventSource!.Value, fact.Values))];
        Key = Machine.Specifications[0].WhenAppended!.EventSource!.Value;
    }

    protected void ReplaceApplication(SemanticApplication application)
    {
        Model = ExecutableSemanticModel.CreateValidated(Model.LanguageVersion, Model.SemanticVersion, application);
        Plan = SemanticExecutionPlan.CompileForSpecifications(Model).Plan!;
    }

    protected SemanticFact Fact(string name) => new(Plan.Events.Values.Single(candidate => candidate.Name == name).Id, Key, []);
}
