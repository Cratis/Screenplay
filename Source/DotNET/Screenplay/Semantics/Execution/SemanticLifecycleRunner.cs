// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Represents the verdict of one state machine or workflow specification.
/// </summary>
/// <param name="Owner">The name of the state machine or workflow the specification belongs to.</param>
/// <param name="Name">The specification name.</param>
/// <param name="Failures">What did not hold; empty when the specification passed.</param>
public sealed record SemanticLifecycleSpecificationResult(string Owner, string Name, ImmutableArray<string> Failures)
{
    /// <summary>
    /// Gets whether the specification passed.
    /// </summary>
    public bool Passed => Failures.IsEmpty;
}

/// <summary>
/// Runs the specifications of state machines and workflows in the reference evaluator.
/// </summary>
/// <remarks>
/// A state machine is derived from the facts: for each event it takes the first declared transition on the event whose guard
/// holds over the followed read model as it is after the event and whose target the current state allows (a state allows the
/// targets of the transitions declared under it); anything else is ignored. A workflow run is stepped deterministically:
/// decisions are computed from the subject read model, actions send their command, and a wait or approval takes the outlet the
/// specification states with <c>outcome</c> or, with none, suspends the run.
/// </remarks>
public static class SemanticLifecycleRunner
{
    enum RunEnd
    {
        NotStarted,
        Completed,
        Failed,
        Waiting
    }

    /// <summary>
    /// Runs every state machine and workflow specification of a model.
    /// </summary>
    /// <param name="model">The validated executable semantic model.</param>
    /// <returns>One result per specification.</returns>
    public static ImmutableArray<SemanticLifecycleSpecificationResult> RunAll(ExecutableSemanticModel model)
    {
        var results = ImmutableArray.CreateBuilder<SemanticLifecycleSpecificationResult>();
        var application = model.Application;
        if (application.StateMachines.All(machine => machine.Specifications.IsDefaultOrEmpty) && application.Workflows.All(workflow => workflow.Specifications.IsDefaultOrEmpty)) return [];
        var compilation = SemanticExecutionPlan.CompileForSpecifications(model);
        foreach (var machine in application.StateMachines)
        {
            foreach (var specification in machine.Specifications)
            {
                results.Add(compilation.Plan is null
                    ? new(machine.Name, specification.Name, ["The model cannot be planned for execution."])
                    : new(machine.Name, specification.Name, RunStateMachine(compilation.Plan, machine, specification)));
            }
        }

        foreach (var workflow in application.Workflows)
        {
            foreach (var specification in workflow.Specifications)
            {
                results.Add(compilation.Plan is null
                    ? new(workflow.Name, specification.Name, ["The model cannot be planned for execution."])
                    : new(workflow.Name, specification.Name, RunWorkflow(compilation.Plan, workflow, specification)));
            }
        }

        return results.ToImmutable();
    }

    /// <summary>
    /// Derives the current state of a state machine instance from the facts that happened to it.
    /// </summary>
    /// <param name="plan">The execution plan.</param>
    /// <param name="machine">The state machine.</param>
    /// <param name="facts">The facts in order.</param>
    /// <param name="key">The identity of the instance.</param>
    /// <returns>The current state and the entering effects of the last fact.</returns>
    public static (string State, ImmutableArray<SemanticStateEntry> Effects) Derive(SemanticExecutionPlan plan, SemanticStateMachine machine, ImmutableArray<SemanticFact> facts, SemanticValue key) =>
        Derive(plan, machine, facts, key, []);

    static (string State, ImmutableArray<SemanticStateEntry> Effects) Derive(SemanticExecutionPlan plan, SemanticStateMachine machine, ImmutableArray<SemanticFact> facts, SemanticValue key, List<string> problems)
    {
        var state = machine.States.Single(candidate => candidate.IsInitial).Name;
        var effects = ImmutableArray<SemanticStateEntry>.Empty;
        for (var index = 0; index < facts.Length; index++)
        {
            effects = [];
            var fact = facts[index];
            if (!SemanticValueRules.AreEqual(fact.Destination, key)) continue;
            var values = ModelValues(plan, machine.ReadModel, facts[..(index + 1)], key, problems);
            var allowed = machine.Transitions.Where(transition => transition.From == state).Select(transition => transition.To).ToHashSet(StringComparer.Ordinal);
            foreach (var transition in machine.Transitions.Where(transition => transition.Event == fact.EventContract))
            {
                if (!allowed.Contains(transition.To)) continue;
                if (transition.Guard is not null && (values is null || !SemanticConditionEvaluator.Evaluate(transition.Guard, values))) continue;
                state = transition.To;
                effects = machine.States.Single(candidate => candidate.Name == state).Entering;
                break;
            }
        }

        return (state, effects);
    }

    static ImmutableArray<string> RunStateMachine(SemanticExecutionPlan plan, SemanticStateMachine machine, SemanticLifecycleSpecification specification)
    {
        var failures = ImmutableArray.CreateBuilder<string>();
        if (specification.WhenAppended is null) return ["A state machine specification appends an event."];
        var facts = Facts(plan, specification, out var rejection);
        if (rejection is not null) return [rejection];
        var key = facts[^1].Destination;
        var problems = new List<string>();
        var (state, effects) = Derive(plan, machine, facts, key, problems);
        failures.AddRange(problems.Distinct());
        foreach (var expectation in specification.Then.Where(expectation => expectation.Kind == SemanticLifecycleExpectationKind.State))
        {
            if (state != expectation.Target) failures.Add($"Expected state '{expectation.Target}', but '{machine.Name}' is in '{state}'.");
        }

        string[] expectedSends = [.. specification.Then.Where(expectation => expectation.Kind == SemanticLifecycleExpectationKind.Sends).Select(expectation => CommandName(plan, expectation.Command))];
        string[] actualSends = [.. effects.Where(effect => effect.Kind == SemanticStateEntryKind.SendCommand).Select(effect => CommandName(plan, effect.Command))];
        Compare(failures, "sends", expectedSends, actualSends);
        string[] expectedStarts = [.. specification.Then.Where(expectation => expectation.Kind == SemanticLifecycleExpectationKind.Starts).Select(expectation => expectation.Target!)];
        string[] actualStarts = [.. effects.Where(effect => effect.Kind == SemanticStateEntryKind.StartWorkflow).Select(effect => effect.Workflow!)];
        Compare(failures, "starts", expectedStarts, actualStarts);
        return failures.ToImmutable();
    }

    static ImmutableArray<string> RunWorkflow(SemanticExecutionPlan plan, SemanticWorkflow workflow, SemanticLifecycleSpecification specification)
    {
        var failures = ImmutableArray.CreateBuilder<string>();
        var facts = Facts(plan, specification, out var rejection);
        if (rejection is not null) return [rejection];
        SemanticValue key;
        SemanticWorkflowNode? trigger = null;
        if (specification.WhenAppended is not null)
        {
            key = facts[^1].Destination;
            var before = facts[..^1];
            trigger = workflow.Nodes.Where(node => node.Kind == SemanticWorkflowNodeKind.Trigger).FirstOrDefault(node => node.TriggerKind switch
            {
                SemanticWorkflowTriggerKind.Event => node.Event == facts[^1].EventContract,
                SemanticWorkflowTriggerKind.StateEntered => StateEntered(plan, workflow, node, before, facts, key),
                _ => false
            });
        }
        else
        {
            key = specification.WhenStarted ?? SemanticValue.Null;
            trigger = workflow.Nodes.FirstOrDefault(node => node.Kind == SemanticWorkflowNodeKind.Trigger && node.TriggerKind is SemanticWorkflowTriggerKind.Manual or SemanticWorkflowTriggerKind.Schedule);
        }

        var problems = new List<string>();
        var run = new Run(workflow, specification, ModelValues(plan, workflow.Subject, facts, key, problems));
        failures.AddRange(problems.Distinct());
        if (trigger is not null) run.Walk(trigger.Name);
        else run.Started = false;
        foreach (var expectation in specification.Then)
        {
            switch (expectation.Kind)
            {
                case SemanticLifecycleExpectationKind.Completes when run.End != RunEnd.Completed:
                    failures.Add($"Expected the run to complete, but {run.Describe()}.");
                    break;
                case SemanticLifecycleExpectationKind.Fails when run.End != RunEnd.Failed || run.Node != expectation.Target:
                    failures.Add($"Expected the run to fail at '{expectation.Target}', but {run.Describe()}.");
                    break;
                case SemanticLifecycleExpectationKind.Waits when run.End != RunEnd.Waiting || run.Node != expectation.Target:
                    failures.Add($"Expected the run to wait at '{expectation.Target}', but {run.Describe()}.");
                    break;
            }
        }

        string[] expectedSends = [.. specification.Then.Where(expectation => expectation.Kind == SemanticLifecycleExpectationKind.Sends).Select(expectation => CommandName(plan, expectation.Command))];
        Compare(failures, "sends", expectedSends, [.. run.Sent.Select(command => CommandName(plan, command))]);
        return failures.ToImmutable();
    }

    static bool StateEntered(SemanticExecutionPlan plan, SemanticWorkflow workflow, SemanticWorkflowNode node, ImmutableArray<SemanticFact> before, ImmutableArray<SemanticFact> after, SemanticValue key)
    {
        var machine = plan.Model.Application.StateMachines.FirstOrDefault(candidate => candidate.Name == node.StateMachine);
        if (machine is null || machine.ReadModel != workflow.Subject) return false;
        return Derive(plan, machine, before, key).State != node.State && Derive(plan, machine, after, key).State == node.State;
    }

    static string CommandName(SemanticExecutionPlan plan, SemanticId command) => plan.Commands.TryGetValue(command, out var declared) ? declared.Name : command.ToString();

    static void Compare(ImmutableArray<string>.Builder failures, string what, string[] expected, string[] actual)
    {
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal)) failures.Add($"Expected {what} [{string.Join(", ", expected)}], but got [{string.Join(", ", actual)}].");
    }

    static ImmutableArray<SemanticFact> Facts(SemanticExecutionPlan plan, SemanticLifecycleSpecification specification, out string? rejection)
    {
        rejection = null;
        try
        {
            return [.. specification.Given.Concat(specification.WhenAppended is null ? [] : [specification.WhenAppended])
                .Select(value => new SemanticFact(value.EventContract, value.EventSource?.Value ?? SemanticValue.Null, value.Values)
                {
                    Context = value.EventSource is null ? null : new(value.EventSource),
                    Route = plan.FormatFixtureRoute(value.Route)
                })
                .Select(fact => SemanticCaptureEvaluation.TakeEventSourceKey(plan, fact))];
        }
        catch (InvalidSemanticContract exception)
        {
            rejection = exception.Message;
            return [];
        }
    }

    // The followed read model as it is after the facts, keyed by property identity; null when the facts build no instance of it.
    static Dictionary<SemanticId, SemanticValue>? ModelValues(SemanticExecutionPlan plan, SemanticId readModel, ImmutableArray<SemanticFact> facts, SemanticValue key, List<string> problems)
    {
        if (facts.IsEmpty) return null;
        var established = new SemanticEvaluator().EstablishSpecificationWorld(plan, facts, default);
        if (established is not SemanticAccepted accepted)
        {
            problems.Add(established switch
            {
                SemanticUnsupported unsupported => $"The facts cannot be projected: {unsupported.Details}",
                SemanticRejected rejected => $"The facts are rejected: {rejected.Details}",
                _ => "The facts cannot be projected."
            });
            return null;
        }
        var instances = accepted.World.ReadModels.Where(instance => instance.ReadModel == readModel).ToArray();
        var instance = instances.FirstOrDefault(candidate => SemanticValueRules.AreEqual(candidate.Key, key)) ?? (instances.Length == 1 ? instances[0] : null);
        return instance?.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
    }

    sealed class Run(SemanticWorkflow workflow, SemanticLifecycleSpecification specification, Dictionary<SemanticId, SemanticValue>? subject)
    {
        readonly Dictionary<string, SemanticWorkflowNode> _nodes = workflow.Nodes.ToDictionary(node => node.Name, StringComparer.Ordinal);
        readonly Dictionary<string, int> _arrivals = new(StringComparer.Ordinal);
        int _iterations = 1;

        public List<SemanticId> Sent { get; } = [];

        public RunEnd End { get; private set; } = RunEnd.NotStarted;

        public string? Node { get; private set; }

        public bool Started { get; set; } = true;

        public string Describe() => !Started ? "no run started" : End switch
        {
            RunEnd.Completed => "the run completed",
            RunEnd.Failed => $"the run failed at '{Node}'",
            RunEnd.Waiting => $"the run waits at '{Node}'",
            _ => "the run did not end"
        };

        public void Walk(string start)
        {
            End = RunEnd.Completed;
            Step(start);
        }

        // Returns true while the run is still going.
        bool Step(string name)
        {
            var guard = 0;
            while (guard++ < 10_000)
            {
                var node = _nodes[name];
                string? outlet;
                switch (node.Kind)
                {
                    case SemanticWorkflowNodeKind.Trigger:
                        outlet = "next";
                        break;
                    case SemanticWorkflowNodeKind.Action:
                        var taken = specification.Outcomes.FirstOrDefault(outcome => outcome.Node == node.Name)?.Outlet ?? "next";
                        for (var attempt = 0; attempt < (taken == "failed" ? node.MaxAttempts ?? 1 : 1); attempt++) Sent.Add(node.Command);
                        outlet = taken;
                        break;
                    case SemanticWorkflowNodeKind.Decision:
                        outlet = subject is not null && SemanticConditionEvaluator.Evaluate(node.Condition!, subject) ? node.WhenTrue : node.WhenFalse;
                        break;
                    case SemanticWorkflowNodeKind.Wait when node.Duration is not null:
                        outlet = "resumed";
                        break;
                    case SemanticWorkflowNodeKind.Wait or SemanticWorkflowNodeKind.Approval:
                        outlet = specification.Outcomes.FirstOrDefault(outcome => outcome.Node == node.Name)?.Outlet;
                        if (outlet is null)
                        {
                            Suspend(node.Name);
                            return false;
                        }

                        break;
                    case SemanticWorkflowNodeKind.ForEach:
                        if (!FanOut(node)) return false;
                        outlet = "completed";
                        break;
                    default:
                        outlet = Join(node, out var ends);
                        if (ends) return true;
                        break;
                }

                var edge = workflow.Edges.FirstOrDefault(candidate => candidate.From == node.Name && candidate.Outlet == outlet);
                if (edge is null)
                {
                    if (string.Equals(outlet, "failed", StringComparison.Ordinal) || string.Equals(outlet, "rejected", StringComparison.Ordinal) || string.Equals(outlet, "expired", StringComparison.Ordinal))
                    {
                        End = RunEnd.Failed;
                        Node = node.Name;
                        return false;
                    }

                    return true;
                }

                name = edge.To;
            }

            End = RunEnd.Failed;
            Node = name;
            return false;
        }

        void Suspend(string node)
        {
            End = RunEnd.Waiting;
            Node = node;
        }

        bool FanOut(SemanticWorkflowNode node)
        {
            var items = subject is not null && subject.TryGetValue(node.Items, out var value) && value is SemanticArrayValue array ? array.Values.Length : 0;
            var each = workflow.Edges.FirstOrDefault(edge => edge.From == node.Name && edge.Outlet == "each");
            var outer = _iterations;
            _iterations = Math.Max(1, items);
            if (each is not null)
            {
                for (var item = 0; item < items; item++)
                {
                    if (!Step(each.To)) return false;
                }
            }

            _iterations = outer;
            return true;
        }

        // A join reached in a branch counts as an arrival and ends that branch; once its mode is satisfied it continues.
        string? Join(SemanticWorkflowNode node, out bool ends)
        {
            var arrivals = _arrivals[node.Name] = _arrivals.GetValueOrDefault(node.Name) + 1;
            var expected = Math.Max(1, workflow.Edges.Count(edge => edge.To == node.Name)) * _iterations;
            var satisfied = node.JoinMode switch
            {
                SemanticJoinMode.Any => arrivals >= 1,
                SemanticJoinMode.AtLeast => arrivals >= node.JoinCount,
                _ => arrivals >= expected
            };
            var limit = expected;
            if (node.JoinMode == SemanticJoinMode.Any)
            {
                limit = 1;
            }
            else if (node.JoinMode == SemanticJoinMode.AtLeast)
            {
                limit = node.JoinCount!.Value;
            }

            ends = !satisfied || arrivals > limit;
            return "next";
        }
    }
}
