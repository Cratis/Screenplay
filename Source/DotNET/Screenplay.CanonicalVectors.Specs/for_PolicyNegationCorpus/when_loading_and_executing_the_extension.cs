// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_PolicyNegationCorpus;

public class when_loading_and_executing_the_extension : Specification
{
    SemanticCompilation[] _forms;

    void Because()
    {
        _forms = [.. PolicyNegationCorpus.SourceForms.Select(Compile)];
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_POLICY_NEGATION") == "1")
        {
            var bytes = SemanticModelSerializer.Serialize(_forms[0].Model);
            var expected = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/PolicyNegation/expected");
            Directory.CreateDirectory(expected);
            File.WriteAllBytes(Path.Combine(expected, "esm-v7.json"), bytes);
            File.WriteAllText(Path.Combine(expected, "semantic-revision.txt"), $"{_forms[0].Model.Revision}\n");
            File.WriteAllBytes(Path.Combine(Root(), "Source/DotNET/Screenplay/Semantics/Serialization/Golden/policy-negation-v7.json"), bytes);
            Assert.Fail("Policy-negation vectors regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_POLICY_NEGATION.");
        }
    }

    [Fact] void should_compare_four_source_forms() => _forms.Length.ShouldEqual(4);
    [Fact] void should_select_v7_only_for_the_extension() => _forms.All(form => form.Model.LanguageVersion == LanguageVersion.V7 && form.Model.SemanticVersion == SemanticVersion.V7).ShouldBeTrue();

    [Fact]
    void should_pin_bytes_revision_and_strict_round_trip()
    {
        var corpus = PolicyNegationCorpus.V7;
        foreach (var form in _forms)
        {
            SemanticModelSerializer.Serialize(form.Model).SequenceEqual(corpus.EsmBytes).ShouldBeTrue();
            form.Model.Revision.ShouldEqual(corpus.SemanticRevision);
        }
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(corpus.EsmBytes.AsSpan())).SequenceEqual(corpus.EsmBytes).ShouldBeTrue();
        using var golden = typeof(when_loading_and_executing_the_extension).Assembly.GetManifestResourceStream("Cratis.Screenplay.CanonicalVectors.Golden.policy-negation-v7.json")!;
        using var bytes = new MemoryStream();
        golden.CopyTo(bytes);
        bytes.ToArray().SequenceEqual(corpus.EsmBytes).ShouldBeTrue();
    }

    [Fact]
    void should_allow_people_and_deny_services_and_unauthenticated_callers()
    {
        foreach (var form in _forms)
        {
            var plan = SemanticExecutionPlan.Compile(form.Model).Plan!;
            var specifications = form.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications;
            specifications.Length.ShouldEqual(4);
            var expectations = PolicyNegationCorpus.V7.SpecificationExpectations;
            specifications.Select(specification => specification.Id).OrderBy(id => id.ToString(), StringComparer.Ordinal).ShouldEqual(expectations.Select(expectation => expectation.Specification));
            foreach (var expectation in expectations)
            {
                specifications.Single(specification => specification.Id == expectation.Specification).Name.ShouldEqual(expectation.Name);
                var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                run.Failures.ShouldBeEmpty();
                run.Passed.ShouldBeTrue();
                run.Execution.Kind.ShouldEqual(expectation.Outcome);
                if (expectation.RejectionCategory is { } category) ((SemanticRejected)run.Execution).Category.ShouldEqual(category);
                run.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
            }
        }
    }

    static SemanticCompilation Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("PolicyNegation", SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Diagnostics.ShouldBeEmpty();
        return result.Value!;
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;
        return directory!.FullName;
    }
}
