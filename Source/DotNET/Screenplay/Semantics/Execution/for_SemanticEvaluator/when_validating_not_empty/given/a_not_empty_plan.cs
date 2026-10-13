// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

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
        Func<ExecutableSemanticModel>[] versions =
        [
            canonical_serialization_golden_vectors.CreateSemanticModel,
            canonical_serialization_golden_vectors.CreateSemanticModelV2,
            canonical_serialization_golden_vectors.CreateSemanticModelV3,
            canonical_serialization_golden_vectors.CreateSemanticModelV4,
            canonical_serialization_golden_vectors.CreateSemanticModelV5,
            canonical_serialization_golden_vectors.CreateSemanticModelV6,
            canonical_serialization_golden_vectors.CreateSemanticModelV7,
            canonical_serialization_golden_vectors.CreateSemanticModelV8,
            canonical_serialization_golden_vectors.CreateSemanticModelV9,
            canonical_serialization_golden_vectors.CreateSemanticModelV10,
            canonical_serialization_golden_vectors.CreateSemanticModelV11,
            canonical_serialization_golden_vectors.CreateSemanticModelV12,
            canonical_serialization_golden_vectors.CreateSemanticModelV13,
            canonical_serialization_golden_vectors.CreateSemanticModelV14,
            canonical_serialization_golden_vectors.CreateSemanticModelV15
        ];
        var slices = _application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).Select(slice => slice.Id).ToArray();
        foreach (var createVersion in versions)
        {
            // Each golden model contains the feature required by its version; execute only the validation slices.
            var baseline = createVersion();
            var application = baseline.Application with
            {
                Concepts = baseline.Application.Concepts.AddRange(_application.Concepts),
                Modules = baseline.Application.Modules.AddRange(_application.Modules)
            };
            var model = ExecutableSemanticModel.Create(baseline.LanguageVersion, baseline.SemanticVersion, application);
            var compilation = SemanticExecutionPlan.Compile(model, slices);
            compilation.Success.ShouldBeTrue();
            var plan = compilation.Plan!;
            var command = plan.Commands.Values.Single(command => command.Name == commandName);
            yield return new SemanticEvaluator().Execute(plan, SemanticWorld.Empty,
                SemanticExecutionRequest.Create(command.Id, [new(command.Properties.Single().Id, value)], []));
        }
    }
}
