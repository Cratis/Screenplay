// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks the <c>statemachine</c> and <c>workflow</c> declarations: their structure, their graph and every name they reference.
/// </summary>
internal sealed class LifecycleValidator
{
    static readonly Dictionary<WorkflowNodeKind, string[]> _outlets = new()
    {
        [WorkflowNodeKind.Trigger] = ["next"],
        [WorkflowNodeKind.Action] = ["next", "failed", "refused"],
        [WorkflowNodeKind.Wait] = ["resumed", "expired"],
        [WorkflowNodeKind.Approval] = ["approved", "rejected", "expired"],
        [WorkflowNodeKind.ForEach] = ["each", "completed"],
        [WorkflowNodeKind.Join] = ["next"]
    };

    static readonly HashSet<string> _reservedOutlets = [with(StringComparer.Ordinal), "next", "failed", "refused", "resumed", "expired", "approved", "rejected", "each", "completed"];

    readonly ApplicationSyntax _application;
    readonly DocumentNames _names;
    readonly ParserContext _context;
    readonly Dictionary<string, ReadModelSyntax> _readModels;

    LifecycleValidator(ApplicationSyntax application, IReadOnlyList<SliceSyntax> slices, DocumentNames names, ParserContext context)
    {
        _application = application;
        _names = names;
        _context = context;
        _readModels = slices.SelectMany(slice => slice.ReadModels ?? []).GroupBy(model => model.Name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
    }

    internal static void Validate(ApplicationSyntax application, IReadOnlyList<SliceSyntax> slices, DocumentNames names, ParserContext context)
    {
        if (!application.StateMachines.Any() && !application.Workflows.Any()) return;
        var validator = new LifecycleValidator(application, slices, names, context);
        foreach (var duplicate in application.StateMachines.GroupBy(machine => machine.Name, StringComparer.Ordinal).SelectMany(group => group.Skip(1)))
        {
            context.Error(DiagnosticCodes.InvalidStateMachineDeclaration, $"State machine '{duplicate.Name}' is declared more than once.", duplicate.Location);
        }

        foreach (var duplicate in application.Workflows.GroupBy(workflow => workflow.Name, StringComparer.Ordinal).SelectMany(group => group.Skip(1)))
        {
            context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Workflow '{duplicate.Name}' is declared more than once.", duplicate.Location);
        }

        foreach (var machine in application.StateMachines) validator.ValidateStateMachine(machine);
        foreach (var workflow in application.Workflows) validator.ValidateWorkflow(workflow);
    }

    static string Short(string name) => name[(name.LastIndexOf('.') + 1)..];

    static IEnumerable<string> PropertiesOf(ConditionSyntax condition) => condition switch
    {
        LogicalConditionSyntax logical => PropertiesOf(logical.Left).Concat(PropertiesOf(logical.Right)),
        ComparisonConditionSyntax comparison => PropertyOf(comparison.Left).Concat(comparison.Right is PathExpressionSyntax path ? PropertyOf(path.Path) : []),
        _ => []
    };

    static IEnumerable<string> PropertyOf(string text) =>
        text.Length > 0 && (char.IsLetter(text[0]) || text[0] == '_') && text.All(character => char.IsLetterOrDigit(character) || character is '_' or '.') ? [text.Split('.')[0]] : [];

    bool KnownEvent(string name) => _names.Events.Contains(name) || _names.Events.Contains(Short(name));

    void ValidateStateMachine(StateMachineSyntax machine)
    {
        var states = machine.States.Where(state => state.Name.Length > 0).ToArray();
        var stateNames = states.Select(state => state.Name).ToHashSet(StringComparer.Ordinal);
        if (machine.Follows.Length > 0 && !_names.ReadModels.Contains(machine.Follows))
        {
            _context.Error(DiagnosticCodes.UnknownStateMachineReference, $"State machine '{machine.Name}' follows read model '{machine.Follows}', which is not declared.", machine.DirectiveLocations.GetValueOrDefault("follows", machine.Location));
        }

        if (states.Count(state => state.IsInitial) != 1)
        {
            _context.Error(DiagnosticCodes.StateMachineInitialState, $"State machine '{machine.Name}' must declare exactly one initial state; it declares {states.Count(state => state.IsInitial)}.", machine.Location);
        }

        foreach (var state in states)
        {
            ValidateState(machine, state, stateNames);
        }

        if (states.Count(state => state.IsInitial) == 1)
        {
            var reached = new HashSet<string>(StringComparer.Ordinal) { states.Single(state => state.IsInitial).Name };
            var queue = new Queue<string>(reached);
            var byName = states.GroupBy(state => state.Name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            while (queue.TryDequeue(out var current))
            {
                foreach (var target in byName[current].Transitions.Select(transition => transition.Target).Where(stateNames.Contains))
                {
                    if (reached.Add(target)) queue.Enqueue(target);
                }
            }

            foreach (var state in states.Where(state => !reached.Contains(state.Name)))
            {
                _context.Warning(DiagnosticCodes.UnreachableState, $"State '{state.Name}' of '{machine.Name}' can never be reached from the initial state.", state.Location);
            }
        }
    }

    void ValidateState(StateMachineSyntax machine, StateSyntax state, HashSet<string> stateNames)
    {
        if (state.IsFinal && state.Transitions.Any())
        {
            _context.Error(DiagnosticCodes.FinalStateWithTransitions, $"Final state '{state.Name}' of '{machine.Name}' cannot have transitions out.", state.Transitions.First().Location);
        }

        if (!state.IsFinal && !state.Transitions.Any())
        {
            _context.Warning(DiagnosticCodes.StateWithNoWayOut, $"State '{state.Name}' of '{machine.Name}' is not final and has no transition out.", state.Location);
        }

        foreach (var entry in state.Entering.Where(entry => entry.EntryKind is StateEntryKind.Send or StateEntryKind.Start))
        {
            var known = entry.EntryKind == StateEntryKind.Send
                ? _names.Commands.Contains(entry.Target) || _names.Commands.Contains(Short(entry.Target))
                : _application.Workflows.Any(workflow => workflow.Name == entry.Target);
            if (!known)
            {
                _context.Error(DiagnosticCodes.UnknownStateMachineReference, $"State '{state.Name}' enters {(entry.EntryKind == StateEntryKind.Send ? "command" : "workflow")} '{entry.Target}', which is not declared.", entry.Location);
            }
        }

        var unguarded = new HashSet<string>(StringComparer.Ordinal);
        foreach (var transition in state.Transitions)
        {
            if (!KnownEvent(transition.Event))
            {
                _context.Error(DiagnosticCodes.UnknownStateMachineReference, $"Transition of state '{state.Name}' is on event '{transition.Event}', which is not declared.", transition.Location);
            }

            if (!stateNames.Contains(transition.Target))
            {
                _context.Error(DiagnosticCodes.UnknownState, $"Transition on '{transition.Event}' leads to state '{transition.Target}', which '{machine.Name}' does not declare.", transition.Location);
            }

            if (transition.When is null && !unguarded.Add(transition.Event))
            {
                _context.Error(DiagnosticCodes.AmbiguousStateTransitions, $"State '{state.Name}' has two unguarded transitions on '{transition.Event}'; the second can never be taken.", transition.Location);
            }

            if (transition.When is not null) CheckCondition(transition.When, machine.Follows, transition.Location);
        }
    }

    void CheckCondition(ConditionSyntax condition, string readModel, SourceLocation location, HashSet<string>? resultNodes = null)
    {
        if (!_readModels.TryGetValue(readModel, out var model)) return;
        var known = model.Properties.Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var property in PropertiesOf(condition).Where(property => !known.Contains(property) && !(resultNodes?.Contains(property.Split('.')[0]) ?? false)).Distinct())
        {
            _context.Error(DiagnosticCodes.UnknownConditionProperty, $"Condition reads '{property}', which read model '{readModel}' does not have.", location);
        }
    }

    void ValidateWorkflow(WorkflowSyntax workflow)
    {
        var nodes = workflow.Nodes.GroupBy(node => node.Name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        if (workflow.Subject.Length > 0 && !_names.ReadModels.Contains(workflow.Subject))
        {
            _context.Error(DiagnosticCodes.UnknownWorkflowReference, $"Workflow '{workflow.Name}' is for read model '{workflow.Subject}', which is not declared.", workflow.DirectiveLocations.GetValueOrDefault("for", workflow.Location));
        }

        if (!workflow.Nodes.Any(node => node.NodeKind == WorkflowNodeKind.Trigger))
        {
            _context.Error(DiagnosticCodes.WorkflowWithoutTrigger, $"Workflow '{workflow.Name}' has no trigger, so nothing starts a run.", workflow.Location);
        }

        var incoming = nodes.Keys.ToDictionary(name => name, _ => 0, StringComparer.Ordinal);
        foreach (var node in workflow.Nodes)
        {
            ValidateNode(workflow, node, nodes, incoming);
        }

        var reached = new HashSet<string>(workflow.Nodes.Where(node => node.NodeKind == WorkflowNodeKind.Trigger).Select(node => node.Name), StringComparer.Ordinal);
        var queue = new Queue<string>(reached);
        while (queue.TryDequeue(out var current))
        {
            foreach (var target in nodes[current].Edges.Select(edge => edge.Target).Where(nodes.ContainsKey))
            {
                if (reached.Add(target)) queue.Enqueue(target);
            }
        }

        foreach (var node in workflow.Nodes.Where(node => !reached.Contains(node.Name)))
        {
            _context.Warning(DiagnosticCodes.UnreachableWorkflowNode, $"Node '{node.Name}' of workflow '{workflow.Name}' can never be reached from a trigger.", node.Location);
        }

        foreach (var join in workflow.Nodes.Where(node => node.NodeKind == WorkflowNodeKind.Join && incoming.GetValueOrDefault(node.Name) == 1))
        {
            _context.Add(new(DiagnosticSeverity.Information, DiagnosticCodes.WorkflowJoinWithOneBranch, $"Join '{join.Name}' has a single incoming branch, so it joins nothing.", join.Location));
        }
    }

    void ValidateNode(WorkflowSyntax workflow, WorkflowNodeSyntax node, Dictionary<string, WorkflowNodeSyntax> nodes, Dictionary<string, int> incoming)
    {
        ValidateReferences(workflow, node);
        var seenOutlets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var edge in node.Edges)
        {
            if (!seenOutlets.Add(edge.Outlet))
            {
                _context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Node '{node.Name}' leaves by outlet '{edge.Outlet}' more than once.", edge.Location);
            }

            if (node.NodeKind == WorkflowNodeKind.Decide)
            {
                if (_reservedOutlets.Contains(edge.Outlet)) _context.Error(DiagnosticCodes.UnknownWorkflowOutlet, $"Outlet '{edge.Outlet}' is reserved for other node kinds; a decide node names its own outlets.", edge.Location);
                if (edge.Target.Length == 0) _context.Error(DiagnosticCodes.WorkflowDecisionOutletWithoutTarget, $"Outlet '{edge.Outlet}' of decide node '{node.Name}' leads nowhere.", edge.Location);
            }
            else if (node.NodeKind == WorkflowNodeKind.Compute || (node.NodeKind == WorkflowNodeKind.Action && node.Command?.Contains('.', StringComparison.Ordinal) == true))
            {
                // Ability outlets and compute outlets are checked by the InvocationValidator against the declared outcomes.
                if (edge.Target.Length == 0) _context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Outlet '{edge.Outlet}' of node '{node.Name}' needs a target: '{edge.Outlet} -> <Node>'.", edge.Location);
            }
            else if (!_outlets[node.NodeKind].Contains(edge.Outlet))
            {
                _context.Error(DiagnosticCodes.UnknownWorkflowOutlet, $"A {node.NodeKind.ToString().ToLowerInvariant()} node has no outlet '{edge.Outlet}'; its outlets are {string.Join(", ", _outlets[node.NodeKind])}.", edge.Location);
            }
            else if (edge.Target.Length == 0)
            {
                _context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Outlet '{edge.Outlet}' of node '{node.Name}' needs a target: '{edge.Outlet} -> <Node>'.", edge.Location);
            }

            if (edge.Target.Length == 0) continue;
            if (!nodes.TryGetValue(edge.Target, out var target))
            {
                _context.Error(DiagnosticCodes.UnknownWorkflowNode, $"Outlet '{edge.Outlet}' of node '{node.Name}' leads to '{edge.Target}', which workflow '{workflow.Name}' does not declare.", edge.Location);
                continue;
            }

            incoming[edge.Target]++;
            if (target.NodeKind == WorkflowNodeKind.Trigger)
            {
                _context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Trigger '{target.Name}' starts a run; no edge can lead to it.", edge.Location);
            }
        }

        if (node.NodeKind == WorkflowNodeKind.Decide && node.Edges.Count() != 2)
        {
            _context.Error(DiagnosticCodes.WorkflowDecisionOutletWithoutTarget, $"Decide node '{node.Name}' needs exactly two outlets: the first is taken when the condition holds, the second when it does not.", node.Location);
        }

        if ((node.NodeKind == WorkflowNodeKind.Approval || (node.NodeKind == WorkflowNodeKind.Wait && node.Duration is null)) && node.ExpiresAfter is null)
        {
            _context.Warning(DiagnosticCodes.WorkflowWithoutExpiry, $"{(node.NodeKind == WorkflowNodeKind.Approval ? "Approval" : "Wait")} '{node.Name}' has no 'expires after', so a run can wait forever.", node.Location);
        }
    }

    void ValidateReferences(WorkflowSyntax workflow, WorkflowNodeSyntax node)
    {
        void Unknown(string kind, string name) => _context.Error(DiagnosticCodes.UnknownWorkflowReference, $"Node '{node.Name}' of workflow '{workflow.Name}' names {kind} '{name}', which is not declared.", node.Location);
        if (node.Event is { } @event && !KnownEvent(@event)) Unknown("event", @event);
        if (node.Command is { } command && !command.Contains('.', StringComparison.Ordinal) && !_names.Commands.Contains(command) && !_names.Commands.Contains(Short(command))) Unknown("command", command);
        if (node.StateMachine is { } machineName)
        {
            var machine = _application.StateMachines.FirstOrDefault(candidate => candidate.Name == machineName);
            if (machine is null)
            {
                Unknown("state machine", machineName);
            }
            else if (node.State is { } state && machine.States.All(candidate => candidate.Name != state))
            {
                _context.Error(DiagnosticCodes.UnknownState, $"Trigger '{node.Name}' waits for state '{state}', which state machine '{machineName}' does not declare.", node.Location);
            }
            else if (machine.Follows.Length > 0 && workflow.Subject.Length > 0 && machine.Follows != workflow.Subject)
            {
                _context.Error(DiagnosticCodes.UnknownWorkflowReference, $"Trigger '{node.Name}' uses state machine '{machineName}', which follows '{machine.Follows}', but workflow '{workflow.Name}' is for '{workflow.Subject}'.", node.Location);
            }
        }

        if (node.By is { } by && !(_application.Personas ?? []).Any(persona => persona.Name == by) && !_names.Policies.Contains(by)) Unknown("persona or policy", by);
        if (node.Condition is { } condition) CheckConditionReferences(condition, workflow, node);
        if (node.Items is { } items && _readModels.TryGetValue(workflow.Subject, out var model))
        {
            var property = model.Properties.FirstOrDefault(candidate => candidate.Name == items);
            if (property is null) _context.Error(DiagnosticCodes.UnknownConditionProperty, $"Foreach '{node.Name}' iterates '{items}', which read model '{workflow.Subject}' does not have.", node.Location);
            else if (!property.Type.IsCollection) _context.Error(DiagnosticCodes.UnknownWorkflowReference, $"Foreach '{node.Name}' iterates '{items}', which is not a collection.", node.Location);
        }
    }

    void CheckConditionReferences(ConditionSyntax condition, WorkflowSyntax workflow, WorkflowNodeSyntax node) =>
        CheckCondition(
            condition,
            workflow.Subject,
            node.Location,
            workflow.Nodes
                .Where(candidate => candidate.NodeKind == WorkflowNodeKind.Compute || (candidate.NodeKind == WorkflowNodeKind.Action && candidate.Command?.Contains('.', StringComparison.Ordinal) == true))
                .Select(candidate => candidate.Name)
                .ToHashSet(StringComparer.Ordinal));
}
