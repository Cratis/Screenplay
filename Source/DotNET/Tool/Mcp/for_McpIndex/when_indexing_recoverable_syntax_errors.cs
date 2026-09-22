// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.Mcp.for_McpIndex;

public class when_indexing_recoverable_syntax_errors : Specification
{
    McpSnapshot _snapshot = null!;
    McpSyntaxIndex _index = null!;

    void Establish() => _snapshot = new([given.synthetic_model.Document("broken", """
        unexpected declaration
        module Billing
          feature Accounts
            slice StateChange Register
              command Register
                produces Missing
        """)]);

    void Because() => _index = _snapshot.Index;

    [Fact] void should_keep_syntax_diagnostics() => _snapshot.Compilation.Success.ShouldBeFalse();
    [Fact] void should_keep_the_recovered_command() => _index.Find("Billing.Accounts.Register.Register", "Command").Length.ShouldEqual(1);
    [Fact] void should_keep_an_unresolved_reference() => _index.Resolve(_index.References.Single()).ShouldBeEmpty();
    [Fact] void should_not_reparse_to_recover_the_original_tree() => _snapshot.ParsedDocumentCount.ShouldEqual(1);
}
