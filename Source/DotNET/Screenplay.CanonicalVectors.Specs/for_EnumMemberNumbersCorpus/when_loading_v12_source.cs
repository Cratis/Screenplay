// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EnumMemberNumbersCorpus;

public class when_loading_v12_source : Specification
{
    SemanticCompilation[] _forms;

    void Because()
    {
        _forms = [.. EnumMemberNumbersCorpus.SourceForms.Select(Compile)];
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_ENUM_MEMBER_NUMBERS_CORPUS") == "1")
        {
            var expected = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/EnumMemberNumbers/v12/expected");
            Directory.CreateDirectory(expected);
            File.WriteAllBytes(Path.Combine(expected, "esm-v12.json"), SemanticModelSerializer.Serialize(_forms[0].Model));
            File.WriteAllText(Path.Combine(expected, "semantic-revision.txt"), $"{_forms[0].Model.Revision}\n");
            Assert.Fail("Enum member numbers corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_ENUM_MEMBER_NUMBERS_CORPUS.");
        }
    }

    [Fact] void should_select_v12() => _forms.All(form => form.Model.LanguageVersion == LanguageVersion.V12 && form.Model.SemanticVersion == SemanticVersion.V12).ShouldBeTrue();

    [Fact]
    void should_pin_bytes_revision_and_strict_round_trip()
    {
        var vector = EnumMemberNumbersCorpus.V12;
        foreach (var form in _forms)
        {
            form.Model.Revision.ShouldEqual(vector.SemanticRevision);
            SemanticModelSerializer.Serialize(form.Model).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
            form.Model.Application.Concepts.Single(concept => concept.Name == "InvoiceStatus").Numbers!.Value.ShouldContainOnly(-1, 4, 90);
        }
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan())).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
    }

    [Fact]
    void should_execute_by_member_name_not_number()
    {
        foreach (var form in _forms)
        {
            var plan = SemanticExecutionPlan.Compile(form.Model).Plan!;
            foreach (var expectation in EnumMemberNumbersCorpus.V12.SpecificationExpectations)
            {
                plan.Specifications[expectation.Specification].Name.ShouldEqual(expectation.Name);
                var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                run.Passed.ShouldBeTrue();
                run.Failures.ShouldBeEmpty();
                run.Execution.Kind.ShouldEqual(expectation.Outcome);
                run.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
                var fact = ((SemanticAccepted)run.Execution).Facts.Single();
                var expected = expectation.Name switch { "DraftStatus" => "draft", "IssuedStatus" => "issued", _ => "cancelled" };
                fact.Values.Single().Value.ShouldEqual(SemanticValue.Text(expected));
            }
        }
    }

    static SemanticCompilation Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("EnumMemberNumbers", SemanticDocumentSet.Create([.. documents], catalog));
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
