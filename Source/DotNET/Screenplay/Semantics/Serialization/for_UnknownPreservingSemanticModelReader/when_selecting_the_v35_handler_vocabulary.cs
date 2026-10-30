// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader;

public class when_selecting_the_v35_handler_vocabulary : Specification
{
    [Fact] void should_admit_possible_events_at_35() => EsmBaselineShapes.Fields(35, EsmBaselineShape.Handler).ContainsKey("possibleEvents").ShouldBeTrue();
    [Fact] void should_leave_34_frozen() => EsmBaselineShapes.Fields(34, EsmBaselineShape.Handler).ContainsKey("possibleEvents").ShouldBeFalse();
}
