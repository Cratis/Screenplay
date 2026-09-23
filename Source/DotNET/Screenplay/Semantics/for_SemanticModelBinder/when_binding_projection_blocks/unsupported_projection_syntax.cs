// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks;

// What stays out of ESM v1, each with a precise reason, beside its admitted spelling:
// - '$causedBy' compiles in Chronicle but has no runtime resolver (Cratis/Chronicle#4119); '$eventContext.causedBy.<p>' executes.
// - '$eventContext.<path>' resolves by reflection over Chronicle's EventContext (EventContext.cs:28-44); other paths throw.
// - variants have no lowering in Chronicle (ProjectionDefinitionSyntaxVisitor.cs:101-102, #185, Cratis/Chronicle#4109).
// - a parent key is only read inside children (ProjectionFactory.cs:975-1007); a non-text literal key is read as a path (:263-268).
public class unsupported_projection_syntax : given.a_projection_block_binder
{
    CompilationResult<SemanticCompilation> _causedBy;
    CompilationResult<SemanticCompilation> _unknownContextPath;
    CompilationResult<SemanticCompilation> _variant;
    CompilationResult<SemanticCompilation> _sequence;
    CompilationResult<SemanticCompilation> _rootParentKey;
    CompilationResult<SemanticCompilation> _numericLiteralKey;
    CompilationResult<SemanticCompilation> _twoEveryBlocks;
    CompilationResult<SemanticCompilation> _duplicateEvent;

    void Because()
    {
        _result = BindProjection("projection Orders => OrderView", "from OrderShipped\n  label = $eventContext.causedBy.name\n  key literal \"00000000-0000-0000-0000-000000000001\"");
        _causedBy = BindProjection("projection Orders => OrderView", "from OrderShipped\n  label = $causedBy.name");
        _unknownContextPath = BindProjection("projection Orders => OrderView", "from OrderShipped\n  label = $eventContext.causationId");
        _variant = BindProjection("projection Orders => OrderView", "variant Open\n  enters on OrderPlaced");
        _sequence = BindProjection("projection Orders => OrderView", "sequence audit\nfrom OrderShipped");
        _rootParentKey = BindProjection("projection Orders => OrderView", "from OrderShipped\n  parent carrier");
        _numericLiteralKey = BindProjection("projection Orders => OrderView", "from OrderShipped key 5");
        _twoEveryBlocks = BindProjection("projection Orders => OrderView", "every\n  count events\nevery\n  lastSeen = $eventContext.occurred");
        _duplicateEvent = BindProjection("projection Orders => OrderView", "from OrderShipped\nremove with OrderShipped");
    }

    [Fact] void should_admit_the_caused_by_event_context_path_and_a_text_literal_key() => _result.Success.ShouldBeTrue();
    [Fact] void should_point_caused_by_at_the_event_context_path() => Message(_causedBy).ShouldContain("$eventContext.causedBy.name");
    [Fact] void should_cite_the_chronicle_issue_for_caused_by() => Message(_causedBy).ShouldContain("Cratis/Chronicle#4119");
    [Fact] void should_reject_an_event_context_path_chronicle_does_not_have() => Message(_unknownContextPath).ShouldContain("$eventContext.causationId");
    [Fact] void should_reject_a_variant_citing_its_issues() => Message(_variant).ShouldContain("#185");
    [Fact] void should_say_why_a_sequence_is_not_portable() => Message(_sequence).ShouldContain("realization concern");
    [Fact] void should_reject_a_parent_key_outside_children() => Message(_rootParentKey).ShouldContain("parent key outside a children block");
    [Fact] void should_reject_a_literal_key_that_is_not_text() => Message(_numericLiteralKey).ShouldContain("must be text");
    [Fact] void should_reject_a_second_every_block_on_one_level() => Message(_twoEveryBlocks).ShouldContain("more than one 'every' or 'all'");
    [Fact] void should_reject_an_event_used_twice_on_one_level() => Message(_duplicateEvent).ShouldContain("can only be used once at each level");

    static string Message(CompilationResult<SemanticCompilation> result) =>
        string.Join('\n', result.Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error).Select(_ => _.Message));
}
