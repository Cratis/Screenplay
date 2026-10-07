// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_FieldOriginCompleteness;

public class when_an_entering_event_populates_a_variant : given.a_view
{
    void Establish() => Compile("slice StateView View\n  event Opened\n    name String\n  readmodel Active\n    name String\n  projection P\n    variant Active\n      enters on Opened");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.FieldOrigins]));

    [Fact] void should_accept_automap_from_the_entering_event() => Findings.ShouldBeEmpty();
}
