// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_a_direct_dependent_has_a_warning : given.a_model
{
    int _exitCode;

    void Establish() => File.WriteAllText(Path.Combine(Root, "consumer.play"), """
        module Other
          feature F
            slice StateChange Use
              command Consume
                id String identifier
                value UnknownDependentType
                produces CleanEvent
                  for id
                  value = value
        """);

    void Because() => _exitCode = ModelCheck.Run([Root, "--scope", "M.F.Clean", "--warnaserror"], Output, Error);

    [Fact] void should_fail_for_the_direct_dependent_warning() => _exitCode.ShouldEqual(1);
    [Fact] void should_report_only_the_dependent_warning() => Output.ToString().ShouldContain("0 error(s), 1 warning(s)");
    [Fact] void should_report_the_affected_scope() => Output.ToString().ShouldContain("Affected scopes: Other.F.Use");
}
