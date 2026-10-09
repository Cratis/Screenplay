// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ContractCommand;

public class when_rejecting_invalid_arguments : Specification
{
    [Theory]
    [InlineData("--unknown")]
    [InlineData("--output")]
    [InlineData("path.json")]
    [InlineData("--output", "")]
    [InlineData("--output", "--help")]
    [InlineData("--output", "contract.json", "extra")]
    void should_refuse_without_writing_a_partial_contract(params string[] arguments)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        ContractCommand.Run(arguments, output, error).ShouldEqual(2);
        output.ToString().ShouldEqual(string.Empty);
        error.ToString().ShouldContain("Usage: screenplay contract");
    }
}
