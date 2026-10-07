// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_checking_an_unknown_scope : given.a_model
{
    int _exitCode;

    void Because() => _exitCode = ModelCheck.Run([Root, "--scope", "M.F.Unknown"], Output, Error);

    [Fact] void should_report_a_usage_error() => _exitCode.ShouldEqual(2);
    [Fact] void should_name_the_unknown_scope() => Error.ToString().ShouldContain("Unknown scope 'M.F.Unknown'");
    [Fact] void should_not_print_a_clean_summary() => Output.ToString().ShouldBeEmpty();
}
