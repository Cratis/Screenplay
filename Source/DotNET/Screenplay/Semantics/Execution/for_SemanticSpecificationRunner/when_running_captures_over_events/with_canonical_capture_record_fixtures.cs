// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_canonical_capture_record_fixtures : given.an_event_capture
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_keep_canonical_record_fixtures_valid_but_unsupported(bool history)
    {
        CompileCapture("append Added", """
            when append Arrived
              id = "parcel"
              status = "sent"
            then Added
            """);
        var specification = _plan.Specifications.Values.Single();
        var capture = _plan.Captures.Values.Single();
        var record = new SemanticSpecificationCapture(capture.Id, new([new("id", SemanticCaptureFieldKind.Value) { Value = SemanticValue.Text("parcel") }]));
        ReplaceSpecification(history
            ? specification with { GivenCaptures = [record] }
            : specification with { WhenAppended = null, WhenCapture = record });
        _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_plan.Model))).Plan!;
        _run = Run("S");
        _run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
        _run.Execution.World.Facts.ShouldBeEmpty();
        ((SemanticUnsupported)_run.Execution).Capability.ShouldEqual(SemanticExecutionCapability.Specification);
    }
}
