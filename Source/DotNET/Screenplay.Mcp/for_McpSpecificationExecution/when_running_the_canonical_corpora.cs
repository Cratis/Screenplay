// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationExecution;

public class when_running_the_canonical_corpora : Specification
{
    readonly List<(CanonicalCorpusVector Vector, McpSpecificationReport Report)> _reports = [];

    void Because()
    {
        foreach (var vector in new[] { RegisterProjectCorpus.LegacyV1, RegisterProjectCorpus.V2, ReadModelAbsenceCorpus.V5, ReactionsCorpus.V6 })
        {
            foreach (var form in vector.SourceForms)
            {
                var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
                var documents = form.Documents.Select(document => WorkspaceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, PortablePlayPath.Parse(document.DisplayPath), Encoding.UTF8.GetBytes(document.Text)));
                var workspace = ScreenplayWorkspace.Create(vector.ApplicationIdentity, vector.ApplicationName, [.. documents], catalog);
                _reports.Add((vector, McpSpecificationExecution.Run(workspace)));
            }
        }
    }

    [Fact] void should_discover_every_expected_specification() => _reports.ShouldEachConformTo(pair => pair.Report.Discovered == pair.Vector.SpecificationExpectations.Length);
    [Fact] void should_match_every_recorded_execution_outcome() => _reports.ShouldEachConformTo(pair => pair.Vector.SpecificationExpectations.All(expectation => pair.Report.Results.Single(result => result.SemanticId == expectation.Specification.ToString()).ExecutionOutcome == expectation.Outcome.ToString()));
    [Fact] void should_match_every_recorded_pass_or_unsupported_outcome() => _reports.ShouldEachConformTo(pair => pair.Vector.SpecificationExpectations.All(expectation => pair.Report.Results.Single(result => result.SemanticId == expectation.Specification.ToString()).Outcome == (expectation.Passed ? "passed" : "unsupported")));
    [Fact] void should_never_report_unsupported_startup_as_passed() => _reports.Where(pair => pair.Vector.Name == ReactionsCorpus.V6.Name).ShouldEachConformTo(pair => pair.Report.Outcome == "unsupported" && pair.Report.Unsupported == 1);
}
