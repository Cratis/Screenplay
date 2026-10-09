// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ContractCommand;

public class when_the_output_directory_does_not_exist : Specification
{
    StringWriter _output;
    StringWriter _error;
    int _exit;

    void Establish()
    {
        _output = new();
        _error = new();
    }

    void Because() => _exit = ContractCommand.Run(["--output", Path.Combine(Directory.GetCurrentDirectory(), ".ai-work", Guid.NewGuid().ToString("N"), "contract.json")], _output, _error);

    [Fact] void should_report_failure() => _exit.ShouldEqual(2);
    [Fact] void should_not_print_a_partial_contract() => _output.ToString().ShouldEqual(string.Empty);
    [Fact] void should_explain_the_write_failure() => _error.ToString().ShouldContain("Could not write the Screenplay contract:");
}
