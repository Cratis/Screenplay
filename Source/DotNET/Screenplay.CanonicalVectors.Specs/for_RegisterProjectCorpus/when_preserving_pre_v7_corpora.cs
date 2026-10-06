// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_RegisterProjectCorpus;

public class when_preserving_pre_v7_corpora : Specification
{
    [Fact]
    void should_keep_every_old_corpus_byte_revision_and_outcome()
    {
        (CanonicalCorpusVector Corpus, string Hash)[] vectors =
        [
            (RegisterProjectCorpus.LegacyV1, "050b69657cacb080e30aeb74b7f398034a898ecce0a5aae38cdcb231394ce387"),
            (RegisterProjectCorpus.V2, "33d559853c1ac64a741c3eec2bc6b01ca33fb3fcb5e78e0a62937c8182a4e8c2"),
            (ReadModelAbsenceCorpus.V5, "b4eb78f60c75ef5562c8e79a42ff15b35fae0cdc5cf8a2b9b2c9221f818b3eee"),
            (ReactionsCorpus.V6, "6207b5a1d4d915a3548123d4df9380104d25194a93640d8ba5d0474f2fdf1155")
        ];
        foreach (var (corpus, hash) in vectors)
        {
            Convert.ToHexString(SHA256.HashData(corpus.EsmBytes.AsSpan())).ToLowerInvariant().ShouldEqual(hash);
            foreach (var form in corpus.SourceForms)
            {
                var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
                var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
                    catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
                var result = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
                result.Success.ShouldBeTrue();
                result.Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information).ShouldBeEmpty();
                var model = result.Value!.Model;
                model.Revision.ShouldEqual(corpus.SemanticRevision);
                SemanticModelSerializer.Serialize(model).SequenceEqual(corpus.EsmBytes).ShouldBeTrue();
                var plan = SemanticExecutionPlan.Compile(model).Plan!;
                foreach (var expectation in corpus.SpecificationExpectations)
                {
                    var run = new SemanticSpecificationRunner().Run(plan, expectation.Specification);
                    run.Passed.ShouldEqual(expectation.Passed);
                    run.Execution.Kind.ShouldEqual(expectation.Outcome);
                    if (expectation.RejectionMessage is { } message) ((SemanticRejected)run.Execution).Details.ShouldEqual(message);
                    if (expectation.RejectionCategory is { } category) ((SemanticRejected)run.Execution).Category.ShouldEqual(category);
                    if (expectation.UnsupportedCapability is { } capability) ((SemanticUnsupported)run.Execution).Capability.ShouldEqual(capability);
                    if (expectation.WorldFactCount is { } count) run.Execution.World.Facts.Length.ShouldEqual(count);
                    (run.Execution as SemanticAccepted)?.Response.ShouldBeNull();
                }
            }
        }
    }
}
