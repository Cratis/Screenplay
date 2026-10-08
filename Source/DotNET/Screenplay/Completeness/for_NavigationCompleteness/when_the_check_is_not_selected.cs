// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_the_check_is_not_selected : given.screens
{
    void Establish() => Compile("screen Home");
    void Because() => Findings = ModelCompleteness.Check(Compilation, CompletenessChecks.None);

    [Fact] void should_preserve_default_compilation() => Findings.ShouldBeEmpty();
}
