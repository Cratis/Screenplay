// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_checking_a_broken_scope : given.a_model
{
    int _exitCode;

    void Because() => _exitCode = ModelCheck.Run([Path.Combine(Root, "application.play"), "--scope", "M.F.Broken"], Output, Error);

    [Fact] void should_fail_for_scoped_defects() => _exitCode.ShouldEqual(1);
    [Fact] void should_report_the_invalid_property() => Output.ToString().ShouldContain("Invalid property 'missingType'");
}
