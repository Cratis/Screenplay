// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison;

public class when_comparing_sample_agent_dependencies : Cratis.Screenplay.for_Samples.given.the_samples
{
    AgentExposureComparison _unchanged = null!;
    AgentExposureComparison _changed = null!;

    void Because()
    {
        var library = _samples.Single(path => Path.GetFileName(path) == "Library");
        var source = File.ReadAllText(Path.Combine(library, "library.play")) + """

            agents
              tokens personal
              query AllBooks
                scope read
                description "Browse the catalog"
              command AddBook
                scope write
                description "Add a book"
            """;
        var before = ComparedModel.FromSources("Library", new Dictionary<string, string> { ["library.play"] = source });
        var changed = source.Replace("require role \"Librarian\"", "require role \"SeniorLibrarian\"", StringComparison.Ordinal);
        var after = ComparedModel.FromSources("Library", new Dictionary<string, string> { ["library.play"] = changed });
        _unchanged = ModelComparison.Compare(before, before).AgentExposure;
        _changed = ModelComparison.Compare(before, after).AgentExposure;
    }

    [Fact] void should_compare_the_unchanged_sample_completely() => _unchanged.Coverage.ShouldEqual(AgentExposureCoverage.Complete);
    [Fact] void should_keep_the_current_sample_contract_unchanged() => _unchanged.Rows.Select(row => row.Change).ShouldEqual([AgentExposureChangeKind.Unchanged, AgentExposureChangeKind.Unchanged]);
    [Fact] void should_compare_a_changed_sample_completely() => _changed.Coverage.ShouldEqual(AgentExposureCoverage.Complete);
    [Fact] void should_detect_a_changed_sample_dependency() => _changed.Rows.Select(row => row.Change).ShouldEqual([AgentExposureChangeKind.Widened, AgentExposureChangeKind.Widened]);
}
