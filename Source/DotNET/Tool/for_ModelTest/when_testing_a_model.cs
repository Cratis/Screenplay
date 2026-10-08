// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.for_ModelTest;

public class when_testing_a_model : given.a_model
{
    int _exit;
    JsonElement _report;

    void Because()
    {
        _exit = ModelTest.Run([Root, "--format", "json"], Output, Error);
        using var document = JsonDocument.Parse(Output.ToString());
        _report = document.RootElement.Clone();
    }

    [Fact] void should_fail_the_wrong_expectation() => _exit.ShouldEqual(1);
    [Fact] void should_discover_both_scenarios() => _report.GetProperty("discovered").GetInt32().ShouldEqual(2);
    [Fact] void should_execute_both_scenarios() => _report.GetProperty("executed").GetInt32().ShouldEqual(2);
    [Fact] void should_include_readable_differences() => _report.GetProperty("results")[1].GetProperty("failures").ToString().ShouldContain("Other");
    [Fact] void should_keep_errors_separate_from_json() => Error.ToString().ShouldBeEmpty();
}
