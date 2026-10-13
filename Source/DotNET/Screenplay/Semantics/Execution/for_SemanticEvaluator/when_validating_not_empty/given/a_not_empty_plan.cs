// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_validating_not_empty.given;

public class a_not_empty_plan : Specification
{
    const string Source =
        """
        concept Name : String
          validate
            not empty message "Name is required"
        module Names
          feature Registration
            slice StateChange ConceptRule
              command RegisterConcept
                name Name?
            slice StateChange PropertyRule
              command RegisterProperty
                name String?
                validate
                  name not empty message "Name is required"
            slice StateChange CollectionRule
              command RegisterCollection
                names String[]
                validate
                  names not empty message "Names are required"
        """;

    SemanticApplication _application;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Names"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("not-empty"), "not-empty", "Names.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Names", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        _application = compilation.Value!.Model.Application;
    }

    protected IEnumerable<SemanticExecutionResult> ExecuteEveryVersion(string commandName, SemanticValue value)
    {
        for (uint version = 1; version <= SemanticVersion.V15.Major; version++)
        {
            var model = ExecutableSemanticModel.Create(new(version, 0), new(version, 0), _application);
            var compilation = SemanticExecutionPlan.Compile(model);
            compilation.Success.ShouldBeTrue();
            var plan = compilation.Plan!;
            var command = plan.Commands.Values.Single(command => command.Name == commandName);
            yield return new SemanticEvaluator().Execute(plan, SemanticWorld.Empty,
                SemanticExecutionRequest.Create(command.Id, [new(command.Properties.Single().Id, value)], []));
        }
    }
}
