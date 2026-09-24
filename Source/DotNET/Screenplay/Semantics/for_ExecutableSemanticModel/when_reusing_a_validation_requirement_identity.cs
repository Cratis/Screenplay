// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_reusing_a_validation_requirement_identity : Specification
{
    const string Source =
        """
        concept Label : String
          validate
            rule CheckLabel
              file Rules/CheckLabel.cs
          validate
            ```csharp
            yield return "Invalid label";
            ```
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                label Label
                validate
                  label rule CheckCommand
                    file Rules/CheckCommand.cs
                validate
                  ```csharp
                  yield return "Invalid order";
                  ```
        """;

    [Fact]
    void should_reject_two_command_code_blocks_with_the_same_identity()
    {
        var model = Model();
        var command = Command(model);
        RejectCommand(model, command with { CodeValidations = command.CodeValidations.Add(command.CodeValidations.Single()) });
    }

    [Fact]
    void should_reject_a_command_code_block_reusing_a_predicate_identity()
    {
        var model = Model();
        var command = Command(model);
        RejectCommand(model, command with { CodeValidations = [new(command.Validations.Single().RequirementId!)] });
    }

    [Fact]
    void should_reject_two_command_predicates_with_the_same_identity()
    {
        var model = Model();
        var command = Command(model);
        RejectCommand(model, command with { Validations = command.Validations.Add(command.Validations.Single()) });
    }

    [Fact]
    void should_reject_a_concept_code_block_reusing_a_predicate_identity()
    {
        var model = Model();
        var concept = model.Application.Concepts.Single();
        var rules = concept.Validations;
        RejectConcept(model, concept with { Validations = [rules[0], rules[1] with { RequirementId = rules[0].RequirementId }] });
    }

    [Fact]
    void should_reject_two_concept_predicates_with_the_same_identity()
    {
        var model = Model();
        var concept = model.Application.Concepts.Single();
        RejectConcept(model, concept with { Validations = concept.Validations.Add(concept.Validations[0]) });
    }

    static ExecutableSemanticModel Model()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders"));
        const string key = "duplicate-validation-requirements";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "Orders.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Orders", SemanticDocumentSet.Create([document], catalog));
        compilation.Diagnostics.ShouldBeEmpty();
        return compilation.Value!.Model;
    }

    static SemanticCommand Command(ExecutableSemanticModel model) => model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();

    static void RejectCommand(ExecutableSemanticModel model, SemanticCommand changed)
    {
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [changed] }] }] }] };
        var error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V3, SemanticVersion.V3, application));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.ShouldEqual("Command 'PlaceOrder' has duplicate validation requirement identities.");
    }

    static void RejectConcept(ExecutableSemanticModel model, SemanticConcept changed)
    {
        var application = model.Application with { Concepts = [changed] };
        var error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V3, SemanticVersion.V3, application));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.ShouldEqual("Concept 'Label' has duplicate validation requirement identities.");
    }
}
