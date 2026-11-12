// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>statemachine</c> declarations: the read model they follow, their states, what entering a state does and the transitions out of it.
/// </summary>
internal static partial class StateMachineParser
{
    internal static StateMachineSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidStateMachineDeclaration, $"Invalid state machine declaration '{header.Content}' - expected 'statemachine <Name>'.", header.Location);
            context.SkipBlock(header.Indent);
            return new(LineText.FirstWord(header.Content), string.Empty, [], header.Location);
        }

        var name = match.Groups[1].Value;
        string? description = null;
        string? follows = null;
        var states = new List<StateSyntax>();
        var specifications = new List<LifecycleSpecificationSyntax>();
        var locations = new Dictionary<string, SourceLocation>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "description":
                    description = DescriptionParser.Parse(context, line, description, $"State machine '{name}'", out _);
                    locations.TryAdd("description", line.Location);
                    break;
                case "follows":
                    var followed = FollowsRegex().Match(line.Content);
                    if (!followed.Success)
                    {
                        context.Error(DiagnosticCodes.InvalidStateMachineDeclaration, $"Invalid follows '{line.Content}' - expected 'follows <ReadModel>'.", line.Location);
                    }
                    else if (follows is not null)
                    {
                        context.Error(DiagnosticCodes.InvalidStateMachineDeclaration, $"State machine '{name}' follows more than one read model.", line.Location);
                    }
                    else
                    {
                        follows = followed.Groups[1].Value;
                        locations["follows"] = line.Location;
                    }

                    OperationParser.RejectChildren(context, line, DiagnosticCodes.InvalidStateMachineDeclaration, "A follows line cannot have children.");
                    break;
                case "state":
                    states.Add(ParseState(context, line, name));
                    break;
                case "specification":
                    specifications.Add(SpecificationParser.ParseLifecycle(context, line, DiagnosticCodes.InvalidStateMachineDeclaration));
                    break;
                default:
                    context.Error(DiagnosticCodes.InvalidStateMachineDeclaration, $"Unexpected '{line.Content}' in state machine '{name}' - expected description, follows, state or specification.", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        if (follows is null) context.Error(DiagnosticCodes.InvalidStateMachineDeclaration, $"State machine '{name}' must follow a read model: 'follows <ReadModel>'.", header.Location);
        foreach (var duplicate in states.GroupBy(state => state.Name, StringComparer.Ordinal).SelectMany(group => group.Skip(1)))
        {
            context.Error(DiagnosticCodes.InvalidStateMachineDeclaration, $"State machine '{name}' declares state '{duplicate.Name}' more than once.", duplicate.Location);
        }

        return new(name, follows ?? string.Empty, states, header.Location) { Description = description, Specifications = specifications, DirectiveLocations = locations };
    }

    static StateSyntax ParseState(ParserContext context, SourceLine header, string machine)
    {
        var match = StateRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidStateMachineDeclaration, $"Invalid state '{header.Content}' - expected 'state <Name> [initial] [final]'.", header.Location);
            context.SkipBlock(header.Indent);
            return new(string.Empty, false, false, [], [], header.Location);
        }

        var modifiers = match.Groups[2].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var isInitial = modifiers.Contains("initial");
        var isFinal = modifiers.Contains("final");
        if (modifiers.Length != modifiers.Distinct().Count()) context.Error(DiagnosticCodes.InvalidStateMachineDeclaration, $"State '{match.Groups[1].Value}' repeats a modifier.", header.Location);
        var entering = new List<StateEntrySyntax>();
        var transitions = new List<StateTransitionSyntax>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "entering" when line.Content == "entering":
                    entering.Add(ParseEnteringBody(context, line));
                    break;
                case "entering" when AbilityEnteringRegex().IsMatch(line.Content):
                    if (ReactionParser.ParseInvokes(context, line with { Content = line.Content["entering ".Length..] }, allowBranches: false) is { } invoked)
                    {
                        entering.Add(new(StateEntryKind.Invoke, invoked.Command, line.Location) { Mappings = invoked.Mappings });
                    }

                    break;
                case "entering":
                    var entry = EnteringRegex().Match(line.Content);
                    if (!entry.Success)
                    {
                        context.Error(DiagnosticCodes.InvalidStateMachineDeclaration, $"Invalid entering '{line.Content}' - expected 'entering invokes <Command>' or 'entering starts <Workflow>'.", line.Location);
                    }
                    else
                    {
                        var verb = entry.Groups[1].Value;
                        var legacy = string.Equals(verb, "send", StringComparison.Ordinal) || string.Equals(verb, "start", StringComparison.Ordinal);
                        var sends = string.Equals(verb, "send", StringComparison.Ordinal) || string.Equals(verb, "invokes", StringComparison.Ordinal);
                        if (legacy)
                        {
                            context.Warning(DiagnosticCodes.LegacyInlineCodeFence, $"'entering {verb}' is deprecated - use 'entering {(sends ? "invokes" : "starts")}'.", line.Location);
                        }

                        entering.Add(new(sends ? StateEntryKind.Send : StateEntryKind.Start, entry.Groups[2].Value, line.Location));
                    }

                    OperationParser.RejectChildren(context, line, DiagnosticCodes.InvalidStateMachineDeclaration, "An entering line cannot have children.");
                    break;
                case "on":
                    var transition = TransitionRegex().Match(line.Content);
                    if (!transition.Success)
                    {
                        context.Error(DiagnosticCodes.InvalidStateMachineDeclaration, $"Invalid transition '{line.Content}' - expected 'on <Event> [when <condition>] -> <State>'.", line.Location);
                    }
                    else
                    {
                        ConditionSyntax? when = null;
                        var failed = false;
                        if (transition.Groups[2].Success)
                        {
                            when = ConditionParser.Parse(context, transition.Groups[2].Value, line.Location);
                            failed = when is null;
                        }

                        if (!failed) transitions.Add(new(transition.Groups[1].Value, when, transition.Groups[3].Value, line.Location));
                    }

                    OperationParser.RejectChildren(context, line, DiagnosticCodes.InvalidStateMachineDeclaration, "A transition cannot have children.");
                    break;
                default:
                    context.Error(DiagnosticCodes.InvalidStateMachineDeclaration, $"Unexpected '{line.Content}' in state '{match.Groups[1].Value}' of '{machine}' - expected entering or on.", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(match.Groups[1].Value, isInitial, isFinal, entering, transitions, header.Location);
    }

    static StateEntrySyntax ParseEnteringBody(ParserContext context, SourceLine header)
    {
        FileReferenceSyntax? file = null;
        CodeBlockSyntax? code = null;
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (file is not null || code is not null)
            {
                context.Error(DiagnosticCodes.ConflictingImplementationSources, "An entering body has at most one file or inline payload.", line.Location);
                context.SkipBlock(line.Indent);
            }
            else if (FileReferenceParser.IsDirective(line))
            {
                file = FileReferenceParser.Parse(context, line);
                OperationParser.RejectChildren(context, line, DiagnosticCodes.InvalidImplementationBlock, "A file directive cannot have children.");
            }
            else if (line.Content.StartsWith("```", StringComparison.Ordinal))
            {
                code = CodeBlockParser.Parse(context, line);
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidStateMachineDeclaration, "An entering body accepts a file or a tagged fence.", line.Location);
                context.SkipBlock(line.Indent);
            }
        }

        if (file is null && code is null)
        {
            context.Error(DiagnosticCodes.InvalidStateMachineDeclaration, "An 'entering' line with no target needs a body: a file or a tagged fence.", header.Location);
        }

        return new(StateEntryKind.Body, string.Empty, header.Location) { File = file, Code = code };
    }

    [GeneratedRegex(@"^statemachine\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^follows\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex FollowsRegex();

    [GeneratedRegex(@"^state\s+([A-Za-z_]\w*)((?:\s+(?:initial|final))*)$", RegexOptions.NonBacktracking)]
    private static partial Regex StateRegex();

    [GeneratedRegex(@"^entering\s+invokes\s+[A-Za-z_]\w*\.[A-Za-z_]\w*$", RegexOptions.NonBacktracking)]
    private static partial Regex AbilityEnteringRegex();

    [GeneratedRegex(@"^entering\s+(invokes|starts|send|start)\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex EnteringRegex();

    [GeneratedRegex(@"^on\s+([A-Za-z_][\w.]*)(?:\s+when\s+(.+?))?\s*->\s*([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex TransitionRegex();
}
