// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_DescribedCommandHandlersCorpus;

public class when_loading_v23_source : Specification
{
    CompilationResult<SemanticCompilation>[] _described;
    CompilationResult<SemanticCompilation>[] _legacy;

    void Because()
    {
        _described = [.. DescribedCommandHandlersCorpus.SourceForms.Select(Compile)];
        _legacy = [.. DescribedCommandHandlersCorpus.LegacySourceForms.Select(Compile)];
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_DESCRIBED_COMMAND_HANDLERS_CORPUS") == "1")
        {
            var expected = Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/DescribedCommandHandlers/v23/expected");
            Directory.CreateDirectory(expected);
            File.WriteAllBytes(Path.Combine(expected, "esm-v23.json"), SemanticModelSerializer.Serialize(_described[0].Value!.Model));
            File.WriteAllText(Path.Combine(expected, "semantic-revision.txt"), $"{_described[0].Value!.Model.Revision}\n");
            File.WriteAllBytes(Path.Combine(expected, "esm-legacy.json"), SemanticModelSerializer.Serialize(_legacy[0].Value!.Model));
            File.WriteAllText(Path.Combine(expected, "legacy-revision.txt"), $"{_legacy[0].Value!.Model.Revision}\n");
            Assert.Fail("Described handler corpus regenerated; review, rebuild and rerun without SCREENPLAY_REGENERATE_DESCRIBED_COMMAND_HANDLERS_CORPUS.");
        }
    }

    [Fact] void should_select_v23_for_the_handler() => _described.All(form => form.Value!.Model.SemanticVersion == SemanticVersion.V23).ShouldBeTrue();
    [Fact] void should_retain_v19_for_the_control() => _legacy.All(form => form.Value!.Model.SemanticVersion == SemanticVersion.V19).ShouldBeTrue();

    [Fact]
    void should_pin_both_contracts_and_every_equivalent_source_form()
    {
        foreach (var (forms, vector) in new[] { (_described, DescribedCommandHandlersCorpus.V23), (_legacy, DescribedCommandHandlersCorpus.Legacy) })
        {
            foreach (var form in forms)
            {
                form.Value!.Model.Revision.ShouldEqual(vector.SemanticRevision);
                SemanticModelSerializer.Serialize(form.Value.Model).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
            }
            SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan())).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
        }
    }

    [Fact]
    void should_publish_a_pending_requirement_with_a_typed_context()
    {
        foreach (var form in _described)
        {
            var requirement = form.ImplementationRequirements.Single();
            requirement.AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.Pending);
            requirement.Hints.ToArray().ShouldEqual(["Calculate the fee from overdue days", "Use the agreed daily rate"]);
            requirement.IsProvisional.ShouldBeTrue();
            requirement.IsAmbiguous.ShouldBeFalse();
            form.TypedContextDescriptors.Single().RequirementId.ShouldEqual(requirement.RequirementId);
        }
        _legacy.All(form => form.ImplementationRequirements.IsEmpty).ShouldBeTrue();
    }

    [Fact]
    void should_pin_denial_validation_body_and_reaction_outcomes()
    {
        foreach (var (forms, vector) in new[] { (_described, DescribedCommandHandlersCorpus.V23), (_legacy, DescribedCommandHandlersCorpus.Legacy) })
        {
            foreach (var form in forms)
            {
                var plan = SemanticExecutionPlan.Compile(form.Value!.Model).Plan!;
                foreach (var expectation in vector.SpecificationExpectations)
                {
                    var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                    Assert.True(run.Passed == expectation.Passed, $"{vector.Name}/{expectation.Name}: {string.Join('\n', run.Failures)}");
                    run.Execution.Kind.ShouldEqual(expectation.Outcome);
                    run.Execution.World.Facts.Length.ShouldEqual(expectation.WorldFactCount!.Value);
                    if (run.Execution is SemanticRejected rejected) rejected.Category.ShouldEqual(expectation.RejectionCategory!.Value);
                    if (run.Execution is SemanticUnsupported unsupported)
                    {
                        unsupported.Capability.ShouldEqual(SemanticExecutionCapability.ImplementationBody);
                        unsupported.Details.ShouldContain("ApplyLateFee");
                        unsupported.Details.ShouldContain(form.ImplementationRequirements.Single().RequirementId);
                    }
                }
            }
        }
    }

    static CompilationResult<SemanticCompilation> Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("DescribedCommandHandlers", SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));

        // This deliberate denial witness declares a system identity that cannot pass the gate.
        var warnings = result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).ToArray();
        Assert.True(warnings.Length == 1, string.Join('\n', warnings.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        warnings[0].Code.ShouldEqual(DiagnosticCodes.UnsatisfiedReactionIdentity);
        return result;
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;
        return directory!.FullName;
    }
}
