// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_evaluating_clock_operands.given;

public class a_clock_command : Semantics.for_SemanticModelBinder.when_binding_clock_operands.given.a_clock_binder
{
    protected SemanticExecutionPlan Plan = null!;
    protected SemanticCommand CommandModel = null!;

    protected void Compile(string source)
    {
        var result = BindClock(source);
        var binding = result.Value ?? throw new InvalidOperationException(string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = ExecutableSemanticModel.Create(binding.LanguageVersion, binding.SemanticVersion, binding.Application);
        Plan = SemanticExecutionPlan.Compile(model).Plan!;
        CommandModel = Command(binding);
    }

    protected sealed record RunOptions
    {
        public string? ContextZone { get; init; }

        public ImmutableDictionary<string, string> ContextZones { get; init; } = ImmutableDictionary<string, string>.Empty;

        public string? ConfiguredZone { get; init; }

        public string? ScenarioZone { get; init; }

        public bool WithOccurrence { get; init; } = true;
    }

    SemanticPropertyValue Value(string name, string value) => new(CommandModel.Properties.Single(property => property.Name == name).Id, SemanticValue.Text(value));

    protected SemanticExecutionResult Run(string dueDate, DateTimeOffset at, RunOptions? options = null)
    {
        options ??= new();
        var request = SemanticExecutionRequest.Create(
            CommandModel.Id,
            [Value("id", "3fa85f64-5717-4562-b3fc-2c963f66afa6"), Value("dueDate", dueDate), Value("occurred", "2025-03-02T10:00:00.0000000Z")],
            []) with
        {
            Occurrence = options.WithOccurrence ? new(at, string.Empty, string.Empty, string.Empty) { IsTimeOnly = true } : null,
            ContextTimeZone = options.ContextZone,
            ContextTimeZones = options.ContextZones,
            ConfiguredTimeZone = options.ConfiguredZone,
            ScenarioTimeZone = options.ScenarioZone
        };
        return new SemanticEvaluator().Execute(Plan, SemanticWorld.Empty, request);
    }
}
