// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_gating_processing_purposes : given.a_model
{
    int _exitCode;

    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), """
        concept Name : String pii
        module M
          feature F
            slice StateChange Record
              command Record
                name Name
        """);

    void Because() => _exitCode = ModelCheck.Run([Root, "--check", "purposes", "--warnaserror"], Output, Error);

    [Fact] void should_fail_on_the_uncovered_personal_data_prompt() => _exitCode.ShouldEqual(1);
    [Fact] void should_report_the_stable_code() => Output.ToString().ShouldContain("PLAY0602");
}
