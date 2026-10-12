// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_QueryFiltersCorpus;

public class when_loading_v14_source : Specification
{
    SemanticCompilation[] _forms;

    void Because()
    {
        _forms = [.. QueryFiltersCorpus.SourceForms.Select(Compile)];
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_QUERY_FILTERS_CORPUS") == "1")
        {
            var expected = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/QueryFilters/v14/expected");
            Directory.CreateDirectory(expected);
            File.WriteAllBytes(Path.Combine(expected, "esm-v14.json"), SemanticModelSerializer.Serialize(_forms[0].Model));
            File.WriteAllText(Path.Combine(expected, "semantic-revision.txt"), $"{_forms[0].Model.Revision}\n");
            Assert.Fail("Query filters corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_QUERY_FILTERS_CORPUS.");
        }
    }

    [Fact] void should_select_v14() => _forms.All(form => form.Model.LanguageVersion == LanguageVersion.V14 && form.Model.SemanticVersion == SemanticVersion.V14).ShouldBeTrue();

    [Fact]
    void should_pin_bytes_revision_and_strict_round_trip()
    {
        var vector = QueryFiltersCorpus.V14;
        foreach (var form in _forms)
        {
            form.Model.Revision.ShouldEqual(vector.SemanticRevision);
            SemanticModelSerializer.Serialize(form.Model).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
        }
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan())).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
    }

    [Fact]
    void should_execute_unordered_rows_and_distinct_calls()
    {
        foreach (var form in _forms)
        {
            var plan = SemanticExecutionPlan.Compile(form.Model).Plan!;
            foreach (var expectation in QueryFiltersCorpus.V14.SpecificationExpectations)
            {
                var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                Assert.True(run.Passed, string.Join('\n', run.Failures));
                run.Execution.Kind.ShouldEqual(expectation.Outcome);
            }
        }
    }

    static SemanticCompilation Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("QueryFilters", SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).ShouldBeEmpty();

        return result.Value!;
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
