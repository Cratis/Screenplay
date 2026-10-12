// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ConceptComplianceCorpus;

public class when_loading_v13_source : Specification
{
    SemanticCompilation[] _forms;

    void Because()
    {
        _forms = [.. ConceptComplianceCorpus.SourceForms.Select(Compile)];
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_CONCEPT_COMPLIANCE_CORPUS") == "1")
        {
            var expected = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ConceptCompliance/v13/expected");
            Directory.CreateDirectory(expected);
            File.WriteAllBytes(Path.Combine(expected, "esm-v13.json"), SemanticModelSerializer.Serialize(_forms[0].Model));
            File.WriteAllText(Path.Combine(expected, "semantic-revision.txt"), $"{_forms[0].Model.Revision}\n");
            Assert.Fail("Concept compliance corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_CONCEPT_COMPLIANCE_CORPUS.");
        }
    }

    [Fact] void should_select_v13() => _forms.All(form => form.Model.LanguageVersion == LanguageVersion.V13 && form.Model.SemanticVersion == SemanticVersion.V13).ShouldBeTrue();

    [Fact]
    void should_pin_bytes_revision_and_strict_round_trip()
    {
        var vector = ConceptComplianceCorpus.V13;
        foreach (var form in _forms)
        {
            form.Model.Revision.ShouldEqual(vector.SemanticRevision);
            SemanticModelSerializer.Serialize(form.Model).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
        }
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan())).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
    }

    [Fact]
    void should_execute_identically_with_and_without_markers()
    {
        foreach (var form in _forms)
        {
            var marked = form.Model;
            var unmarkedApplication = marked.Application with { Concepts = [.. marked.Application.Concepts.Select(concept => concept with { Compliance = null })] };
            var unmarked = ExecutableSemanticModel.Create(LanguageVersion.V12, SemanticVersion.V12, unmarkedApplication);
            var markedPlan = SemanticExecutionPlan.Compile(marked);
            var unmarkedPlan = SemanticExecutionPlan.Compile(unmarked);
            markedPlan.Plan.ShouldNotBeNull();
            unmarkedPlan.Plan.ShouldNotBeNull();
            foreach (var expectation in ConceptComplianceCorpus.V13.SpecificationExpectations)
            {
                var run = new SemanticSpecificationRunner().Run(markedPlan.Plan!, expectation.Specification);
                var baseline = new SemanticSpecificationRunner().Run(unmarkedPlan.Plan!, expectation.Specification);
                run.Passed.ShouldBeTrue();
                run.Failures.ShouldBeEmpty();
                baseline.Passed.ShouldBeTrue();
                run.Execution.Kind.ShouldEqual(expectation.Outcome);
                run.Execution.Kind.ShouldEqual(baseline.Execution.Kind);
                run.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
                var fact = ((SemanticAccepted)run.Execution).Facts.Single();
                var unmarkedFact = ((SemanticAccepted)baseline.Execution).Facts.Single();
                fact.Values.SequenceEqual(unmarkedFact.Values).ShouldBeTrue();
                fact.Destination.ShouldEqual(unmarkedFact.Destination);
            }
        }
    }

    static SemanticCompilation Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("ConceptCompliance", SemanticDocumentSet.Create([.. documents], catalog));
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
