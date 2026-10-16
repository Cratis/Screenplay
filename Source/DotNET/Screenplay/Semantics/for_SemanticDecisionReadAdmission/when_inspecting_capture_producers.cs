// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_SemanticDecisionReadAdmission;

public class when_inspecting_capture_producers : Specification
{
    [Fact]
    void should_include_root_child_and_nested_appends_without_inventing_output_routes()
    {
        var model = canonical_serialization_golden_vectors.CreateSemanticModelV25();
        var slices = model.Application.Modules.Single().Features.Single().Slices;
        var command = slices.SelectMany(slice => slice.Commands).Single();
        var produced = command.Produces.Single();
        var text = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
        var append = new SemanticCaptureAppend(produced.EventContract, text, null, [new(produced.Mappings.Single().TargetProperty) { Field = "value" }]);
        var address = SemanticAddress.ForCapture(SemanticAddress.ForSlice(ApplicationIdentity.Create("Projects"), "M", "F", "Import"), "Import");
        var capture = new SemanticCapture(SemanticId.Create(address), "Import", "id", [], [append])
        {
            Children = [new("items", "id", [], [append])],
            Nested = [new("details", [], [append])]
        };
        var source = slices.Single(slice => slice.Name == "Write") with { Commands = [], Captures = [capture] };
        var producers = SemanticDecisionReadAdmission.Producers([source]).ToArray();
        producers.Length.ShouldEqual(3);
        producers.All(producer => producer.EventContract == produced.EventContract && !producer.Routed).ShouldBeTrue();
        var projections = slices.SelectMany(slice => slice.Projections);
        SemanticDecisionReadAdmission.Routed(false, false, projections, producers).ShouldBeFalse();
        SemanticDecisionReadAdmission.Routed(false, false, projections, producers.Select(producer => (producer.EventContract, true))).ShouldBeTrue();
    }
}
