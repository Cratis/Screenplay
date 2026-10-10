// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_MatchEndOfInputCorpus;

public class when_executing_match_rules : Specification
{
    SemanticSpecificationRun _accepted;
    SemanticSpecificationRun _rejected;

    void Because()
    {
        using var stream = typeof(RegisterProjectCorpus).Assembly.GetManifestResourceStream("Cratis.Screenplay.CanonicalCorpus.Corpus.MatchEndOfInput.source.play")!;
        using var reader = new StreamReader(stream);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("MatchEndOfInput"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "source.play", reader.ReadToEnd());
        var result = new SemanticModelCompiler().Compile("MatchEndOfInput", SemanticDocumentSet.Create([document], catalog));
        result.Success.ShouldBeTrue();
        var plan = SemanticExecutionPlan.Compile(result.Value!.Model).Plan!;
        var runner = new SemanticSpecificationRunner();
        _accepted = runner.Run(plan, plan.Specifications.Values.Single(specification => specification.Name == "AcceptingTheWholeInput").Id);
        _rejected = runner.Run(plan, plan.Specifications.Values.Single(specification => specification.Name == "RejectingATrailingNewline").Id);
    }

    [Fact] void should_accept_the_whole_input() => _accepted.Passed.ShouldBeTrue();
    [Fact] void should_reject_a_trailing_newline() => _rejected.Passed.ShouldBeTrue();
    [Fact] void should_report_the_validation_rejection() => _rejected.Execution.ShouldBeOfExactType<SemanticRejected>();
}
