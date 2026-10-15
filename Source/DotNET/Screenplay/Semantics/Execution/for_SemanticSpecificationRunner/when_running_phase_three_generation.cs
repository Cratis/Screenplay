// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_running_phase_three_generation : given.a_v6_scenario
{
    [Theory]
    [InlineData("2024-03-01T00:30:00+02:00", "0229-0229-A", true)]
    [InlineData("2024-03-01T00:30:00+02:00", "0301-0301-A", false)]
    [InlineData("2024-02-29T23:30:00-02:00", "0301-0301-A", true)]
    [InlineData("2024-02-29T23:30:00-02:00", "0301-0229-A", false)]
    [InlineData(null, "9999-9999-A", true)]
    void should_thread_given_clock_into_generated_pattern_fixtures(string? clock, string fixture, bool accepted)
    {
        var given = clock is null ? "" : $"given clock \"{clock}\"";
        Compile(Source("String", "pattern \"{MM}{dd}-{MM}{dd}-?\"", $"\"{fixture}\"", accepted ? $"then returns \"{fixture}\"" : "then error", given));
        AssertOutcome(accepted);
    }

    [Theory]
    [InlineData("-9", true)]
    [InlineData("9", true)]
    [InlineData("9223372036854775808", false)]
    void should_accept_only_exact_integral_fixtures_in_the_random_interval(string fixture, bool accepted)
    {
        Compile(Source("Int", "random -9223372036854775808..9223372036854775807", fixture, accepted ? $"then returns {fixture}" : "then error"));
        AssertOutcome(accepted);
    }

    [Theory]
    [InlineData(false, "0009", true)]
    [InlineData(true, "0009", false)]
    [InlineData(true, "0000", true)]
    void should_only_enforce_checksum_arithmetic_when_authored(bool validate, string fixture, bool accepted)
    {
        var source = Source("String", "pattern \"#{3}\" checksum luhn", $"\"{fixture}\"", accepted ? $"then returns \"{fixture}\"" : "then error");
        if (validate) source = source.Replace("module M", "  validate checksum luhn\nmodule M", StringComparison.Ordinal);
        Compile(source);
        AssertOutcome(accepted);
    }

    [Fact]
    void should_use_a_generator_fixture_without_executing_its_body()
    {
        Compile(Source("String", "rule Make\n    ```csharp\n    throw new InvalidOperationException();\n    ```", "\"provided\"", "then returns \"provided\""));
        AssertOutcome(true);
    }

    [Fact]
    void should_still_refuse_an_opaque_validator_even_with_a_generator_fixture()
    {
        Compile(Source("String", "rule Make\n    ```csharp\n    return value;\n    ```", "\"provided\"", "then returns \"provided\"")
            .Replace("module M", "  validate\n    ```csharp\n    return true;\n    ```\nmodule M", StringComparison.Ordinal));
        Run("Check").Execution.ShouldBeOfExactType<SemanticUnsupported>();
    }

    void AssertOutcome(bool accepted)
    {
        var run = Run("Check");
        run.Passed.ShouldBeTrue();
        if (accepted) run.Execution.ShouldBeOfExactType<SemanticAccepted>();
        else ((SemanticRejected)run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Contract);
    }

    static string Source(string primitive, string strategy, string fixture, string then, string given = "") =>
        $"concept Value : {primitive}\n  generate {strategy}\nmodule M\n  feature F\n    slice StateChange S\n      command Create\n        value Value generated\n        returns value\n      specification Check\n        {given}\n        when Create\n          generated value = {fixture}\n        {then}";
}
