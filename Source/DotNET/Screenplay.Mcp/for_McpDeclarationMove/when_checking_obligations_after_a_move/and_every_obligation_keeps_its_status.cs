// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_moving_a_declaration.given;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpDeclarationMove.when_checking_obligations_after_a_move;

public class and_every_obligation_keeps_its_status : Specification
{
    static McpSnapshot Snapshot(string source) => new([WorkspaceDocument.Create("application", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(source))]);

    string? _refusal;

    void Because()
    {
        _refusal = McpDeclarationMove.Obligations(Snapshot(a_declaration_move_connection.Model), Snapshot(a_declaration_move_connection.Model), new Dictionary<SemanticAddress, SemanticAddress>());
    }

    [Fact] void should_not_refuse() => _refusal.ShouldBeNull();
}
