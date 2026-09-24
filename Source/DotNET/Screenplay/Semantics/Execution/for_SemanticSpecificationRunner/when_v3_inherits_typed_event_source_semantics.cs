// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_v3_inherits_typed_event_source_semantics : Specification
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
                  projectId = projectId
              command AllocateProject
                projectId Uuid identifier
                produces ProjectAllocated
                  projectId = projectId
              event ProjectRegistered
                projectId Uuid
              event ProjectAllocated
                projectId Uuid
              specification RegisterOnItsSource
                when RegisterProject
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then ProjectRegistered
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        """;

    const string ReducerAttachment =
        """
            slice StateView Count
              readmodel ProjectCount
                id Uuid
                count Int
              query CountById => ProjectCount?
                by id Uuid
              reducer Count => ProjectCount
                on ProjectRegistered
                  file Reducers/Registered.cs
        """;

    const string CodeAttachment =
        """
            slice StateChange ValidateOther
              command ValidateOther
                label String
                validate
                  ```csharp
                  yield return "Invalid label";
                  ```
        """;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_run_a_v2_typed_destination_specification_in_v3(bool withReducer)
    {
        var plan = Compile(withReducer);
        var specification = plan.Specifications.Values.Single(value => value.Name == "RegisterOnItsSource");
        var run = new SemanticSpecificationRunner().Run(plan, specification.Id);

        plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V3);
        run.Failures.ShouldBeEmpty();
        run.Passed.ShouldBeTrue();
        ((SemanticAccepted)run.Execution).Facts.Single().Context!.EventSource.Type.ShouldEqual(specification.ThenEvents.Single().EventSource!.Type);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_reject_a_mismatched_typed_when_event_source_in_v3(bool withReducer)
    {
        var model = Compile(withReducer).Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single(value => value.Name == "Register");
        var specification = slice.Specifications.Single();
        var mismatched = specification with
        {
            When = specification.When! with
            {
                EventSource = specification.When.EventSource! with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text) }
            }
        };
        var application = model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(value => value == slice ? slice with { Specifications = [mismatched] } : value)] }] }]
        };

        var error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V3, SemanticVersion.V3, application));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.ShouldEqual("A specification event source must have the required scalar destination type.");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_reject_a_mismatched_allocated_event_source_type_in_v3(bool withReducer)
    {
        var plan = Compile(withReducer);
        var command = plan.Commands.Values.Single(value => value.Name == "AllocateProject");
        var value = SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var request = SemanticExecutionRequest.Create(command.Id, [new(command.Properties.Single().Id, value)], []) with
        {
            AllocatedIdentities = System.Collections.Immutable.ImmutableDictionary<SemanticId, SemanticValue>.Empty.Add(command.Id, value),
            AllocatedEventSourceType = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text)
        };

        var result = new SemanticEvaluator().Execute(plan, SemanticWorld.Empty, request);
        ((SemanticRejected)result).Details.ShouldEqual("Allocated event source type differs from the command identifier type.");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_require_a_type_for_an_allocated_event_source_in_v3(bool withReducer)
    {
        var plan = Compile(withReducer);
        var command = plan.Commands.Values.Single(value => value.Name == "AllocateProject");
        var value = SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var request = SemanticExecutionRequest.Create(command.Id, [new(command.Properties.Single().Id, value)], []) with
        {
            AllocatedIdentities = System.Collections.Immutable.ImmutableDictionary<SemanticId, SemanticValue>.Empty.Add(command.Id, value)
        };

        var result = new SemanticEvaluator().Execute(plan, SemanticWorld.Empty, request);
        ((SemanticRejected)result).Details.ShouldEqual("A v2 allocated event source requires its declared scalar identity type.");
    }

    static SemanticExecutionPlan Compile(bool withReducer)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        const string key = "typed-event-source-v3";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "Projects.play", Source + "\n" + (withReducer ? ReducerAttachment : CodeAttachment));
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        compilation.Diagnostics.ShouldBeEmpty();
        return SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }
}
