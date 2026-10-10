// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison;

public class when_a_changed_declaration_has_dependants : given.two_models
{
    void Establish()
    {
        ChangeSource(Source.Replace("event Registered\n        name String", "event Registered\n        name String\n        extra String", StringComparison.Ordinal));
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_report_the_producing_command() => _result.Dependants.Any(change => change.DependantAddress.EndsWith(".Register", StringComparison.Ordinal) && change.Role == "produces").ShouldBeTrue();
    [Fact] void should_identify_the_changed_event() => _result.Dependants.Where(change => change.Role == "produces").All(change => change.Changed.Kind == "Event" && change.Changed.BeforeAddress == "Projects.Registration.Register.Registered" && change.Changed.AfterAddress == "Projects.Registration.Register.Registered").ShouldBeTrue();
}
