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
        AppendStateMachineResults(application, compilation, results);
        AppendWorkflowResults(application, compilation, results);
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
    public static (string State, ImmutableArray<SemanticStateEntry> Effects) Derive(SemanticExecutionPlan plan, SemanticStateMachine machine, ImmutableArray<SemanticFact> facts, SemanticValue key)
    {
        var derived = DeriveState(plan, machine, facts, key);
        return (derived.State, derived.Effects);
    }

    static void AppendStateMachineResults(SemanticApplication application, SemanticExecutionPlanCompilation compilation, ImmutableArray<SemanticLifecycleSpecificationResult>.Builder results)
    {
        foreach (var machine in application.StateMachines)
        {
            foreach (var specification in machine.Specifications)
            {
                results.Add(compilation.Plan is null
                    ? new(machine.Name, specification.Name, ["The model cannot be planned for execution."])
                    : new(machine.Name, specification.Name, RunStateMachine(compilation.Plan, machine, specification)));
            }
        }
    }

    static void AppendWorkflowResults(SemanticApplication application, SemanticExecutionPlanCompilation compilation, ImmutableArray<SemanticLifecycleSpecificationResult>.Builder results)
    {
        foreach (var workflow in application.Workflows)
        {
            foreach (var specification in workflow.Specifications)
            {
                results.Add(compilation.Plan is null
                    ? new(workflow.Name, specification.Name, ["The model cannot be planned for execution."])
                    : new(workflow.Name, specification.Name, RunWorkflow(compilation.Plan, workflow, specification)));
            }
        }
    }

    static (string State, ImmutableArray<SemanticStateEntry> Effects, List<string> Problems) DeriveState(SemanticExecutionPlan plan, SemanticStateMachine machine, ImmutableArray<SemanticFact> facts, SemanticValue key)
    {
        var problems = new List<string>();
        var state = machine.States.Single(candidate => candidate.IsInitial).Name;
        var effects = ImmutableArray<SemanticStateEntry>.Empty;
        for (var index = 0; index < facts.Length; index++)
        {
            effects = [];
            var fact = facts[index];
            if (!SemanticValueRules.AreEqual(fact.Destination, key)) continue;
            var (values, problem) = ModelValues(plan, machine.ReadModel, facts[..(index + 1)], key);
            if (problem is not null) problems.Add(problem);
            var transition = FindTransition(machine, state, fact, values);
            if (transition is null) continue;
            state = transition.To;
            effects = machine.States.Single(candidate => candidate.Name == state).Entering;
        }

        return (state, effects, problems);
    }

    static SemanticStateTransition? FindTransition(SemanticStateMachine machine, string state, SemanticFact fact, Dictionary<SemanticId, SemanticValue>? values)
    {
        var allowed = machine.Transitions.Where(transition => transition.From == state).Select(transition => transition.To).ToHashSet(StringComparer.Ordinal);
        return machine.Transitions.Where(transition => transition.Event == fact.EventContract)
            .FirstOrDefault(transition => allowed.Contains(transition.To) && GuardMatches(transition, values));
    }

    static bool GuardMatches(SemanticStateTransition transition, Dictionary<SemanticId, SemanticValue>? values)
    {
        if (transition.Guard is null) return true;
        return values is not null && SemanticConditionEvaluator.Evaluate(transition.Guard, values);
    }

    static ImmutableArray<string> RunStateMachine(SemanticExecutionPlan plan, SemanticStateMachine machine, SemanticLifecycleSpecification specification)
    {
        var failures = ImmutableArray.CreateBuilder<string>();
        if (specification.WhenAppended is null) return ["A state machine specification appends an event."];
        var facts = Facts(plan, specification, out var rejection);
        if (rejection is not null) return [rejection];
        var key = facts[^1].Destination;
        var (state, effects, problems) = DeriveState(plan, machine, facts, key);
        failures.AddRange(problems.Distinct());
        foreach (var expectation in specification.Then.Where(expectation => expectation.Kind == SemanticLifecycleExpectationKind.State))
        {
            if (state != expectation.Target) failures.Add($"Expected state '{expectation.Target}', but '{machine.Name}' is in '{state}'.");
        }

        string[] expectedSends = [.. specification.Then.Where(expectation => expectation.Kind == SemanticLifecycleExpectationKind.Sends).Select(expectation => CommandName(plan, expectation.Command))];
        string[] actualSends = [.. effects.Where(effect => effect.Kind == SemanticStateEntryKind.SendCommand).Select(effect => CommandName(plan, effect.Command))];
        Compare(failures, "invokes", expectedSends, actualSends);
        string[] expectedStarts = [.. specification.Then.Where(expectation => expectation.Kind == SemanticLifecycleExpectationKind.Starts).Select(expectation => expectation.Target!)];
        string[] actualStarts = [.. effects.Where(effect => effect.Kind == SemanticStateEntryKind.StartWorkflow).Select(effect => effect.Workflow!)];
        Compare(failures, "starts", expectedStarts, actualStarts);
        Compare(failures, "ability invocations", ExpectedAbilities(specification), [.. effects.Where(effect => effect.Kind == SemanticStateEntryKind.InvokeAbility).Select(effect => AbilityName(plan, effect.System, effect.Ability!))]);
        return failures.ToImmutable();
    }

    static ImmutableArray<string> RunWorkflow(SemanticExecutionPlan plan, SemanticWorkflow workflow, SemanticLifecycleSpecification specification)
    {
        var failures = ImmutableArray.CreateBuilder<string>();
        var facts = Facts(plan, specification, out var rejection);
        if (rejection is not null) return [rejection];
        var key = specification.WhenAppended is not null ? facts[^1].Destination : specification.WhenStarted ?? SemanticValue.Null;
        var trigger = FindTrigger(plan, workflow, specification, facts);
        var (values, problem) = ModelValues(plan, workflow.Subject, facts, key);
        var run = new Run(workflow, specification, values);
        if (problem is not null) failures.Add(problem);
        if (trigger is not null) run.Walk(trigger.Name);
        else run.Started = false;
        foreach (var expectation in specification.Then)
        {
            var failure = WorkflowExpectationFailure(run, expectation);
            if (failure is not null) failures.Add(failure);
        }

        string[] expectedSends = [.. specification.Then.Where(expectation => expectation.Kind == SemanticLifecycleExpectationKind.Sends).Select(expectation => CommandName(plan, expectation.Command))];
        Compare(failures, "invokes", expectedSends, [.. run.Sent.Select(command => CommandName(plan, command))]);
        Compare(failures, "ability invocations", ExpectedAbilities(specification), [.. run.Invoked.Select(call => AbilityName(plan, call.System, call.Ability))]);
        return failures.ToImmutable();
    }

    static SemanticWorkflowNode? FindTrigger(SemanticExecutionPlan plan, SemanticWorkflow workflow, SemanticLifecycleSpecification specification, ImmutableArray<SemanticFact> facts)
    {
        if (specification.WhenAppended is null)
        {
            return workflow.Nodes.FirstOrDefault(node => node.Kind == SemanticWorkflowNodeKind.Trigger && node.TriggerKind is SemanticWorkflowTriggerKind.Manual or SemanticWorkflowTriggerKind.Schedule);
        }

        return workflow.Nodes.Where(node => node.Kind == SemanticWorkflowNodeKind.Trigger).FirstOrDefault(node => node.TriggerKind switch
        {
            SemanticWorkflowTriggerKind.Event => node.Event == facts[^1].EventContract,
            SemanticWorkflowTriggerKind.StateEntered => StateEntered(plan, workflow, node, facts),
            _ => false
        });
    }

    static string? WorkflowExpectationFailure(Run run, SemanticLifecycleExpectation expectation) => expectation.Kind switch
    {
        SemanticLifecycleExpectationKind.Completes when run.End != RunEnd.Completed => $"Expected the run to complete, but {run.Describe()}.",
        SemanticLifecycleExpectationKind.Fails when run.End != RunEnd.Failed || run.Node != expectation.Target => $"Expected the run to fail at '{expectation.Target}', but {run.Describe()}.",
        SemanticLifecycleExpectationKind.Waits when run.End != RunEnd.Waiting || run.Node != expectation.Target => $"Expected the run to wait at '{expectation.Target}', but {run.Describe()}.",
        _ => null
    };

    static bool StateEntered(SemanticExecutionPlan plan, SemanticWorkflow workflow, SemanticWorkflowNode node, ImmutableArray<SemanticFact> after)
    {
        var machine = plan.Model.Application.StateMachines.FirstOrDefault(candidate => candidate.Name == node.StateMachine);
        if (machine is null || machine.ReadModel != workflow.Subject) return false;
        var key = after[^1].Destination;
        var before = after[..^1];
        return Derive(plan, machine, before, key).State != node.State && Derive(plan, machine, after, key).State == node.State;
    }

    static string[] ExpectedAbilities(SemanticLifecycleSpecification specification) =>
        [.. specification.Then.Where(expectation => expectation.Kind == SemanticLifecycleExpectationKind.Invokes).Select(expectation => expectation.Target!)];

    static string AbilityName(SemanticExecutionPlan plan, SemanticId system, string ability) =>
        $"{plan.Model.Application.Systems.FirstOrDefault(candidate => candidate.Id == system)?.Name ?? system.ToString()}.{ability}";

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
    static (Dictionary<SemanticId, SemanticValue>? Values, string? Problem) ModelValues(SemanticExecutionPlan plan, SemanticId readModel, ImmutableArray<SemanticFact> facts, SemanticValue key)
    {
        if (facts.IsEmpty) return (null, null);
        var established = new SemanticEvaluator().EstablishSpecificationWorld(plan, facts, default);
        if (established is not SemanticAccepted accepted)
        {
            return (null, established switch
            {
                SemanticUnsupported unsupported => $"The facts cannot be projected: {unsupported.Details}",
                SemanticRejected rejected => $"The facts are rejected: {rejected.Details}",
                _ => "The facts cannot be projected."
            });
        }
        var instances = accepted.World.ReadModels.Where(instance => instance.ReadModel == readModel).ToArray();
        var instance = instances.FirstOrDefault(candidate => SemanticValueRules.AreEqual(candidate.Key, key));
        return (instance?.Values.ToDictionary(value => value.TargetProperty, value => value.Value), null);
    }

    sealed class Run(SemanticWorkflow workflow, SemanticLifecycleSpecification specification, Dictionary<SemanticId, SemanticValue>? subject)
    {
        readonly Dictionary<string, SemanticWorkflowNode> _nodes = workflow.Nodes.ToDictionary(node => node.Name, StringComparer.Ordinal);
        readonly Dictionary<string, int> _arrivals = new(StringComparer.Ordinal);
        readonly List<string> _pendingJoins = [];
        readonly HashSet<string> _completedJoins = new(StringComparer.Ordinal);
        int _iterations = 1;

        public List<SemanticId> Sent { get; } = [];

        public List<(SemanticId System, string Ability)> Invoked { get; } = [];

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
            if (!Step(start)) return;

            // A branch ending at a join does not complete the run until every reached join is satisfied.
            if (_pendingJoins.Count > 0) Suspend(_pendingJoins[0]);
            else End = RunEnd.Completed;
        }

        static bool IsFailureOutlet(string? outlet) =>
            string.Equals(outlet, "failed", StringComparison.Ordinal) || string.Equals(outlet, "refused", StringComparison.Ordinal) || string.Equals(outlet, "rejected", StringComparison.Ordinal) || string.Equals(outlet, "expired", StringComparison.Ordinal);

        // Returns true while the run is still going.
        bool Step(string name)
        {
            var guard = 0;
            while (guard++ < 10_000)
            {
                var node = _nodes[name];
                var resolution = ResolveOutlet(node);
                if (resolution.BranchResult is { } branchResult) return branchResult;
                var continuation = FollowEdge(node, resolution.Outlet);
                if (continuation.BranchResult is { } terminalResult) return terminalResult;
                name = continuation.Next!;
            }

            End = RunEnd.Failed;
            Node = name;
            return false;
        }

        // A null branch result follows an edge; a value ends this branch without changing global completion.
        (string? Outlet, bool? BranchResult) ResolveOutlet(SemanticWorkflowNode node) => node.Kind switch
        {
            SemanticWorkflowNodeKind.Trigger => ("next", null),
            SemanticWorkflowNodeKind.Decision => (subject is not null && SemanticConditionEvaluator.Evaluate(node.Condition!, subject) ? node.WhenTrue : node.WhenFalse, null),
            SemanticWorkflowNodeKind.ForEach => FanOut(node) ? ("completed", null) : (null, false),
            _ => ResolveActivityOutlet(node)
        };

        (string? Outlet, bool? BranchResult) ResolveActivityOutlet(SemanticWorkflowNode node)
        {
            switch (node.Kind)
            {
                case SemanticWorkflowNodeKind.Action:
                    return (Act(node), null);
                case SemanticWorkflowNodeKind.Compute:
                    return ResolveOutcome(node);
                case SemanticWorkflowNodeKind.Wait when node.Duration is not null:
                    return ("resumed", null);
                case SemanticWorkflowNodeKind.Wait or SemanticWorkflowNodeKind.Approval:
                    return ResolveOutcome(node);
                default:
                    var outlet = Join(node, out var ends);
                    return (outlet, ends ? true : null);
            }
        }

        string? ComputeDefault(SemanticWorkflowNode node) =>
            workflow.Edges.Any(edge => edge.From == node.Name && edge.Outlet == "next") ? "next" : null;

        (string? Outlet, bool? BranchResult) ResolveOutcome(SemanticWorkflowNode node)
        {
            var outlet = specification.Outcomes.FirstOrDefault(outcome => outcome.Node == node.Name)?.Outlet;
            if (outlet is null && node.Kind == SemanticWorkflowNodeKind.Compute) outlet = ComputeDefault(node);
            if (outlet is not null) return (outlet, null);
            Suspend(node.Name);
            return (null, false);
        }

        (string? Next, bool? BranchResult) FollowEdge(SemanticWorkflowNode node, string? outlet)
        {
            var edge = workflow.Edges.FirstOrDefault(candidate => candidate.From == node.Name && candidate.Outlet == outlet);
            if (edge is not null) return (edge.To, null);
            if (!IsFailureOutlet(outlet)) return (null, true);
            End = RunEnd.Failed;
            Node = node.Name;
            return (null, false);
        }

        string Act(SemanticWorkflowNode node)
        {
            var outlet = specification.Outcomes.FirstOrDefault(outcome => outcome.Node == node.Name)?.Outlet ?? "next";
            var attempts = outlet == "failed" ? node.MaxAttempts ?? 1 : 1;
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                if (node.Ability is not null) Invoked.Add((node.System, node.Ability));
                else Sent.Add(node.Command);
            }

            return outlet;
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
            ends = true;
            if (_completedJoins.Contains(node.Name)) return "next";
            var arrivals = _arrivals[node.Name] = _arrivals.GetValueOrDefault(node.Name) + 1;
            if (arrivals < JoinThreshold(node))
            {
                RecordPendingJoin(node.Name);
                return "next";
            }

            _pendingJoins.Remove(node.Name);
            _completedJoins.Add(node.Name);
            ends = false;
            return "next";
        }

        int JoinThreshold(SemanticWorkflowNode node) => node.JoinMode switch
        {
            SemanticJoinMode.Any => 1,
            SemanticJoinMode.AtLeast => node.JoinCount!.Value,
            _ => Math.Max(1, workflow.Edges.Count(edge => edge.To == node.Name)) * _iterations
        };

        void RecordPendingJoin(string name)
        {
            if (!_pendingJoins.Contains(name, StringComparer.Ordinal)) _pendingJoins.Add(name);
        }
    }
}
