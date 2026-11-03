// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_evaluating_clock_operands;

public class with_a_configured_application_zone : given.a_clock_command
{
    static readonly DateTimeOffset Instant = new(2025, 3, 1, 23, 30, 0, TimeSpan.Zero);

    SemanticExecutionResult _configured = null!;
    SemanticExecutionResult _default = null!;
    SemanticExecutionResult _empty = null!;
    SemanticExecutionResult _unknown = null!;

    void Establish() => Compile(Source("        validate\n          require dueDate >= today\n        produces Done\n          for id\n", "time zone configurable default \"Asia/Tokyo\"\n"));

    void Because()
    {
        _configured = Run("2025-03-01", Instant, new() { ConfiguredZone = "America/New_York" });
        _default = Run("2025-03-01", Instant);
        _empty = Run("2025-03-01", Instant, new() { ConfiguredZone = " " });
        _unknown = Run("2025-03-01", Instant, new() { ConfiguredZone = "Mars/Olympus" });
    }

    [Fact] void should_let_the_configured_zone_beat_the_declared_default() => _configured.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_use_the_declared_default_when_none_is_supplied() => _default.ShouldBeOfExactType<SemanticRejected>();
    [Fact]
    void should_refuse_an_empty_configured_zone()
    {
        _empty.ShouldBeOfExactType<SemanticRejected>();
        ((SemanticRejected)_empty).Category.ShouldEqual(SemanticRejectionCategory.Contract);
        ((SemanticRejected)_empty).Details.ShouldEqual("The configured application time zone is empty; supply an IANA time zone id or none.");
    }

    [Fact]
    void should_refuse_an_unknown_configured_zone()
    {
        _unknown.ShouldBeOfExactType<SemanticRejected>();
        ((SemanticRejected)_unknown).Category.ShouldEqual(SemanticRejectionCategory.Contract);
        ((SemanticRejected)_unknown).Details.ShouldEqual("The calendar time zone 'Mars/Olympus' is not a known IANA time zone.");
    }
}
