// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_disclosing_example_command_readiness : given.a_connection
{
    McpSyntaxIndex _index = null!;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), """
        example Input : Records.Entries.Record.RecordItem
          count = 1
        module Records
          feature Entries
            slice StateChange Record
              command RecordItem
                count Int
                handler
                  file RecordItem.cs
              specification Recording
                when Input
        """);

    void Because() => _index = new McpSnapshot(Root.Read()).Index;

    [Fact] void should_retain_the_underlying_commands_readiness() => _index.Readiness.SyntaxOnly(_index.Declarations.Single(declaration => declaration.Syntax is SpecificationSyntax).Syntax).ShouldBeTrue();
    [Fact] void should_explain_the_unadmitted_handler() => _index.Readiness.ExecutionReadiness(_index.Declarations.Single(declaration => declaration.Syntax is SpecificationSyntax).Syntax)!.ShouldContain("command handlers");
}
