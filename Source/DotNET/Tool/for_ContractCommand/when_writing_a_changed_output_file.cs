// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Contracts;

namespace Cratis.Screenplay.Tool.for_ContractCommand;

public class when_writing_a_changed_output_file : Specification
{
    static readonly DateTime _marker = new(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    string _directory;
    string _path;
    StringWriter _output;
    StringWriter _error;
    int _exit;

    void Establish()
    {
        _directory = SpecFixtureDirectory.Create("contract-changed");
        _path = Path.Combine(_directory, "contract.json");
        _output = new();
        _error = new();
        File.WriteAllText(_path, "{}");
        File.SetLastWriteTimeUtc(_path, _marker);
    }

    void Because() => _exit = ContractCommand.Run(["--output", _path], _output, _error);

    [Fact] void should_succeed() => _exit.ShouldEqual(0);
    [Fact] void should_rewrite_the_file() => File.ReadAllText(_path).ShouldEqual(ScreenplayContract.Serialize());
    [Fact] void should_update_the_write_time() => File.GetLastWriteTimeUtc(_path).ShouldNotEqual(_marker);

    void Destroy()
    {
        _output.Dispose();
        _error.Dispose();
        SpecFixtureDirectory.Delete(_directory);
    }
}
