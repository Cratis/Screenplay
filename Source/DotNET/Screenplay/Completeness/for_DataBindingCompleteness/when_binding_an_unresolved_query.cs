// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_DataBindingCompleteness;

public class when_binding_an_unresolved_query : given.a_screen
{
    void Establish() => Compile("data R[] via query Missing");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.DataBindings]));

    [Fact] void should_not_guess_the_query_shape() => Findings.ShouldBeEmpty();
}
