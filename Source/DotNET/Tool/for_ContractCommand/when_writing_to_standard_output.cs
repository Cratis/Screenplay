// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Contracts;

namespace Cratis.Screenplay.Tool.for_ContractCommand;

public class when_writing_to_standard_output : Specification
{
    StringWriter _output;
    StringWriter _error;
    int _exit;

    void Establish()
    {
        _output = new();
        _error = new();
    }

    void Because() => _exit = ContractCommand.Run([], _output, _error);

    [Fact] void should_succeed_without_a_model() => _exit.ShouldEqual(0);
    [Fact] void should_write_the_library_contract() => _output.ToString().ShouldEqual(ScreenplayContract.Serialize());
    [Fact] void should_not_write_errors() => _error.ToString().ShouldEqual(string.Empty);
}
