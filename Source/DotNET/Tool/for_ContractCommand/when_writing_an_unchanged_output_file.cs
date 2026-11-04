// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ContractCommand;

public class when_writing_an_unchanged_output_file : Specification
{
    static readonly DateTime _marker = new(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    string _directory;
    string _path;
    StringWriter _output;
    StringWriter _error;
    int _exit;

    void Establish()
    {
        _directory = SpecFixtureDirectory.Create("contract-unchanged");
        _path = Path.Combine(_directory, "contract.json");
        _output = new();
        _error = new();
        ContractCommand.Run(["--output", _path], _output, _error);
        File.SetLastWriteTimeUtc(_path, _marker);
    }

    void Because() => _exit = ContractCommand.Run(["--output", _path], _output, _error);

    [Fact] void should_succeed() => _exit.ShouldEqual(0);
    [Fact] void should_not_rewrite_the_file() => File.GetLastWriteTimeUtc(_path).ShouldEqual(_marker);

    void Destroy()
    {
        _output.Dispose();
        _error.Dispose();
        SpecFixtureDirectory.Delete(_directory);
    }
}
