// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ProtectedCommandReadsCorpus;

public class when_loading_v25_replacement : Specification
{
    [Fact]
    void should_compile_pin_and_run_every_replacement_source_form_without_dispositions()
    {
        var vector = ProtectedReadReplacements.V25R2;
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_PROTECTED_COMMAND_READS_CORPUS_V25R2") == "1") Regenerate();
        vector.SourceForms.Length.ShouldEqual(4);
        vector.SpecificationExpectations.Length.ShouldEqual(19);
        foreach (var form in vector.SourceForms)
        {
            var compiled = when_loading_v25_source.Compile(form);
            Assert.True(compiled.Success, string.Join('\n', compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
            var model = compiled.Value!.Model;
            model.Revision.ShouldEqual(vector.SemanticRevision);
            SemanticModelSerializer.Serialize(model).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
            when_loading_v25_source.VerifySpecifications(model, vector);
        }
        Hash(vector.EsmBytes.ToArray()).ShouldEqual(ProtectedReadReplacements.V25R2Sha256);
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan())).SequenceEqual(vector.EsmBytes).ShouldBeTrue();
    }

    [Fact]
    void should_pin_thirteen_exact_original_dispositions_and_the_frozen_bytes()
    {
        var original = ProtectedCommandReadsCorpus.V25;
        var dispositions = ProtectedReadReplacements.SpecificationDispositions;
        Hash(original.EsmBytes.ToArray()).ShouldEqual(ProtectedReadReplacements.OriginalV25Sha256);
        dispositions.Length.ShouldEqual(13);
        dispositions.Select(disposition => disposition.CaseName).Distinct(StringComparer.Ordinal).Count().ShouldEqual(13);
        foreach (var disposition in dispositions)
        {
            disposition.VectorId.ShouldEqual("ProtectedCommandReadsCorpus.V25");
            disposition.SemanticRevision.ShouldEqual(original.SemanticRevision);
            disposition.Sha256.ShouldEqual(ProtectedReadReplacements.OriginalV25Sha256);
            disposition.ReplacementVectorId.ShouldEqual("ProtectedReadReplacements.V25R2");
            disposition.CommitIndex.ShouldBeNull();
            original.SpecificationExpectations.Single(expectation => expectation.Name == disposition.CaseName).RejectionMessage.ShouldEqual(disposition.OriginalMessage);
            disposition.ExpectedFailures.Length.ShouldEqual(1);
            disposition.Reason.ShouldNotBeEmpty();
        }
    }

    [Fact]
    void should_preserve_application_and_specification_identities_but_not_the_revision()
    {
        var original = ProtectedCommandReadsCorpus.V25;
        var replacement = ProtectedReadReplacements.V25R2;
        replacement.Name.ShouldEqual("protected-command-reads/v25-r2");
        replacement.ApplicationIdentity.ShouldEqual(original.ApplicationIdentity);
        replacement.SemanticRevision.ShouldNotEqual(original.SemanticRevision);
        replacement.SpecificationExpectations.Select(expectation => expectation.Specification).ShouldEqual(original.SpecificationExpectations.Select(expectation => expectation.Specification));
        foreach (var (before, after) in original.SourceForms.Zip(replacement.SourceForms))
            before.IdentityCatalogBytes.SequenceEqual(after.IdentityCatalogBytes).ShouldBeTrue();
    }

    static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    static void Regenerate([CallerFilePath] string path = "")
    {
        var compiled = when_loading_v25_source.Compile(ProtectedReadReplacements.V25R2.SourceForms.Single(form => form.Name == "folder"));
        Assert.True(compiled.Success);
        var model = compiled.Value!.Model;
        when_loading_v25_source.VerifySpecifications(model, ProtectedReadReplacements.V25R2);
        var directory = Path.GetDirectoryName(path)!;
        while (!File.Exists(Path.Combine(directory, "Screenplay.slnx"))) directory = Path.GetDirectoryName(directory)!;
        var expected = Path.Combine(directory, "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ProtectedCommandReads/v25-r2/expected");
        File.WriteAllBytes(Path.Combine(expected, "esm-v25.json"), SemanticModelSerializer.Serialize(model));
        File.WriteAllText(Path.Combine(expected, "semantic-revision.txt"), $"{model.Revision}\n");
        Assert.Fail("V25R2 regenerated; review its bytes, revision and SHA-256 pin, rebuild and rerun without SCREENPLAY_REGENERATE_PROTECTED_COMMAND_READS_CORPUS_V25R2. Originals were not written.");
    }
}
