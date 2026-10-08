// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_checking_a_scope_after_renaming_an_event : given.a_model
{
    int _exitCode;

    void Establish()
    {
        File.WriteAllText(Path.Combine(Root, "application.play"), "module M\n  feature F\n    slice StateChange Clean\n      event CleanedEvent\n        value String");
        File.WriteAllText(Path.Combine(Root, "consumer.play"), """
            module Other
              feature F
                slice StateChange Use
                  command Consume
                    id String identifier
                    value String
                    produces CleanEvent
                      for id
                      value = value
            """);
    }

    void Because() => _exitCode = ModelCheck.Run([Root, "--scope", "M.F.Clean", "--warnaserror"], Output, Error, useColors: true);

    [Fact] void should_keep_the_clean_scope_exit_code() => _exitCode.ShouldEqual(0);
    [Fact] void should_not_claim_a_directly_affected_scope() => Output.ToString().ShouldContain("Affected scopes: none");
    [Fact] void should_report_the_unresolved_event_consumer() => Output.ToString().ShouldContain("Unresolved event consumers (cannot be attributed to a scope): 1 reference(s) in Other.F.Use");
    [Fact] void should_report_the_whole_application_warning_as_a_failure_with_warnaserror() => Output.ToString().ShouldContain("\e[31mWhole application: 0 error(s), 1 warning(s) (1 outside the reported set)");
}
