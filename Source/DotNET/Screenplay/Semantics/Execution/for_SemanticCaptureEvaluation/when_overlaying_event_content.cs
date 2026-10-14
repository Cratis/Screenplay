// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticCaptureEvaluation;

public class when_overlaying_event_content : Specification
{
    readonly SemanticId _name = SemanticId.Create(SemanticKind.Property, "name");
    readonly SemanticId _label = SemanticId.Create(SemanticKind.Property, "label");
    SemanticCaptureRecord _previous;
    SemanticCaptureRecord _current;
    SemanticCaptureRecord _result;

    void Establish()
    {
        _previous = new([
            Field("id", SemanticValue.Text("parcel")),
            Field("retained", SemanticValue.Text("earlier")),
            Field("cleared", SemanticValue.Text("earlier")),
            Field("items", SemanticValue.Array([SemanticValue.Text("old"), SemanticValue.Text("extra")])),
            Field("payload", SemanticValue.Composite([new(_name, SemanticValue.Text("before")), new(_label, SemanticValue.Text("old"))]))]);
        _current = new([
            Field("id", SemanticValue.Text("parcel")),
            Field("cleared", SemanticValue.Null),
            Field("items", SemanticValue.Array([SemanticValue.Text("new")])),
            Field("payload", SemanticValue.Composite([new(_name, SemanticValue.Text("after"))]))]);
    }

    void Because() => _result = SemanticCaptureEvaluation.Overlay(_previous, _current);

    [Fact] void should_keep_absent_properties() => _result.Fields.Single(field => field.Name == "retained").Value.ShouldEqual(SemanticValue.Text("earlier"));
    [Fact] void should_overlay_carried_null() => _result.Fields.Single(field => field.Name == "cleared").Value.ShouldEqual(SemanticValue.Null);
    [Fact] void should_replace_the_whole_list() => ((SemanticArrayValue)_result.Fields.Single(field => field.Name == "items").Value!).Values.ShouldEqual(new[] { SemanticValue.Text("new") });
    [Fact] void should_replace_the_whole_object() => ((SemanticCompositeValue)_result.Fields.Single(field => field.Name == "payload").Value!).Properties.Length.ShouldEqual(1);
    [Fact] void should_not_mutate_the_previous_record() => ((SemanticCompositeValue)_previous.Fields.Single(field => field.Name == "payload").Value!).Properties.Length.ShouldEqual(2);

    static SemanticCaptureField Field(string name, SemanticValue value) => new(name, SemanticCaptureFieldKind.Value) { Value = value };
}
