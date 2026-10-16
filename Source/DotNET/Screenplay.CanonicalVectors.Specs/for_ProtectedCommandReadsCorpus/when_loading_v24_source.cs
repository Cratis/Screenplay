// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ProtectedCommandReadsCorpus;

public class when_loading_v24_source : Specification
{
    SemanticCompilation[] _protected;
    CompilationResult<SemanticCompilation>[] _legacy;

    void Because()
    {
        _protected = [.. ProtectedCommandReadsCorpus.SourceForms.Select(form =>
        {
            var result = Compile(form);
            Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
            result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).ShouldBeEmpty();
            return result.Value!;
        })];
        _legacy = [.. ProtectedCommandReadsCorpus.LegacySourceForms.Select(Compile)];
        VerifySpecifications(_protected[0].Model);
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_PROTECTED_COMMAND_READS_CORPUS") == "1")
        {
            var root = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ProtectedCommandReads/v24");
            Directory.CreateDirectory(Path.Combine(root, "expected"));
            Directory.CreateDirectory(Path.Combine(root, "identity"));
            File.WriteAllBytes(Path.Combine(root, "expected/esm-v24.json"), SemanticModelSerializer.Serialize(_protected[0].Model));
            File.WriteAllText(Path.Combine(root, "expected/semantic-revision.txt"), $"{_protected[0].Model.Revision}\n");
            File.WriteAllBytes(Path.Combine(root, "expected/legacy-diagnostics.json"), Diagnostics(_legacy[0]));
            File.WriteAllBytes(Path.Combine(root, "identity/catalog-v1.json"), [.. ProtectedCommandReadsCorpus.SourceForms[0].IdentityCatalogBytes]);
            Assert.Fail("Protected command reads corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_PROTECTED_COMMAND_READS_CORPUS.");
        }
    }

    [Fact]
    void should_select_v24_only_with_the_mode()
    {
        _protected.Length.ShouldEqual(4);
        _protected.All(form => form.Model.SemanticVersion == SemanticVersion.V24 && form.Model.Application.CommandReadMode == SemanticCommandReadMode.Protected).ShouldBeTrue();
    }

    [Fact]
    void should_pin_the_model_and_every_equivalent_source_form()
    {
        var vector = ProtectedCommandReadsCorpus.V24;
        foreach (var form in _protected)
        {
            form.Model.Revision.ShouldEqual(vector.SemanticRevision);
            SemanticModelSerializer.Serialize(form.Model).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
        }
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan())).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
    }

    [Fact]
    void should_pin_the_shared_identity_catalog()
    {
        var bytes = ProtectedCommandReadsCorpus.IdentityCatalogBytes;
        SemanticIdentityCatalogSerializer.Serialize(SemanticIdentityCatalogSerializer.Deserialize(bytes.AsSpan())).SequenceEqual(bytes).ShouldBeTrue();
        foreach (var form in ProtectedCommandReadsCorpus.SourceForms.Concat(ProtectedCommandReadsCorpus.LegacySourceForms))
            form.IdentityCatalogBytes.SequenceEqual(bytes).ShouldBeTrue();
    }

    [Fact]
    void should_remove_only_the_preamble_in_legacy_sources()
    {
        foreach (var (current, legacy) in ProtectedCommandReadsCorpus.SourceForms.Zip(ProtectedCommandReadsCorpus.LegacySourceForms))
        {
            current.Name.ShouldEqual(legacy.Name);
            current.Documents.Length.ShouldEqual(legacy.Documents.Length);
            foreach (var (original, control) in current.Documents.Zip(legacy.Documents))
            {
                original.StableKey.ShouldEqual(control.StableKey);
                original.DisplayPath.ShouldEqual(control.DisplayPath);
                original.Text.Replace("command reads protected\n", string.Empty, StringComparison.Ordinal).ShouldEqual(control.Text);
            }
        }
    }

    [Fact]
    void should_pin_the_legacy_refusal_diagnostics_without_fabricating_an_esm()
    {
        var vectors = ProtectedCommandReadsCorpus.Legacy;
        vectors.Length.ShouldEqual(4);
        foreach (var (result, vector) in _legacy.Zip(vectors))
        {
            result.Success.ShouldBeFalse();
            result.Value.ShouldBeNull();
            result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.PreservedLegacySemanticSyntax);
            result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyCommandReadsRepairAvailable).ShouldEqual(1);
            Diagnostics(result).SequenceEqual(ProtectedCommandReadsCorpus.LegacyDiagnosticsBytes).ShouldBeTrue();
            result.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Message)).SequenceEqual(vector.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Message))).ShouldBeTrue();
            vector.ArtifactPaths.ShouldBeEmpty();
        }
    }

    [Fact]
    void should_pin_every_specification_outcome_and_committed_world()
    {
        var vector = ProtectedCommandReadsCorpus.V24;
        vector.SpecificationExpectations.Length.ShouldEqual(12);
        foreach (var form in _protected)
        {
            var compilation = SemanticExecutionPlan.Compile(form.Model);
            Assert.True(compilation.Success, string.Join('\n', compilation.Issues.Select(issue => issue.Details)));
            var plan = compilation.Plan!;
            plan.Specifications.Count.ShouldEqual(vector.SpecificationExpectations.Length);
            foreach (var expectation in vector.SpecificationExpectations)
            {
                var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                Assert.True(run.Passed, $"{expectation.Name}: {string.Join('\n', run.Failures)}");
                run.Execution.Kind.ShouldEqual(expectation.Outcome);
                run.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
                if (run.Execution is SemanticRejected rejected)
                {
                    rejected.Category.ShouldEqual(expectation.RejectionCategory!.Value);
                    rejected.Details.ShouldEqual(expectation.RejectionMessage);
                    VerifyRejectedState(plan, expectation.Name, rejected.World);
                }
            }
        }
    }

    static void VerifyRejectedState(SemanticExecutionPlan plan, string name, SemanticWorld world)
    {
        (string Key, string Value)[] expected = name switch
        {
            "MissingFromEmpty" or "MissingAfterRemoval" or "AbsenceBeforeRequirement" or "AbsenceBeforeGeneration" => [],
            "MissingFromNonEmpty" => [("other", "unrelated")],
            "MissingAfterOtherRecreation" => [("other", "recreated elsewhere")],
            "RequirementAfterPresence" => [("reference", "present")],
            "ReadPathRequirementRejectsEmpty" => [("reference", string.Empty)],
            _ => throw new InvalidSemanticContract($"No rejected-world expectation exists for '{name}'.")
        };
        world.ReadModels.Length.ShouldEqual(expected.Length);
        var view = plan.ReadModels.Values.Single();
        var valueProperty = view.Properties.Single(property => property.Name == "value").Id;
        foreach (var state in expected)
        {
            var instance = world.ReadModels.Single(instance => instance.ReadModel == view.Id && instance.Key == SemanticValue.Text(state.Key));
            instance.Values.Single(value => value.TargetProperty == valueProperty).Value.ShouldEqual(SemanticValue.Text(state.Value));
        }
    }

    static void VerifySpecifications(ExecutableSemanticModel model)
    {
        var compilation = SemanticExecutionPlan.Compile(model);
        Assert.True(compilation.Success, string.Join('\n', compilation.Issues.Select(issue => issue.Details)));
        var plan = compilation.Plan!;
        plan.Specifications.Count.ShouldEqual(12);
        foreach (var specification in plan.Specifications.Values)
        {
            var run = new SemanticSpecificationRunner().Run(plan, specification.Id);
            Assert.True(run.Passed, $"{specification.Name}: {string.Join('\n', run.Failures)}");
        }
    }

    static byte[] Diagnostics(CompilationResult<SemanticCompilation> result) => JsonSerializer.SerializeToUtf8Bytes(result.Diagnostics.Select(diagnostic => new { code = diagnostic.Code, message = diagnostic.Message }));

    static CompilationResult<SemanticCompilation> Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        return new SemanticModelCompiler().Compile("ProtectedCommandReads", SemanticDocumentSet.Create([.. documents], catalog));
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;
        return directory!.FullName;
    }
}
