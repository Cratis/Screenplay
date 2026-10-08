// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_a_form_is_not_invoked : given.screens
{
    void Establish() => Compile("command Go\nscreen Detail", "form Input for Go\n  on submit navigate to Detail");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_not_make_a_form_an_entry_point() => Findings.Single().Message.ShouldContain("no navigation entry points");
}
