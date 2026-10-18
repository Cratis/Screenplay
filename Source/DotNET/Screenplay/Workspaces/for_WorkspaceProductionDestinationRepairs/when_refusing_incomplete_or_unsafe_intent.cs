// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceProductionDestinationRepairs;

public class when_refusing_incomplete_or_unsafe_intent : given.a_destination_model
{
    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("foreign")]
    [InlineData("null")]
    [InlineData("version")]
    [InlineData("route")]
    [InlineData("retarget")]
    [InlineData("unsupported")]
    void should_return_no_partial_candidate(string defect)
    {
        var workspace = Create();
        var intent = Intent(WorkspaceProductionDestinationRepairs.Discover(workspace));
        var selections = intent.Productions;
        intent = defect switch
        {
            "missing" => intent with { Productions = [.. selections.Skip(1)] },
            "duplicate" => intent with { Productions = [selections[0], selections[0], selections[2]] },
            "foreign" => intent with { Productions = [selections[0] with { Subject = selections[0].Subject with { Path = "/foreign" } }, selections[1], selections[2]] },
            "null" => intent with { Productions = [selections[0] with { For = null! }, selections[1], selections[2]] },
            "version" => intent with { Version = 2 },
            "route" => intent with { Productions = [selections[0] with { Route = new("Named") }, selections[1], selections[2]] },
            "retarget" => intent with { Productions = [selections[0], selections[1], selections[2] with { For = new PathExpressionSyntax("otherId", selections[2].For.Location) }] },
            _ => intent with { Productions = [selections[0] with { For = new LiteralExpressionSyntax("allocated", selections[0].For.Location) }, selections[1], selections[2]] }
        };
        var result = Propose(workspace, intent);
        result.Accepted.ShouldBeFalse();
        result.Workspace.ShouldBeNull();
        result.WritePlan.ShouldBeNull();
        result.Conflicts.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("workspace")]
    [InlineData("catalog")]
    [InlineData("subject")]
    [InlineData("formatting")]
    [InlineData("authoring")]
    void should_refuse_stale_or_weakened_requests(string defect)
    {
        var workspace = Create();
        var rows = WorkspaceProductionDestinationRepairs.Discover(workspace);
        var request = Request(workspace);
        request = defect switch
        {
            "workspace" => request with { ExpectedRevision = default },
            "catalog" => request with { ExpectedCatalogRevision = default },
            "formatting" => request with { Formatting = WorkspaceAuthoringFormatting.PreserveTrivia },
            "authoring" => request with { Validation = WorkspaceAuthoringValidation.Authoring },
            _ => request
        };
        var subject = defect == "subject" ? rows[0].Command with { Revision = default } : rows[0].Command;
        var result = WorkspaceProductionDestinationRepairs.Propose(workspace, subject, Intent(rows), request);
        result.Accepted.ShouldBeFalse();
        result.WritePlan.ShouldBeNull();
    }

    [Fact]
    void should_refuse_unrelated_whole_workspace_binding_blockers_with_diagnostics()
    {
        var workspace = Create("system Outside\n" + Source);
        var result = Propose(workspace);
        result.Accepted.ShouldBeFalse();
        result.Workspace.ShouldBeNull();
        result.WritePlan.ShouldBeNull();
        result.ProductionDestinationReport.ShouldBeNull();
        result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.CompilationFailed);
        var diagnostic = result.ExecutableDiagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
        diagnostic.Arguments["issue"].ShouldEqual(DiagnosticArgument.Text("Cratis/Screenplay#301"));
        diagnostic.Location.ShouldEqual(new SourceLocation(1, 1, "application.play"));
    }
}
