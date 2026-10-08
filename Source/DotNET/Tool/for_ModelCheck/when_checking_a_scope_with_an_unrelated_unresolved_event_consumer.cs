// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_checking_a_scope_with_an_unrelated_unresolved_event_consumer : given.a_model
{
    int _exitCode;

    void Establish()
    {
        File.WriteAllText(Path.Combine(Root, "application.play"), "module M\n  feature F\n    slice StateChange Clean\n      event CleanEvent\n        value String");
        File.WriteAllText(Path.Combine(Root, "consumer.play"), """
            module Other
              feature F
                slice StateChange Use
                  command Consume
                    id String identifier
                    broken
                    produces MissingEvent
                      for id
            """);
    }

    void Because() => _exitCode = ModelCheck.Run([Root, "--scope", "M.F.Clean", "--warnaserror"], Output, Error, useColors: true);

    [Fact] void should_keep_the_clean_scope_exit_code() => _exitCode.ShouldEqual(0);
    [Fact] void should_not_claim_a_directly_affected_scope() => Output.ToString().ShouldContain("Affected scopes: none");
    [Fact] void should_report_the_unresolved_event_separately() => Output.ToString().ShouldContain("Unresolved event consumers (cannot be attributed to a scope): 1 reference(s) in Other.F.Use");
    [Fact] void should_not_count_the_event_consumer_as_possibly_affected() => Output.ToString().ShouldContain("Possibly affected: 0 other unresolved reference(s)");
    [Fact] void should_color_the_whole_application_failure_red() => Output.ToString().ShouldContain("\e[31mWhole application: 1 error(s), 1 warning(s)");
}
