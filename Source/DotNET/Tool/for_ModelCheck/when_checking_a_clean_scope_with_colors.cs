// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_checking_a_clean_scope_with_colors : given.a_model
{
    int _exitCode;

    void Because() => _exitCode = ModelCheck.Run([Root, "--scope", "M.F.Clean"], Output, Error, useColors: true);

    [Fact] void should_keep_the_scoped_exit_code() => _exitCode.ShouldEqual(0);
    [Fact] void should_color_the_failing_whole_application_red() => Output.ToString().ShouldContain("\e[31mWhole application: 1 error(s), 1 warning(s)");
}
