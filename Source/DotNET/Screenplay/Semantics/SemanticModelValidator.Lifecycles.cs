// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    static readonly string[] _decisionReserved = ["next", "failed", "refused", "resumed", "expired", "approved", "rejected", "each", "completed"];

    // ESM v36 constructs: state machines and workflows. A v36 model contains one; a model before v36 contains none.
    internal static bool UsesLifecycles(SemanticApplication application) =>
        !application.StateMachines.IsDefaultOrEmpty || !application.Workflows.IsDefaultOrEmpty;

    internal static string[] Outlets(SemanticWorkflowNodeKind kind) => kind switch
    {
        SemanticWorkflowNodeKind.Trigger or SemanticWorkflowNodeKind.Join => ["next"],
        SemanticWorkflowNodeKind.Action => ["next", "failed", "refused"],
        SemanticWorkflowNodeKind.Wait => ["resumed", "expired"],
        SemanticWorkflowNodeKind.Approval => ["approved", "rejected", "expired"],
        SemanticWorkflowNodeKind.ForEach => ["each", "completed"],
        _ => []
    };

    static void ValidateLifecycleVersion(SemanticApplication application, SemanticVersion version) =>
        ValidateFeatureVersion(
            UsesLifecycles(application),
            version,
            SemanticVersion.V36,
            "An ESM v36 model must declare a state machine or a workflow.",
            "State machines and workflows require ESM v36.");

    private sealed partial class ValidationContext
    {
        void ValidateLifecycles(SemanticApplication application)
        {
            RequireObjects(application.StateMachines, nameof(application.StateMachines), "state machine");
            RequireObjects(application.Workflows, nameof(application.Workflows), "workflow");
            RejectDuplicateNames(application.StateMachines.Select(_ => _.Name), "state machine");
            RejectDuplicateNames(application.Workflows.Select(_ => _.Name), "workflow");
            foreach (var machine in application.StateMachines) ValidateStateMachine(application, machine);
            foreach (var workflow in application.Workflows) ValidateWorkflow(application, workflow);
        }

        void ValidateStateMachine(SemanticApplication application, SemanticStateMachine machine)
        {
            var where = $"state machine '{machine.Name}'";
            if (!_readModels.TryGetValue(machine.ReadModel, out var readModel)) throw new InvalidSemanticContract($"The {where} follows an unknown read model.");
            RejectDuplicateNames(machine.States.Select(_ => _.Name), $"state of {where}");
            if (machine.States.Count(state => state.IsInitial) != 1) throw new InvalidSemanticContract($"The {where} must have exactly one initial state.");
            var states = machine.States.ToDictionary(state => state.Name, StringComparer.Ordinal);
            var properties = Properties(readModel.Properties);
            foreach (var state in machine.States)
            {
                if (state.IsFinal && machine.Transitions.Any(transition => transition.From == state.Name)) throw new InvalidSemanticContract($"The final state '{state.Name}' of {where} has transitions out.");
                foreach (var entry in state.Entering)
                {
                    if (entry.Kind == SemanticStateEntryKind.SendCommand && !_commands.ContainsKey(entry.Command)) throw new InvalidSemanticContract($"State '{state.Name}' of {where} sends an unknown command.");
                    if (entry.Kind == SemanticStateEntryKind.StartWorkflow && !application.Workflows.Any(workflow => workflow.Name == entry.Workflow)) throw new InvalidSemanticContract($"State '{state.Name}' of {where} starts an unknown workflow.");
                }
            }

            foreach (var specification in machine.Specifications) ValidateLifecycleSpecification(specification, where, null, states.Keys);
            foreach (var transition in machine.Transitions)
            {
                if (!states.ContainsKey(transition.From) || !states.ContainsKey(transition.To)) throw new InvalidSemanticContract($"A transition of {where} names an unknown state.");
                if (!_events.ContainsKey(transition.Event)) throw new InvalidSemanticContract($"A transition of {where} is on an unknown event.");
                if (transition.Guard is not null) ValidateCondition(transition.Guard, properties);
            }
        }

        void ValidateWorkflow(SemanticApplication application, SemanticWorkflow workflow)
        {
            var where = $"workflow '{workflow.Name}'";
            if (!_readModels.TryGetValue(workflow.Subject, out var subject)) throw new InvalidSemanticContract($"The {where} is for an unknown read model.");
            RejectDuplicateNames(workflow.Nodes.Select(_ => _.Name), $"node of {where}");
            if (!workflow.Nodes.Any(node => node.Kind == SemanticWorkflowNodeKind.Trigger)) throw new InvalidSemanticContract($"The {where} has no trigger.");
            var nodes = workflow.Nodes.ToDictionary(node => node.Name, StringComparer.Ordinal);
            var properties = Properties(subject.Properties);
            foreach (var node in workflow.Nodes) ValidateWorkflowNode(application, workflow, node, properties);
            foreach (var specification in workflow.Specifications) ValidateLifecycleSpecification(specification, where, nodes.Keys, null);
            var seen = new HashSet<(string, string)>();
            foreach (var edge in workflow.Edges)
            {
                if (!nodes.TryGetValue(edge.From, out var from) || !nodes.TryGetValue(edge.To, out var to)) throw new InvalidSemanticContract($"An edge of {where} names an unknown node.");
                if (!seen.Add((edge.From, edge.Outlet))) throw new InvalidSemanticContract($"Node '{edge.From}' of {where} leaves by outlet '{edge.Outlet}' more than once.");
                if (to.Kind == SemanticWorkflowNodeKind.Trigger) throw new InvalidSemanticContract($"An edge of {where} leads to trigger '{to.Name}'.");
                var legal = from.Kind == SemanticWorkflowNodeKind.Decision ? from.WhenTrue == edge.Outlet || from.WhenFalse == edge.Outlet : Outlets(from.Kind).Contains(edge.Outlet);
                if (!legal) throw new InvalidSemanticContract($"Node '{edge.From}' of {where} has no outlet '{edge.Outlet}'.");
            }
        }

        void ValidateLifecycleSpecification(SemanticLifecycleSpecification specification, string where, IEnumerable<string>? nodes, IEnumerable<string>? states)
        {
            RejectDuplicateNames([specification.Name], $"specification of {where}");
            if (specification.Given.Concat(specification.WhenAppended is null ? [] : [specification.WhenAppended]).Any(step => !_events.ContainsKey(step.EventContract)))
            {
                throw new InvalidSemanticContract($"A specification of {where} names an unknown event.");
            }

            if (specification.Outcomes.Any(outcome => nodes?.Contains(outcome.Node) != true))
            {
                throw new InvalidSemanticContract($"A specification of {where} states an outcome for an unknown node.");
            }

            foreach (var expectation in specification.Then)
            {
                var valid = expectation.Kind switch
                {
                    SemanticLifecycleExpectationKind.State => states?.Contains(expectation.Target ?? string.Empty) == true,
                    SemanticLifecycleExpectationKind.Sends => _commands.ContainsKey(expectation.Command),
                    SemanticLifecycleExpectationKind.Starts => expectation.Target is not null,
                    SemanticLifecycleExpectationKind.Completes => nodes is not null,
                    _ => nodes?.Contains(expectation.Target ?? string.Empty) == true
                };
                if (!valid) throw new InvalidSemanticContract($"A specification of {where} holds an expectation that does not apply.");
            }
        }

        void ValidateWorkflowNode(SemanticApplication application, SemanticWorkflow workflow, SemanticWorkflowNode node, Dictionary<SemanticId, SemanticProperty> subject)
        {
            var where = $"node '{node.Name}' of workflow '{workflow.Name}'";
            switch (node.Kind)
            {
                case SemanticWorkflowNodeKind.Trigger:
                    switch (node.TriggerKind)
                    {
                        case SemanticWorkflowTriggerKind.Event when _events.ContainsKey(node.Event): break;
                        case SemanticWorkflowTriggerKind.StateEntered when node.StateMachine is not null && node.State is not null &&
                            application.StateMachines.FirstOrDefault(machine => machine.Name == node.StateMachine) is { } machine && machine.States.Any(state => state.Name == node.State): break;
                        case SemanticWorkflowTriggerKind.Schedule when node.Every is > 0 || node.At is not null: break;
                        case SemanticWorkflowTriggerKind.Manual: break;
                        default: throw new InvalidSemanticContract($"The trigger {where} is malformed or names something unknown.");
                    }

                    break;
                case SemanticWorkflowNodeKind.Action:
                    if (!_commands.ContainsKey(node.Command)) throw new InvalidSemanticContract($"The action {where} sends an unknown command.");
                    if (node.MaxAttempts is < 1 || node.AttemptTimeout is < 1 || node.RetryDelay is < 1) throw new InvalidSemanticContract($"The action {where} has a non-positive attempt policy.");
                    break;
                case SemanticWorkflowNodeKind.Decision:
                    if (node.Condition is null || node.WhenTrue is null || node.WhenFalse is null || node.WhenTrue == node.WhenFalse || _decisionReserved.Contains(node.WhenTrue) || _decisionReserved.Contains(node.WhenFalse))
                    {
                        throw new InvalidSemanticContract($"The decision {where} must hold a condition and two distinct author-named outlets.");
                    }

                    ValidateCondition(node.Condition, subject);
                    break;
                case SemanticWorkflowNodeKind.Wait:
                    if ((node.Duration is null) == !_events.ContainsKey(node.Event) || node.Duration is < 1 || node.ExpiresAfter is < 1 || (node.Duration is not null && (node.ExpiresAfter is not null || node.Event.IsSet)))
                    {
                        throw new InvalidSemanticContract($"The wait {where} must wait for exactly one of an event and a duration.");
                    }

                    break;
                case SemanticWorkflowNodeKind.Approval:
                    if (node.Approver is null || node.ApproverKind is null || node.ExpiresAfter is < 1) throw new InvalidSemanticContract($"The approval {where} must name who approves.");
                    break;
                case SemanticWorkflowNodeKind.ForEach:
                    if (!subject.TryGetValue(node.Items, out var items) || !items.Type.IsCollection || node.MaxParallel is < 1) throw new InvalidSemanticContract($"The foreach {where} must iterate a collection property of the subject.");
                    break;
                case SemanticWorkflowNodeKind.Join:
                    if (node.JoinMode is null || node.JoinMode == SemanticJoinMode.AtLeast != (node.JoinCount is > 0)) throw new InvalidSemanticContract($"The join {where} is malformed.");
                    break;
                default:
                    throw new InvalidSemanticContract($"The {where} has an unknown kind.");
            }
        }
    }
}
