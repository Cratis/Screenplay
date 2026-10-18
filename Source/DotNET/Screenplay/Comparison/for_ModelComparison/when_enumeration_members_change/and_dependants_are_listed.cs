// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change;

public class and_dependants_are_listed : given.an_enumeration
{
    void Establish() => Models(Positional, "paid\n  issued\n  draft");
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_list_the_storing_event_before_the_change() => _result.Dependants.Any(change => change.Changed.SemanticId == Member("draft").Declaration.SemanticId && change.Side == ComparedSide.Before && change.DependantAddress == "Projects.Registration.Register.Registered").ShouldBeTrue();
    [Fact] void should_list_the_storing_event_after_the_change() => _result.Dependants.Any(change => change.Changed.SemanticId == Member("draft").Declaration.SemanticId && change.Side == ComparedSide.After && change.DependantAddress == "Projects.Registration.Register.Registered").ShouldBeTrue();
}
