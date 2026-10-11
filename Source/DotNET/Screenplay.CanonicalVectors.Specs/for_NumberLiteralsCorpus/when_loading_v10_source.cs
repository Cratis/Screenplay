// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_NumberLiteralsCorpus;

public class when_loading_v10_source : Specification
{
    SemanticCompilation[] _forms;

    void Because()
    {
        _forms = [.. NumberLiteralsCorpus.SourceForms.Select(Compile)];
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_NUMBER_LITERALS_CORPUS") == "1")
        {
            var expected = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/NumberLiterals/v10/expected");
            Directory.CreateDirectory(expected);
            File.WriteAllBytes(Path.Combine(expected, "esm-v10.json"), SemanticModelSerializer.Serialize(_forms[0].Model));
            File.WriteAllText(Path.Combine(expected, "semantic-revision.txt"), $"{_forms[0].Model.Revision}\n");
            Assert.Fail("Number literals corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_NUMBER_LITERALS_CORPUS.");
        }
    }

    [Fact] void should_select_v10_for_exact_literal_lowering() => _forms.All(form => form.Model.LanguageVersion == LanguageVersion.V10 && form.Model.SemanticVersion == SemanticVersion.V10).ShouldBeTrue();

    [Fact]
    void should_pin_bytes_revision_and_strict_round_trip()
    {
        var vector = NumberLiteralsCorpus.ExactLiteralsV10;
        foreach (var form in _forms)
        {
            form.Model.Revision.ShouldEqual(vector.SemanticRevision);
            SemanticModelSerializer.Serialize(form.Model).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
        }
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan())).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
    }

    [Fact]
    void should_execute_exact_literals_and_inputs_without_losing_digits()
    {
        foreach (var form in _forms)
        {
            var plan = SemanticExecutionPlan.Compile(form.Model).Plan!;
            foreach (var expectation in NumberLiteralsCorpus.ExactLiteralsV10.SpecificationExpectations)
            {
                plan.Specifications[expectation.Specification].Name.ShouldEqual(expectation.Name);
                var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                run.Passed.ShouldBeTrue();
                run.Failures.ShouldBeEmpty();
                run.Execution.Kind.ShouldEqual(expectation.Outcome);
                run.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
                var fact = ((SemanticAccepted)run.Execution).Facts.Single();
                var contract = plan.Events[fact.EventContract];
                foreach (var property in contract.Properties)
                {
                    var expected = property.Name switch
                    {
                        "largest" => 9007199254740991m,
                        "smallest" => -9007199254740991m,
                        "whole" => 1234567890123456m,
                        _ => 0.10000000000000002m
                    };
                    fact.Values.Single(value => value.TargetProperty == property.Id).Value.ShouldEqual(SemanticValue.Number(expected));
                }
            }
        }
    }

    static SemanticCompilation Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("NumberLiterals", SemanticDocumentSet.Create([.. documents], catalog));
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
