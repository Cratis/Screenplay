// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        static long Seconds(WorkflowDurationSyntax duration) => duration.Amount * duration.Unit switch
        {
            IntervalUnit.Seconds => 1L,
            IntervalUnit.Minutes => 60L,
            IntervalUnit.Hours => 3_600L,
            _ => 86_400L
        };

        static IEnumerable<string> LifecycleOutlets(WorkflowNodeSyntax node) => node.NodeKind switch
        {
            WorkflowNodeKind.Action when IsAbilityTarget(node.Command) => [.. node.Edges.Select(edge => edge.Outlet), "next", "failed"],
            WorkflowNodeKind.Compute => [.. node.Edges.Select(edge => edge.Outlet)],
            WorkflowNodeKind.Action => ["next", "failed", "refused"],
            WorkflowNodeKind.Wait => ["resumed", "expired"],
            WorkflowNodeKind.Approval => ["approved", "rejected", "expired"],
            WorkflowNodeKind.ForEach => ["each", "completed"],
            _ => []
        };

        (ImmutableArray<SemanticStateMachine> StateMachines, ImmutableArray<SemanticWorkflow> Workflows) BindLifecycles(ImmutableArray<SemanticModule> modules)
        {
            if (!syntax.StateMachines.Any() && !syntax.Workflows.Any()) return ([], []);
            var commands = modules.SelectMany(module => module.Features).SelectMany(AllBoundSlices).SelectMany(slice => slice.Commands)
                .GroupBy(command => command.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.Ordinal);
            var full = modules.SelectMany(module => module.Features).SelectMany(AllBoundSlices).SelectMany(slice => slice.Commands)
                .GroupBy(command => command.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var machines = syntax.StateMachines.Select(machine => BindStateMachine(machine, commands, full)).OfType<SemanticStateMachine>().ToImmutableArray();
            var workflows = syntax.Workflows.Select(workflow => BindWorkflow(workflow, commands, full)).OfType<SemanticWorkflow>().ToImmutableArray();
            return (machines, workflows);
        }

        SemanticId? EventId(string name, SourceLocation location)
        {
            if (_events.TryGetValue(ShortName(name), out var @event)) return @event.Contract.Id;
            Error(DiagnosticCodes.InvalidSemanticBinding, $"Event '{name}' is not declared.", location);
            return null;
        }

        SemanticId? CommandId(Dictionary<string, SemanticId> commands, string name, SourceLocation location)
        {
            if (commands.TryGetValue(ShortName(name), out var id)) return id;
            Error(DiagnosticCodes.InvalidSemanticBinding, $"Command '{name}' is not declared.", location);
            return null;
        }

        BoundReadModel? SubjectOf(string name, SourceLocation location)
        {
            if (_readModels.TryGetValue(name, out var model)) return model;
            Error(DiagnosticCodes.InvalidSemanticBinding, $"Read model '{name}' is not declared.", location);
            return null;
        }

        SemanticStateMachine? BindStateMachine(StateMachineSyntax machine, Dictionary<string, SemanticId> commands, Dictionary<string, SemanticCommand> full)
        {
            var before = _diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            if (SubjectOf(machine.Follows, machine.Location) is not { } followed) return null;
            var states = new List<SemanticState>();
            var transitions = new List<SemanticStateTransition>();
            foreach (var state in machine.States)
            {
                var entering = new List<SemanticStateEntry>();
                foreach (var entry in state.Entering)
                {
                    if (entry.EntryKind == StateEntryKind.Send)
                    {
                        if (CommandId(commands, entry.Target, entry.Location) is { } command)
                        {
                            entering.Add(new(SemanticStateEntryKind.SendCommand, command, null));
                        }
                    }
                    else if (entry.EntryKind == StateEntryKind.Invoke)
                    {
                        if (BindLifecycleAbility($"State '{state.Name}'", entry.Target, entry.Mappings, entry.Location, followed.Properties, null) is { } call)
                        {
                            entering.Add(new(SemanticStateEntryKind.InvokeAbility, default, null) { System = call.System, Ability = call.Ability, Inputs = call.Inputs });
                        }
                    }
                    else if (entry.EntryKind == StateEntryKind.Body)
                    {
                        var requirement = RequireLifecycleBody(
                            SemanticImplementationRole.StateEntryBody,
                            followed.Address,
                            $"{machine.Name}/{state.Name}/{entering.Count}",
                            entry.File,
                            entry.Code);
                        entering.Add(new(SemanticStateEntryKind.RunBody, default, null) { RequirementId = requirement });
                    }
                    else
                    {
                        entering.Add(new(SemanticStateEntryKind.StartWorkflow, default, entry.Target));
                    }
                }

                states.Add(new(state.Name, state.IsInitial, state.IsFinal, [.. entering]));
                foreach (var transition in state.Transitions)
                {
                    if (EventId(transition.Event, transition.Location) is not { } @event) continue;
                    SemanticCondition? guard = null;
                    if (transition.When is not null && (guard = BindCondition(transition.When, followed.Properties)) is null) continue;
                    transitions.Add(new(state.Name, @event, guard, transition.Target));
                }
            }

            var specifications = machine.Specifications.Select(specification => BindLifecycleSpecification(specification, followed, commands, full, machine, null)).OfType<SemanticLifecycleSpecification>().ToImmutableArray();
            return _diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) > before ? null : new(machine.Name, followed.Model.Id, [.. states], [.. transitions]) { Specifications = specifications };
        }

        SemanticWorkflow? BindWorkflow(WorkflowSyntax workflow, Dictionary<string, SemanticId> commands, Dictionary<string, SemanticCommand> full)
        {
            var before = _diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            if (SubjectOf(workflow.Subject, workflow.Location) is not { } subject) return null;
            var nodes = new List<SemanticWorkflowNode>();
            var edges = new List<SemanticWorkflowEdge>();
            var results = WorkflowNodeResults(workflow);
            foreach (var node in workflow.Nodes)
            {
                if (BindWorkflowNode(node, subject, commands, workflow, results) is { } bound) nodes.Add(bound);
                edges.AddRange(node.Edges.Where(edge => edge.Target.Length > 0).Select(edge => new SemanticWorkflowEdge(node.Name, edge.Outlet, edge.Target)));
            }

            var specifications = workflow.Specifications.Select(specification => BindLifecycleSpecification(specification, subject, commands, full, null, workflow)).OfType<SemanticLifecycleSpecification>().ToImmutableArray();
            return _diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) > before ? null : new(workflow.Name, subject.Model.Id, [.. nodes], [.. edges]) { Specifications = specifications };
        }

        SemanticWorkflowNode? BindWorkflowNode(
            WorkflowNodeSyntax node,
            BoundReadModel subject,
            Dictionary<string, SemanticId> commands,
            WorkflowSyntax workflow,
            Dictionary<string, ImmutableArray<SemanticAbilityField>> results)
        {
            switch (node.NodeKind)
            {
                case WorkflowNodeKind.Trigger:
                    var trigger = new SemanticWorkflowNode(node.Name, SemanticWorkflowNodeKind.Trigger);
                    switch (node.TriggerKind)
                    {
                        case WorkflowTriggerKind.Event:
                            return EventId(node.Event!, node.Location) is { } @event ? trigger with { TriggerKind = SemanticWorkflowTriggerKind.Event, Event = @event } : null;
                        case WorkflowTriggerKind.StateEntered:
                            return trigger with { TriggerKind = SemanticWorkflowTriggerKind.StateEntered, StateMachine = node.StateMachine, State = node.State };
                        case WorkflowTriggerKind.Schedule:
                            return node.Schedule switch
                            {
                                IntervalTriggerSourceSyntax interval => trigger with
                                {
                                    TriggerKind = SemanticWorkflowTriggerKind.Schedule,
                                    Every = Seconds(new(interval.Amount, interval.Unit, interval.Location))
                                },
                                ScheduleTriggerSourceSyntax schedule => trigger with
                                {
                                    TriggerKind = SemanticWorkflowTriggerKind.Schedule,
                                    At = (int)schedule.Time.ToTimeSpan().TotalSeconds,
                                    OnDayOfWeek = schedule.DayOfWeek is { } day ? (int)day : null,
                                    OnDayOfMonth = schedule.DayOfMonth
                                },
                                _ => null
                            };
                        default:
                            return trigger with { TriggerKind = SemanticWorkflowTriggerKind.Manual };
                    }

                case WorkflowNodeKind.Action when IsAbilityTarget(node.Command):
                    return BindLifecycleAbility($"Workflow action '{node.Name}'", node.Command!, node.Mappings, node.Location, subject.Properties, results) is { } call
                        ? new(node.Name, SemanticWorkflowNodeKind.Action)
                        {
                            System = call.System,
                            Ability = call.Ability,
                            Inputs = call.Inputs,
                            MaxAttempts = node.Attempts,
                            AttemptTimeout = node.Timeout is { } abilityTimeout ? Seconds(abilityTimeout) : null,
                            RetryDelay = node.RetryAfter is { } abilityRetry ? Seconds(abilityRetry) : null,
                            ExpiresAfter = node.ExpiresAfter is { } abilityExpiry ? Seconds(abilityExpiry) : null
                        }
                        : null;
                case WorkflowNodeKind.Compute:
                    return new(node.Name, SemanticWorkflowNodeKind.Compute)
                    {
                        Returns = results.GetValueOrDefault(node.Name, []),
                        RequirementId = RequireLifecycleBody(SemanticImplementationRole.WorkflowCompute, subject.Address, $"{workflow.Name}/{node.Name}", node.File, node.Code)
                    };
                case WorkflowNodeKind.Action:
                    return CommandId(commands, node.Command!, node.Location) is { } command
                        ? new(node.Name, SemanticWorkflowNodeKind.Action)
                        {
                            Command = command,
                            MaxAttempts = node.Attempts,
                            AttemptTimeout = node.Timeout is { } timeout ? Seconds(timeout) : null,
                            RetryDelay = node.RetryAfter is { } retry ? Seconds(retry) : null
                        }
                        : null;
                case WorkflowNodeKind.Decide when ReadsNodeResult(node, workflow):
                    return null;
                case WorkflowNodeKind.Decide:
                    var edges = node.Edges.ToArray();
                    if (edges.Length != 2) Error(DiagnosticCodes.InvalidSemanticBinding, $"Decide '{node.Name}' needs exactly two outlets.", node.Location);
                    return edges.Length == 2 && BindCondition(node.Condition!, subject.Properties) is { } condition
                        ? new(node.Name, SemanticWorkflowNodeKind.Decision) { Condition = condition, WhenTrue = edges[0].Outlet, WhenFalse = edges[1].Outlet }
                        : null;
                case WorkflowNodeKind.Wait:
                    if (node.Duration is { } duration) return new(node.Name, SemanticWorkflowNodeKind.Wait) { Duration = Seconds(duration) };
                    return EventId(node.Event!, node.Location) is { } waited
                        ? new(node.Name, SemanticWorkflowNodeKind.Wait) { Event = waited, ExpiresAfter = node.ExpiresAfter is { } expires ? Seconds(expires) : null }
                        : null;
                case WorkflowNodeKind.Approval:
                    SemanticApproverKind? kind = null;
                    if ((syntax.Personas ?? []).Any(persona => persona.Name == node.By))
                    {
                        kind = SemanticApproverKind.Persona;
                    }
                    else if (syntax.Policies.Any(policy => policy.Name == node.By))
                    {
                        kind = SemanticApproverKind.Policy;
                    }

                    if (kind is null)
                    {
                        Error(DiagnosticCodes.InvalidSemanticBinding, $"Approver '{node.By}' is neither a persona nor a policy.", node.Location);
                        return null;
                    }

                    return new(node.Name, SemanticWorkflowNodeKind.Approval) { ApproverKind = kind, Approver = node.By, ExpiresAfter = node.ExpiresAfter is { } expiry ? Seconds(expiry) : null };
                case WorkflowNodeKind.ForEach:
                    if (!subject.Properties.TryGetValue(node.Items!, out var items) || !items.Type.IsCollection)
                    {
                        Error(DiagnosticCodes.InvalidSemanticBinding, $"Foreach '{node.Name}' must iterate a collection property of '{subject.Syntax.Name}'.", node.Location);
                        return null;
                    }

                    return new(node.Name, SemanticWorkflowNodeKind.ForEach) { Items = items.Id, MaxParallel = node.Parallel };
                default:
                    return new(node.Name, SemanticWorkflowNodeKind.Join)
                    {
                        JoinMode = node.JoinMode switch { WorkflowJoinMode.Any => SemanticJoinMode.Any, WorkflowJoinMode.AtLeast => SemanticJoinMode.AtLeast, _ => SemanticJoinMode.All },
                        JoinCount = node.JoinCount
                    };
            }
        }

        SemanticLifecycleSpecification? BindLifecycleSpecification(
            LifecycleSpecificationSyntax specification,
            BoundReadModel subject,
            Dictionary<string, SemanticId> commands,
            Dictionary<string, SemanticCommand> full,
            StateMachineSyntax? machine,
            WorkflowSyntax? workflow)
        {
            var owner = machine?.Name ?? workflow!.Name;
            var failed = false;
            void Fail(string message, SourceLocation location)
            {
                failed = true;
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Specification '{specification.Name}' of '{owner}': {message}", location);
            }

            var subjectKey = subject.Model.Properties.FirstOrDefault(property => property.IsIdentifier)?.Type;
            var given = ImmutableArray.CreateBuilder<SemanticSpecificationEvent>();
            foreach (var step in specification.Given)
            {
                if (BindSpecificationEvent(step, full, historicalFact: true, subjectKey: subjectKey) is { } bound) given.Add(bound);
                else failed = true;
            }

            SemanticSpecificationEvent? appended = null;
            if (specification.WhenAppended is not null && (appended = BindSpecificationEvent(specification.WhenAppended, full, subjectKey: subjectKey)) is null) failed = true;
            SemanticValue? started = null;
            if (specification.WhenStarted is { } key)
            {
                var identifier = subject.Model.Properties.FirstOrDefault(property => property.IsIdentifier);
                if (workflow is null) Fail("'when start' belongs to a workflow specification.", key.Location);
                else if (identifier is null || BindConcreteValue(key, identifier.Type, "workflow subject key", false) is not { } value) Fail("'when start' needs a literal subject key of the read model's identifier type.", key.Location);
                else started = value;
            }

            if (workflow is not null && specification.WhenAppended is null && specification.WhenStarted is null) Fail("A workflow specification states 'when append <Event>' or 'when start <key>'.", specification.Location);
            if (machine is not null && specification.WhenAppended is null) Fail("A state machine specification states 'when append <Event>'.", specification.Location);
            var nodes = workflow?.Nodes.ToDictionary(node => node.Name, StringComparer.Ordinal);
            var outcomes = ImmutableArray.CreateBuilder<SemanticLifecycleOutcome>();
            foreach (var outcome in specification.Outcomes)
            {
                if (nodes is null) Fail("Outcomes belong to workflow specifications.", outcome.Location);
                else if (!nodes.TryGetValue(outcome.Node, out var node)) Fail($"Outcome names node '{outcome.Node}', which the workflow does not declare.", outcome.Location);
                else if (node.NodeKind is WorkflowNodeKind.Trigger or WorkflowNodeKind.Decide or WorkflowNodeKind.Join) Fail($"Node '{outcome.Node}' takes no stated outcome.", outcome.Location);
                else if (!LifecycleOutlets(node).Contains(outcome.Outlet)) Fail($"Node '{outcome.Node}' has no outlet '{outcome.Outlet}'.", outcome.Location);
                else outcomes.Add(new(outcome.Node, outcome.Outlet));
            }

            var then = ImmutableArray.CreateBuilder<SemanticLifecycleExpectation>();
            foreach (var expectation in specification.Then)
            {
                switch (expectation.Expectation)
                {
                    case LifecycleExpectationKind.State when machine?.States.Any(state => state.Name == expectation.Target) == true:
                    case LifecycleExpectationKind.Starts when syntax.Workflows.Any(candidate => candidate.Name == expectation.Target):
                    case LifecycleExpectationKind.Completes when workflow is not null:
                    case LifecycleExpectationKind.Fails or LifecycleExpectationKind.Waits when nodes?.ContainsKey(expectation.Target!) == true:
                        var expectationKind = expectation.Expectation switch
                        {
                            LifecycleExpectationKind.State => SemanticLifecycleExpectationKind.State,
                            LifecycleExpectationKind.Starts => SemanticLifecycleExpectationKind.Starts,
                            LifecycleExpectationKind.Completes => SemanticLifecycleExpectationKind.Completes,
                            LifecycleExpectationKind.Fails => SemanticLifecycleExpectationKind.Fails,
                            _ => SemanticLifecycleExpectationKind.Waits
                        };
                        then.Add(new(expectationKind, expectation.Target));
                        break;
                    case LifecycleExpectationKind.Sends when IsAbilityTarget(expectation.Target):
                        if (FindAbility(expectation.Target!) is null)
                        {
                            Fail($"'{expectation.Target}' is not one declared ability.", expectation.Location);
                            failed = true;
                        }
                        else
                        {
                            then.Add(new(SemanticLifecycleExpectationKind.Invokes, expectation.Target));
                        }

                        break;
                    case LifecycleExpectationKind.Sends:
                        if (CommandId(commands, expectation.Target!, expectation.Location) is { } command) then.Add(new(SemanticLifecycleExpectationKind.Sends, expectation.Target) { Command = command });
                        else failed = true;
                        break;
                    default:
                        Fail($"'{expectation.Expectation}' {expectation.Target} is not something a {(machine is not null ? "state machine" : "workflow")} specification can expect.", expectation.Location);
                        break;
                }
            }

            return failed ? null : new(specification.Name, given.ToImmutable()) { WhenAppended = appended, WhenStarted = started, Outcomes = outcomes.ToImmutable(), Then = then.ToImmutable() };
        }
    }
}
