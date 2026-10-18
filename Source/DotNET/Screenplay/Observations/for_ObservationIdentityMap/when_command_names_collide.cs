// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationIdentityMap;

public class when_command_names_collide : for_ObservationComparison.given.a_compiled_model
{
    ObservationIdentityMap _created = null!;

    void Establish() => Model(Source + "\n    slice StateChange Another\n      command Register\n        produces Registered\n          name = \"another\"\n");
    void Because() => _created = ObservationIdentityMap.FromModel(_compilation.Model);

    [Fact] void should_leave_both_commands_unmapped() => _created.Producers.ShouldBeEmpty();
    [Fact] void should_disclose_both_ambiguous_identities() => _created.AmbiguousConstructs.Count.ShouldEqual(2);
}
