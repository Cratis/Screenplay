// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Screenplay.Tool.for_ModelTest;

public class when_testing_the_canonical_corpora : given.a_model
{
    readonly List<(CanonicalCorpusVector Vector, int Exit, JsonElement Report)> _reports = [];

    void Because()
    {
        foreach (var vector in new[] { RegisterProjectCorpus.LegacyV1, RegisterProjectCorpus.V2, ReadModelAbsenceCorpus.V5, ReactionsCorpus.V6 })
        {
            var folder = Path.Combine(Root, vector.Name.Replace('/', '-'));
            Directory.CreateDirectory(folder);
            foreach (var document in vector.SourceForms[0].Documents)
            {
                var path = Path.Combine(folder, document.DisplayPath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, document.Text);
            }

            using var output = new StringWriter();
            var exit = ModelTest.Run([folder, "--format", "json"], output, Error);
            using var json = JsonDocument.Parse(output.ToString());
            _reports.Add((vector, exit, json.RootElement.Clone()));
        }
    }

    [Fact] void should_match_the_corpus_exit_outcomes() => _reports.ShouldEachConformTo(pair => pair.Exit == (pair.Vector.SpecificationExpectations.All(expectation => expectation.Passed) ? 0 : 3));
    [Fact] void should_discover_every_corpus_scenario() => _reports.ShouldEachConformTo(pair => pair.Report.GetProperty("discovered").GetInt32() == pair.Vector.SpecificationExpectations.Length);
    [Fact] void should_match_the_recorded_execution_outcomes() => _reports.ShouldEachConformTo(pair => pair.Vector.SpecificationExpectations.All(expectation => pair.Report.GetProperty("results").EnumerateArray().Single(result => result.GetProperty("address").GetString()!.EndsWith($".{expectation.Name}", StringComparison.Ordinal)).GetProperty("executionOutcome").GetString() == expectation.Outcome.ToString()));
}
