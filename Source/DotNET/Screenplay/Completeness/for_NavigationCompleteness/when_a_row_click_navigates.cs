// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_a_row_click_navigates : given.screens
{
    void Establish() => Compile("readmodel R\n  name String\nquery List => R[]\nscreen Home\n  data R[] via query List\n  table R\n    column name\n    on row-click navigate to Detail\nscreen Detail", "on load\n  navigate to Home");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_follow_the_row_click_edge() => Findings.ShouldBeEmpty();
}
