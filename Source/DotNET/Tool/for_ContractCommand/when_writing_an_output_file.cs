// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Contracts;

namespace Cratis.Screenplay.Tool.for_ContractCommand;

public class when_writing_an_output_file : Specification
{
    string _directory;
    string _path;
    StringWriter _output;
    StringWriter _error;
    int _exit;

    void Establish()
    {
        _directory = SpecFixtureDirectory.Create("contract-output");
        _path = Path.Combine(_directory, "contract.json");
        _output = new();
        _error = new();
    }

    void Because() => _exit = ContractCommand.Run(["--output", _path], _output, _error);

    [Fact] void should_succeed() => _exit.ShouldEqual(0);
    [Fact] void should_write_the_library_contract() => File.ReadAllText(_path).ShouldEqual(ScreenplayContract.Serialize());
    [Fact] void should_not_write_a_byte_order_mark() => File.ReadAllBytes(_path)[0].ShouldEqual((byte)'{');
    [Fact] void should_leave_standard_output_empty() => _output.ToString().ShouldEqual(string.Empty);
    [Fact] void should_leave_standard_error_empty() => _error.ToString().ShouldEqual(string.Empty);

    void Destroy()
    {
        _output.Dispose();
        _error.Dispose();
        SpecFixtureDirectory.Delete(_directory);
    }
}
