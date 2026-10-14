// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_GeneratedConceptRulesCorpus;

public class when_loading_v20_source : Specification
{
    SemanticCompilation[] _forms;

    void Because()
    {
        _forms = [.. GeneratedConceptRulesCorpus.SourceForms.Select(Compile)];
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_GENERATED_CONCEPT_RULES_CORPUS") == "1")
        {
            var expected = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/GeneratedConceptRules/v20/expected");
            Directory.CreateDirectory(expected);
            File.WriteAllBytes(Path.Combine(expected, "esm-v20.json"), SemanticModelSerializer.Serialize(_forms[0].Model));
            File.WriteAllText(Path.Combine(expected, "semantic-revision.txt"), $"{_forms[0].Model.Revision}\n");
            Assert.Fail("Generated concept rules corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_GENERATED_CONCEPT_RULES_CORPUS.");
        }
    }

    [Fact] void should_select_v20() => _forms.All(form => form.Model.LanguageVersion == LanguageVersion.V20 && form.Model.SemanticVersion == SemanticVersion.V20).ShouldBeTrue();

    [Fact]
    void should_pin_bytes_revision_and_strict_round_trip()
    {
        var vector = GeneratedConceptRulesCorpus.V20;
        foreach (var form in _forms)
        {
            form.Model.Revision.ShouldEqual(vector.SemanticRevision);
            SemanticModelSerializer.Serialize(form.Model).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
        }

        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan())).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
    }

    [Fact]
    void should_execute_proven_rules_reject_invalid_fixtures_and_refuse_opaque_rules()
    {
        foreach (var form in _forms)
        {
            var plan = SemanticExecutionPlan.Compile(form.Model).Plan!;
            foreach (var expectation in GeneratedConceptRulesCorpus.V20.SpecificationExpectations)
            {
                var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                run.Execution.Kind.ShouldEqual(expectation.Outcome);
                run.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
                run.Passed.ShouldEqual(expectation.Outcome != SemanticExecutionOutcomeKind.Unsupported);
                if (run.Execution is SemanticRejected rejected)
                {
                    rejected.Category.ShouldEqual(SemanticRejectionCategory.Contract);
                    rejected.Details.ShouldEqual("Generated fixture 'code' fails concept 'InviteCode' rules");
                }
            }
        }
    }

    [Fact]
    void should_check_programmatic_fixtures_as_contracts_not_client_validation()
    {
        var plan = SemanticExecutionPlan.Compile(_forms[0].Model).Plan!;
        var command = plan.Commands.Values.Single(command => command.Name == "Invite");
        var request = SemanticExecutionRequest.Create(command.Id, [], []) with
        {
            GeneratedValues = [new(command.Properties.Single().Id, SemanticValue.Text(new string('a', 63) + "b"))]
        };
        var execution = (SemanticRejected)new SemanticEvaluator().Execute(plan, SemanticWorld.Empty, request);
        execution.Category.ShouldEqual(SemanticRejectionCategory.Contract);
        execution.Details.ShouldEqual("Generated fixture 'code' fails concept 'InviteCode' rules");
        execution.World.Facts.ShouldBeEmpty();
    }

    static SemanticCompilation Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("GeneratedConceptRules", SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnprovenGeneratedConceptRule).ShouldEqual(2);
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning && diagnostic.Code != DiagnosticCodes.UnprovenGeneratedConceptRule).ShouldBeEmpty();

        return result.Value!;
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
