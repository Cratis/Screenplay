// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>workflow</c> declarations: the subject read model and a graph of named nodes with their outlets.
/// </summary>
/// <remarks>
/// The node words are contextual. They are recognized only on the lines directly inside a <c>workflow</c>, never at
/// document level, so <c>trigger</c> (a top-level construct) and <c>join</c> (a projection keyword) keep their meaning elsewhere.
/// </remarks>
internal static partial class WorkflowParser
{
    internal static WorkflowSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Invalid workflow declaration '{header.Content}' - expected 'workflow <Name>'.", header.Location);
            context.SkipBlock(header.Indent);
            return new(LineText.FirstWord(header.Content), string.Empty, [], header.Location);
        }

        var name = match.Groups[1].Value;
        string? description = null;
        string? subject = null;
        var nodes = new List<WorkflowNodeSyntax>();
        var specifications = new List<LifecycleSpecificationSyntax>();
        var locations = new Dictionary<string, SourceLocation>();
        var triggers = 0;
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var keyword = LineText.FirstWord(line.Content);
            if (keyword == "description")
            {
                description = DescriptionParser.Parse(context, line, description, $"Workflow '{name}'", out _);
                locations.TryAdd("description", line.Location);
            }
            else if (keyword == "specification")
            {
                specifications.Add(SpecificationParser.ParseLifecycle(context, line, DiagnosticCodes.InvalidWorkflowDeclaration));
            }
            else if (keyword == "for")
            {
                var subjectMatch = ForRegex().Match(line.Content);
                if (!subjectMatch.Success)
                {
                    context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Invalid subject '{line.Content}' - expected 'for <ReadModel>'.", line.Location);
                }
                else if (subject is not null)
                {
                    context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Workflow '{name}' declares its subject more than once.", line.Location);
                }
                else
                {
                    subject = subjectMatch.Groups[1].Value;
                    locations["for"] = line.Location;
                }

                OperationParser.RejectChildren(context, line, DiagnosticCodes.InvalidWorkflowDeclaration, "A for line cannot have children.");
            }
            else if (ParseNode(context, line, name, ref triggers) is { } node)
            {
                nodes.Add(node);
            }
        }

        if (subject is null) context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Workflow '{name}' must name its subject: 'for <ReadModel>'.", header.Location);
        foreach (var duplicate in nodes.GroupBy(node => node.Name, StringComparer.Ordinal).SelectMany(group => group.Skip(1)))
        {
            context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Workflow '{name}' declares node '{duplicate.Name}' more than once.", duplicate.Location);
        }

        return new(name, subject ?? string.Empty, nodes, header.Location) { Description = description, Specifications = specifications, DirectiveLocations = locations };
    }

    static WorkflowNodeSyntax? ParseNode(ParserContext context, SourceLine line, string workflow, ref int triggers)
    {
        var keyword = LineText.FirstWord(line.Content);
        var node = keyword switch
        {
            "trigger" => ParseTrigger(context, line, ++triggers),
            "action" => ParseAction(context, line),
            "decide" => ParseDecide(context, line),
            "wait" => ParseWait(context, line),
            "approval" => ParseApproval(context, line),
            "foreach" => ParseForEach(context, line),
            "join" => ParseJoin(context, line),
            _ => Unexpected(context, line, workflow)
        };
        if (node is null)
        {
            context.SkipBlock(line.Indent);
            return null;
        }

        return node with { Edges = ParseEdges(context, line) };
    }

    static WorkflowNodeSyntax? Unexpected(ParserContext context, SourceLine line, string workflow)
    {
        context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Unexpected '{line.Content}' in workflow '{workflow}' - expected for, description, specification or a node: trigger, action, decide, wait, approval, foreach or join.", line.Location);
        return null;
    }

    static List<WorkflowEdgeSyntax> ParseEdges(ParserContext context, SourceLine header)
    {
        var edges = new List<WorkflowEdgeSyntax>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var shorthand = ShorthandEdgeRegex().Match(line.Content);
            var edge = EdgeRegex().Match(line.Content);
            if (shorthand.Success)
            {
                edges.Add(new("next", shorthand.Groups[1].Value, line.Location));
            }
            else if (edge.Success)
            {
                edges.Add(new(edge.Groups[1].Success ? edge.Groups[1].Value : edge.Groups[2].Value, edge.Groups[3].Success ? edge.Groups[3].Value : string.Empty, line.Location));
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Invalid edge '{line.Content}' - expected '<outlet> -> <Node>'.", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            OperationParser.RejectChildren(context, line, DiagnosticCodes.InvalidWorkflowDeclaration, "An edge cannot have children.");
        }

        return edges;
    }

    static WorkflowNodeSyntax? ParseTrigger(ParserContext context, SourceLine line, int ordinal)
    {
        var match = TriggerRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Invalid trigger '{line.Content}' - expected 'trigger [<Name>] on <Event>', 'when <StateMachine> enters <State>', 'every <n> <unit>', 'at <HH:mm>' or 'manual'.", line.Location);
            return null;
        }

        var fallbackName = ordinal == 1 ? "Trigger" : $"Trigger{ordinal.ToString(CultureInfo.InvariantCulture)}";
        var name = match.Groups[1].Success ? match.Groups[1].Value : fallbackName;
        var rest = match.Groups[2].Value;
        var location = line.Location;
        if (rest == "manual") return new(WorkflowNodeKind.Trigger, name, [], location) { TriggerKind = WorkflowTriggerKind.Manual };
        if (OnEventRegex().Match(rest) is { Success: true } onEvent) return new(WorkflowNodeKind.Trigger, name, [], location) { TriggerKind = WorkflowTriggerKind.Event, Event = onEvent.Groups[1].Value };
        if (EntersRegex().Match(rest) is { Success: true } enters) return new(WorkflowNodeKind.Trigger, name, [], location) { TriggerKind = WorkflowTriggerKind.StateEntered, StateMachine = enters.Groups[1].Value, State = enters.Groups[2].Value };
        if ((rest.StartsWith("every ", StringComparison.Ordinal) || rest.StartsWith("at ", StringComparison.Ordinal)) && TriggerParser.ParseSource(context, line with { Content = rest }) is { } schedule)
        {
            return new(WorkflowNodeKind.Trigger, name, [], location) { TriggerKind = WorkflowTriggerKind.Schedule, Schedule = schedule };
        }

        if (!rest.StartsWith("every ", StringComparison.Ordinal) && !rest.StartsWith("at ", StringComparison.Ordinal))
        {
            context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Invalid trigger '{line.Content}' - expected 'on <Event>', 'when <StateMachine> enters <State>', 'every <n> <unit>', 'at <HH:mm>' or 'manual'.", line.Location);
        }

        return null;
    }

    static WorkflowNodeSyntax? ParseAction(ParserContext context, SourceLine line)
    {
        var match = ActionRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Invalid action '{line.Content}' - expected 'action <Name> sends <Command> [attempts <n>] [timeout <n> <unit>] [retry after <n> <unit>]'.", line.Location);
            return null;
        }

        int? attempts = null;
        WorkflowDurationSyntax? timeout = null;
        WorkflowDurationSyntax? retry = null;
        var hasTimeout = false;
        var hasRetry = false;
        foreach (Match option in ActionOptionRegex().Matches(match.Groups[3].Value))
        {
            if (option.Groups[1].Success)
            {
                if (attempts is not null || !int.TryParse(option.Groups[1].Value, CultureInfo.InvariantCulture, out var count) || count < 1) context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Invalid or repeated attempts in '{line.Content}'.", line.Location);
                else attempts = count;
            }
            else if (option.Groups[2].Success)
            {
                if (hasTimeout || Duration(context, option.Groups[2].Value, option.Groups[3].Value, line) is not { } parsed)
                {
                    context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Invalid or repeated timeout in '{line.Content}'.", line.Location);
                }
                else
                {
                    timeout = parsed;
                    hasTimeout = true;
                }
            }
            else if (hasRetry || Duration(context, option.Groups[4].Value, option.Groups[5].Value, line) is not { } parsedRetry)
            {
                context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Invalid or repeated retry in '{line.Content}'.", line.Location);
            }
            else
            {
                retry = parsedRetry;
                hasRetry = true;
            }
        }

        return new(WorkflowNodeKind.Action, match.Groups[1].Value, [], line.Location) { Command = match.Groups[2].Value, Attempts = attempts, Timeout = timeout, RetryAfter = retry };
    }

    static WorkflowNodeSyntax? ParseDecide(ParserContext context, SourceLine line)
    {
        var match = DecideRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Invalid decide '{line.Content}' - expected 'decide <Name> when <condition>'.", line.Location);
            return null;
        }

        var condition = ConditionParser.Parse(context, match.Groups[2].Value, line.Location);
        return condition is null ? null : new(WorkflowNodeKind.Decide, match.Groups[1].Value, [], line.Location) { Condition = condition };
    }

    static WorkflowNodeSyntax? ParseWait(ParserContext context, SourceLine line)
    {
        var match = WaitRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Invalid wait '{line.Content}' - expected 'wait <Name> for <Event> [expires after <n> <unit>]' or 'wait <Name> for <n> <unit>'.", line.Location);
            return null;
        }

        var name = match.Groups[1].Value;
        if (match.Groups[2].Success)
        {
            return Duration(context, match.Groups[2].Value, match.Groups[3].Value, line) is { } duration
                ? new(WorkflowNodeKind.Wait, name, [], line.Location) { Duration = duration }
                : null;
        }

        WorkflowDurationSyntax? expires = null;
        if (match.Groups[5].Success && (expires = Duration(context, match.Groups[5].Value, match.Groups[6].Value, line)) is null) return null;
        return new(WorkflowNodeKind.Wait, name, [], line.Location) { Event = match.Groups[4].Value, ExpiresAfter = expires };
    }

    static WorkflowNodeSyntax? ParseApproval(ParserContext context, SourceLine line)
    {
        var match = ApprovalRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Invalid approval '{line.Content}' - expected 'approval <Name> by <Persona or Policy> [expires after <n> <unit>]'.", line.Location);
            return null;
        }

        WorkflowDurationSyntax? expires = null;
        if (match.Groups[3].Success && (expires = Duration(context, match.Groups[3].Value, match.Groups[4].Value, line)) is null) return null;
        return new(WorkflowNodeKind.Approval, match.Groups[1].Value, [], line.Location) { By = match.Groups[2].Value, ExpiresAfter = expires };
    }

    static WorkflowNodeSyntax? ParseForEach(ParserContext context, SourceLine line)
    {
        var match = ForEachRegex().Match(line.Content);
        if (!match.Success || (match.Groups[3].Success && (!int.TryParse(match.Groups[3].Value, CultureInfo.InvariantCulture, out var bound) || bound < 1)))
        {
            context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Invalid foreach '{line.Content}' - expected 'foreach <Name> over <property> [parallel <n>]'.", line.Location);
            return null;
        }

        return new(WorkflowNodeKind.ForEach, match.Groups[1].Value, [], line.Location)
        {
            Items = match.Groups[2].Value,
            Parallel = match.Groups[3].Success ? int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) : null
        };
    }

    static WorkflowNodeSyntax? ParseJoin(ParserContext context, SourceLine line)
    {
        var match = JoinRegex().Match(line.Content);
        if (!match.Success || (match.Groups[3].Success && (!int.TryParse(match.Groups[3].Value, CultureInfo.InvariantCulture, out var count) || count < 1)))
        {
            context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Invalid join '{line.Content}' - expected 'join <Name> all', 'join <Name> any' or 'join <Name> at least <n>'.", line.Location);
            return null;
        }

        var mode = match.Groups[2].Value switch { "all" => WorkflowJoinMode.All, "any" => WorkflowJoinMode.Any, _ => WorkflowJoinMode.AtLeast };
        return new(WorkflowNodeKind.Join, match.Groups[1].Value, [], line.Location)
        {
            JoinMode = mode,
            JoinCount = match.Groups[3].Success ? int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) : null
        };
    }

    static WorkflowDurationSyntax? Duration(ParserContext context, string amount, string unit, SourceLine line)
    {
        if (!int.TryParse(amount, CultureInfo.InvariantCulture, out var count) || count < 1)
        {
            context.Error(DiagnosticCodes.InvalidWorkflowDeclaration, $"Invalid duration '{amount} {unit}' in '{line.Content}' - an amount is at least 1.", line.Location);
            return null;
        }

        var parsed = unit.TrimEnd('s') switch
        {
            "second" => IntervalUnit.Seconds,
            "minute" => IntervalUnit.Minutes,
            "hour" => IntervalUnit.Hours,
            _ => IntervalUnit.Days
        };
        return new(count, parsed, line.Location);
    }

    [GeneratedRegex(@"^workflow\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^for\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex ForRegex();

    [GeneratedRegex(@"^->\s*([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex ShorthandEdgeRegex();

    [GeneratedRegex("^(?:\"([^\"]+)\"|([A-Za-z_]\\w*))(?:\\s*->\\s*([A-Za-z_]\\w*))?$", RegexOptions.NonBacktracking)]
    private static partial Regex EdgeRegex();

    [GeneratedRegex(@"^trigger(?:\s+(?!(?:on|when|every|at|manual)\b)([A-Za-z_]\w*))?\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex TriggerRegex();

    [GeneratedRegex(@"^on\s+([A-Za-z_][\w.]*)$", RegexOptions.NonBacktracking)]
    private static partial Regex OnEventRegex();

    [GeneratedRegex(@"^when\s+([A-Za-z_]\w*)\s+enters\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex EntersRegex();

    [GeneratedRegex(@"^action\s+([A-Za-z_]\w*)\s+sends\s+([A-Za-z_]\w*)((?:\s+(?:attempts\s+\d+|timeout\s+\d+\s+\w+|retry\s+after\s+\d+\s+\w+))*)$", RegexOptions.NonBacktracking)]
    private static partial Regex ActionRegex();

    [GeneratedRegex(@"attempts\s+(\d+)|timeout\s+(\d+)\s+(\w+)|retry\s+after\s+(\d+)\s+(\w+)", RegexOptions.NonBacktracking)]
    private static partial Regex ActionOptionRegex();

    [GeneratedRegex(@"^decide\s+([A-Za-z_]\w*)\s+when\s+(.+)$", RegexOptions.NonBacktracking)]
    private static partial Regex DecideRegex();

    [GeneratedRegex(@"^wait\s+([A-Za-z_]\w*)\s+for\s+(?:(\d+)\s+(seconds?|minutes?|hours?|days?)|([A-Za-z_][\w.]*)(?:\s+expires\s+after\s+(\d+)\s+(seconds?|minutes?|hours?|days?))?)$", RegexOptions.NonBacktracking)]
    private static partial Regex WaitRegex();

    [GeneratedRegex(@"^approval\s+([A-Za-z_]\w*)\s+by\s+([A-Za-z_]\w*)(?:\s+expires\s+after\s+(\d+)\s+(seconds?|minutes?|hours?|days?))?$", RegexOptions.NonBacktracking)]
    private static partial Regex ApprovalRegex();

    [GeneratedRegex(@"^foreach\s+([A-Za-z_]\w*)\s+over\s+([A-Za-z_][\w.]*)(?:\s+parallel\s+(\d+))?$", RegexOptions.NonBacktracking)]
    private static partial Regex ForEachRegex();

    [GeneratedRegex(@"^join\s+([A-Za-z_]\w*)\s+(all|any|at\s+least\s+(\d+))$", RegexOptions.NonBacktracking)]
    private static partial Regex JoinRegex();
}
