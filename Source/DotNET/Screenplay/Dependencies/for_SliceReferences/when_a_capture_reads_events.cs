// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Dependencies.for_SliceReferences;

public class when_a_capture_reads_events : Specification
{
    IReadOnlyList<SliceReference> _references;

    void Because()
    {
        var slice = new ScreenplayCompiler().Parse("module M\n  feature F\n    slice Translate T\n      direction inbound\n      capture C\n        source events\n          from Arrived\n          from Delivered\n        key id\n", "capture.play").Value!
            .Modules.Single().Features.Single().Slices.Single();
        _references = SliceReferences.In(slice);
    }

    [Fact] void should_reference_each_consumed_event() => _references.Select(reference => reference.Name).ShouldEqual("Arrived", "Delivered");
    [Fact] void should_resolve_against_events() => _references.ShouldEachConformTo(reference => reference.TargetKind == "Event");
    [Fact] void should_retain_the_source_event_role() => _references.ShouldEachConformTo(reference => reference.Role == "sourceEvent");
}
