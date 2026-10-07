// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_guarded_choices_discover_submitting_forms : given.screens
{
    void Establish() => Compile(
        """
        command Go
        command Fallback
        readmodel Item
          value String
        query Get => Item
        screen Home
          data Item via query Get
          action "Choose"
            when item.value == "ready" execute Go
            otherwise execute Fallback
        screen Detail
        screen FallbackDetail
        """,
        """
        on load
          navigate to Home
        form Input for Go
          on submit navigate to Detail
        form FallbackInput for Fallback
          on submit navigate to FallbackDetail
        """);
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_follow_both_discovered_forms_submit_edges() => Findings.ShouldBeEmpty();
}
