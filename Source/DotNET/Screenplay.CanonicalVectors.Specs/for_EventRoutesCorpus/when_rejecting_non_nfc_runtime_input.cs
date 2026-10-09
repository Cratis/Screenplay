// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EventRoutesCorpus;

public class when_rejecting_non_nfc_runtime_input : Specification
{
    [Fact]
    void should_refuse_without_normalizing_allocating_or_appending()
    {
        foreach (var form in EventRoutesCorpus.V8.SourceForms)
        {
            var model = when_loading_and_executing_event_routes.Compile(form).Model;
            var command = model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single(command => command.Name == "Text");
            var request = SemanticExecutionRequest.Create(command.Id, [.. command.Properties.Select(property => new SemanticPropertyValue(property.Id, SemanticValue.Text(property.Name == "text" ? "e\u0301" : "other")))], []);
            var result = new SemanticEvaluator().Execute(SemanticExecutionPlan.Compile(model).Plan!, SemanticWorld.Empty, request);
            result.ShouldBeOfExactType<SemanticRejected>();
            var rejected = (SemanticRejected)result;
            rejected.Category.ShouldEqual(SemanticRejectionCategory.Contract);
            rejected.Details.ShouldEqual("Canonical JSON field 'semantic text value' must use Unicode NFC text.");
            rejected.World.Facts.ShouldBeEmpty();
        }
    }
}
