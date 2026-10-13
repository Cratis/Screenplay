// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_NotEmptyWhitespaceCorpus;

public class when_executing_not_empty_rules : Specification
{
    Dictionary<string, SemanticSpecificationRun> _runs;

    void Because()
    {
        using var stream = typeof(RegisterProjectCorpus).Assembly.GetManifestResourceStream("Cratis.Screenplay.CanonicalCorpus.Corpus.NotEmptyWhitespace.source.play")!;
        using var reader = new StreamReader(stream);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("NotEmptyWhitespace"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "source.play", reader.ReadToEnd());
        var result = new SemanticModelCompiler().Compile("NotEmptyWhitespace", SemanticDocumentSet.Create([document], catalog));
        result.Success.ShouldBeTrue();
        var compilation = SemanticExecutionPlan.Compile(result.Value!.Model);
        compilation.Success.ShouldBeTrue();
        var plan = compilation.Plan!;
        var runner = new SemanticSpecificationRunner();
        _runs = plan.Specifications.Values.ToDictionary(specification => specification.Name, specification => runner.Run(plan, specification.Id));
    }

    [Fact] void should_load_every_case() => _runs.Count.ShouldEqual(7);
    [Fact] void should_reject_spaces_on_a_concept() => AssertRejected("RejectingWhitespaceOnAConcept");
    [Fact] void should_reject_next_line_on_a_concept() => AssertRejected("RejectingNextLineOnAConcept");
    [Fact] void should_accept_non_whitespace_on_a_concept() => AssertAccepted("AcceptingNonWhitespaceOnAConcept");
    [Fact] void should_accept_a_byte_order_mark_on_a_concept() => AssertAccepted("AcceptingByteOrderMarkOnAConcept");
    [Fact] void should_reject_spaces_on_a_property() => AssertRejected("RejectingWhitespaceOnAProperty");
    [Fact] void should_reject_next_line_on_a_property() => AssertRejected("RejectingNextLineOnAProperty");
    [Fact] void should_accept_a_byte_order_mark_on_a_property() => AssertAccepted("AcceptingByteOrderMarkOnAProperty");

    void AssertRejected(string name)
    {
        _runs[name].Passed.ShouldBeTrue();
        _runs[name].Execution.ShouldBeOfExactType<SemanticRejected>();
        ((SemanticRejected)_runs[name].Execution).Category.ShouldEqual(SemanticRejectionCategory.Validation);
    }

    void AssertAccepted(string name)
    {
        _runs[name].Passed.ShouldBeTrue();
        _runs[name].Execution.ShouldBeOfExactType<SemanticAccepted>();
    }
}
