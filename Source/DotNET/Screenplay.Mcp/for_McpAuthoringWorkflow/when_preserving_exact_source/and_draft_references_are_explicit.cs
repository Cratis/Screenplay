// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_preserving_exact_source;

public class and_draft_references_are_explicit : given.an_exact_source_connection
{
    static readonly string Supplied = Original.Replace("\n        produces", "\n        notes UnknownProjectName\n        produces", StringComparison.Ordinal);
    JsonElement _proposal;

    void Because()
    {
        var arguments = Arguments(ReplaceSource(Supplied));
        arguments["referencePolicy"] = "Draft";
        _proposal = Result("propose-source", arguments);
        Apply(Opened, _proposal);
    }

    [Fact] void should_retain_explicit_reference_debt() => _proposal.GetProperty("referencePolicy").GetString().ShouldEqual("Draft");
    [Fact] void should_report_reference_diagnostics() => _proposal.GetProperty("authoringDiagnosticCount").GetInt32().ShouldBeGreaterThan(0);
    [Fact] void should_not_claim_executable_readiness() => _proposal.GetProperty("after").GetProperty("executableReady").GetBoolean().ShouldBeFalse();
    [Fact] void should_write_the_draft_source_verbatim() => File.ReadAllText(ApplicationPath).ShouldEqual(Supplied);
}
