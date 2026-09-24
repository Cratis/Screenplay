// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_running_composite_case_insensitive_constraint : Specification
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId Uuid identifier
                code String
                year String
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  code = code
                  year = year
              event ProjectRegistered
                projectId Uuid
                code String
                year String
              constraint UniqueProject
                unique code, year on ProjectRegistered
                ignore casing
                message "Project already exists"
              specification SameCompositeValueIgnoringCase
                when RegisterProject
                  projectId = "7c9e6679-7425-40de-944b-e07fc1f90ae7"
                  code = "ALPHA"
                  year = "2025"
                then error "Project already exists"
        """;

    SemanticExecutionResult _collision;
    SemanticExecutionResult _differentYear;
    bool _compiled;

    void Because()
    {
        const string stableKey = "composite-constraint-vector";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(stableKey), stableKey, "Projects.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        _compiled = compilation.Success;
        if (!_compiled)
        {
            return;
        }

        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var @event = plan.Events.Values.Single(_ => _.Name == "ProjectRegistered");
        var command = plan.Commands.Values.Single(_ => _.Name == "RegisterProject");
        const string first = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
        const string second = "7c9e6679-7425-40de-944b-e07fc1f90ae7";
        var existingValues = @event.Properties.Select(property => new SemanticPropertyValue(
            property.Id,
            SemanticValue.Text(property.Name switch
            {
                "projectId" => first,
                "code" => "alpha",
                _ => "2025"
            })));
        var world = SemanticWorld.Create([new SemanticFact(@event.Id, SemanticValue.Text(first), [.. existingValues])], []);
        SemanticExecutionRequest Request(string year)
        {
            var values = command.Properties.Select(property => new SemanticPropertyValue(
                property.Id,
                SemanticValue.Text(property.Name switch
                {
                    "projectId" => second,
                    "code" => "ALPHA",
                    _ => year
                })));
            return SemanticExecutionRequest.Create(command.Id, [.. values], []);
        }
        var evaluator = new SemanticEvaluator();
        _collision = evaluator.Execute(plan, world, Request("2025"));
        _differentYear = evaluator.Execute(plan, world, Request("2026"));
    }

    [Fact] void should_compile() => _compiled.ShouldBeTrue();
    [Fact] void should_reject_a_case_insensitive_composite_collision() => ((SemanticRejected)_collision).Code.ShouldEqual("UniqueProject");
    [Fact] void should_use_the_declared_message() => ((SemanticRejected)_collision).Details.ShouldEqual("Project already exists");
    [Fact] void should_allow_a_different_composite_value() => _differentYear.ShouldBeOfExactType<SemanticAccepted>();
}
