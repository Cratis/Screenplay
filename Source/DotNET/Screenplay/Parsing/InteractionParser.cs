// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the interaction constructs - <c>behavior</c> declarations, the <c>on</c> bindings inside them, the
/// actions those bindings run, and the <c>uses</c> clause attaching a named behavior to something.
/// </summary>
/// <remarks>
/// An inline <c>on</c> block and a named <c>behavior</c> are the same construct: the inline form produces a
/// <see cref="BehaviorSyntax"/> with no name holding the one binding that was written. Everything downstream -
/// walker, printer, semantics - therefore sees one shape.
/// </remarks>
internal static partial class InteractionParser
{
    /// <summary>
    /// The deepest continuation nesting admitted. Continuations nest arbitrarily in principle
    /// (<c>execute -> on success -> confirm -> on success -> ...</c>), so the parser caps the depth and reports
    /// it rather than recursing until the stack gives out.
    /// </summary>
    public const int MaximumActionDepth = 16;

    /// <summary>
    /// The shortest interval a client is asked to honour, in seconds.
    /// </summary>
    public const int MinimumIntervalSeconds = 5;

    /// <summary>
    /// The built-in interaction kinds, by the surface text that names them. Reserved inside an <c>on</c> clause
    /// only - these words carry no special meaning anywhere else in the language.
    /// </summary>
    static readonly Dictionary<string, InteractionTriggerKind> _builtInTriggers = new(StringComparer.Ordinal)
    {
        ["click"] = InteractionTriggerKind.Click,
        ["double click"] = InteractionTriggerKind.DoubleClick,
        ["select"] = InteractionTriggerKind.Select,
        ["submit"] = InteractionTriggerKind.Submit,
        ["change"] = InteractionTriggerKind.Change,
        ["load"] = InteractionTriggerKind.Load,
        ["unload"] = InteractionTriggerKind.Unload,
        ["enter"] = InteractionTriggerKind.Enter,
        ["leave"] = InteractionTriggerKind.Leave
    };

    /// <summary>
    /// Gets the surface text of every built-in interaction kind.
    /// </summary>
    public static IEnumerable<string> BuiltInTriggerNames => _builtInTriggers.Keys;

    /// <summary>
    /// Whether the text names a built-in interaction kind.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True when it does.</returns>
    public static bool IsBuiltInTriggerName(string text) => _builtInTriggers.ContainsKey(text);

    /// <summary>
    /// Whether the line opens an interaction directive - an <c>on</c> binding or a <c>uses</c> clause.
    /// </summary>
    /// <param name="line">The <see cref="SourceLine"/> to check.</param>
    /// <returns>True when it does.</returns>
    public static bool IsInteractionDirective(SourceLine line)
    {
        var first = LineText.FirstWord(line.Content);
        return string.Equals(first, "on", StringComparison.Ordinal) || string.Equals(first, "uses", StringComparison.Ordinal);
    }

    /// <summary>
    /// Parses an interaction directive - an inline <c>on</c> binding or a <c>uses</c> attachment - into
    /// whichever collection it belongs in.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the directive.</param>
    /// <param name="behaviors">The inline behaviors attached so far.</param>
    /// <param name="usedBehaviors">The named behaviors attached so far.</param>
    /// <remarks>
    /// Every attachment site - layout, template, module, feature, form, screen, element - runs this, so a
    /// behavior means the same thing and reports the same diagnostics wherever it is written.
    /// </remarks>
    public static void ParseAttachment(
        ParserContext context,
        SourceLine line,
        List<BehaviorSyntax> behaviors,
        List<UsesBehaviorSyntax> usedBehaviors)
    {
        if (string.Equals(LineText.FirstWord(line.Content), "uses", StringComparison.Ordinal))
        {
            if (ParseUses(context, line) is { } uses)
            {
                usedBehaviors.Add(uses);
            }

            return;
        }

        if (ParseInlineBehavior(context, line) is { } behavior)
        {
            behaviors.Add(behavior);
        }
    }

    /// <summary>
    /// Parses a named behavior from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>behavior</c> header.</param>
    /// <returns>The parsed <see cref="BehaviorSyntax"/>.</returns>
    public static BehaviorSyntax ParseBehavior(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidBehaviorDeclaration, $"Invalid behavior declaration '{header.Content}' - expected 'behavior <Name>'", header.Location);
        }

        var name = match.Success ? match.Groups[1].Value : string.Empty;
        var parameters = new List<BehaviorParameterSyntax>();
        var bindings = new List<InteractionBindingSyntax>();
        string? description = null;
        int? order = null;
        FileReferenceSyntax? file = null;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();

            if (FileReferenceParser.IsDirective(line))
            {
                file = FileReferenceParser.Parse(context, line);
                continue;
            }

            switch (LineText.FirstWord(line.Content))
            {
                case "description":
                    description = DescriptionParser.Parse(context, line, description, $"behavior '{name}'");
                    break;
                case "parameter":
                    ParseParameter(context, line, parameters);
                    break;
                case "order":
                    order = ParseOrder(context, line) ?? order;
                    break;
                case "on":
                    if (ParseBinding(context, line) is { } binding)
                    {
                        bindings.Add(binding);
                    }

                    break;
                default:
                    context.Error(
                        DiagnosticCodes.UnknownBehaviorDirective,
                        $"Unexpected '{line.Content}' in behavior body - expected description, parameter, order or 'on <trigger>'",
                        line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        if (bindings.Count == 0)
        {
            context.Warning(
                DiagnosticCodes.BehaviorWithoutBindings,
                $"Behavior '{name}' declares no bindings - nothing it is attached to will ever run anything",
                header.Location);
        }

        return new(name, parameters, bindings, header.Location, description, order) { File = file };
    }

    /// <summary>
    /// Parses an inline <c>on</c> block into the anonymous behavior it is sugar for.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the <c>on</c> clause.</param>
    /// <returns>The parsed <see cref="BehaviorSyntax"/>, or <c>null</c> when the clause was not usable.</returns>
    public static BehaviorSyntax? ParseInlineBehavior(ParserContext context, SourceLine line)
    {
        var binding = ParseBinding(context, line);
        return binding is null ? null : new(null, [], [binding], line.Location);
    }

    /// <summary>
    /// Parses a <c>uses</c> clause from its already consumed line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the clause.</param>
    /// <returns>The parsed <see cref="UsesBehaviorSyntax"/>, or <c>null</c> when it was not usable.</returns>
    public static UsesBehaviorSyntax? ParseUses(ParserContext context, SourceLine line)
    {
        var match = UsesRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidUsesDeclaration, $"Invalid behavior attachment '{line.Content}' - expected 'uses <Behavior>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var arguments = new List<BehaviorArgumentSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var argument = BehaviorArgumentRegex().Match(child.Content);
            if (!argument.Success)
            {
                context.Error(
                    DiagnosticCodes.InvalidBehaviorArgument,
                    $"Invalid argument '{child.Content}' for behavior '{match.Groups[1].Value}' - expected '<parameter> <value>'",
                    child.Location);
                continue;
            }

            arguments.Add(new(argument.Groups[1].Value, argument.Groups[2].Value.Trim(), child.Location));
        }

        return new(match.Groups[1].Value, arguments, line.Location);
    }

    static void ParseParameter(ParserContext context, SourceLine line, List<BehaviorParameterSyntax> parameters)
    {
        var match = ParameterRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidBehaviorParameter, $"Invalid parameter declaration '{line.Content}' - expected 'parameter <name> [<Type>]'", line.Location);
            return;
        }

        var name = match.Groups[1].Value;
        if (parameters.Exists(parameter => string.Equals(parameter.Name, name, StringComparison.Ordinal)))
        {
            context.Error(DiagnosticCodes.DuplicateBehaviorParameter, $"A parameter named '{name}' is already declared - parameter names must be unique within a behavior", line.Location);
            return;
        }

        var type = match.Groups[2].Success ? PropertyLineParser.ParseTypeRef(match.Groups[2].Value, line.Location) : null;
        parameters.Add(new(name, type, line.Location));
    }

    static int? ParseOrder(ParserContext context, SourceLine line)
    {
        var match = OrderRegex().Match(line.Content);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var order))
        {
            return order;
        }

        context.Error(DiagnosticCodes.InvalidBehaviorOrder, $"Invalid order '{line.Content}' - expected 'order <number>'", line.Location);
        return null;
    }

    static InteractionBindingSyntax? ParseBinding(ParserContext context, SourceLine line)
    {
        var trigger = ParseTrigger(context, line);
        if (trigger is null)
        {
            context.SkipBlock(line.Indent);
            return null;
        }

        string? condition = null;
        var actions = new List<InteractionActionSyntax>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var where = WhereRegex().Match(child.Content);
            if (where.Success)
            {
                if (condition is not null)
                {
                    context.Error(DiagnosticCodes.RepeatedInteractionCondition, "The binding already declares a 'where' guard - a binding has at most one", child.Location);
                    continue;
                }

                condition = where.Groups[1].Value.Trim();
                continue;
            }

            if (ParseAction(context, child, 1) is { } action)
            {
                actions.Add(action);
            }
        }

        if (actions.Count == 0)
        {
            context.Error(DiagnosticCodes.InteractionBindingWithoutActions, $"'{line.Content}' declares no actions - a trigger with nothing to do is never what was meant", line.Location);
        }

        return new(trigger, condition, actions, line.Location);
    }

    static InteractionTriggerSyntax? ParseTrigger(ParserContext context, SourceLine line)
    {
        var match = OnRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(
                DiagnosticCodes.InvalidInteractionTrigger,
                $"Invalid interaction trigger '{line.Content}' - expected 'on <{string.Join('|', BuiltInTriggerNames)}|event <Event>|interval <n> <unit>|<ApplicationTrigger>>'",
                line.Location);
            return null;
        }

        var text = match.Groups[1].Value.Trim();

        if (_builtInTriggers.TryGetValue(text, out var kind))
        {
            return new BuiltInInteractionTriggerSyntax(kind, line.Location);
        }

        var @event = EventTriggerRegex().Match(text);
        if (@event.Success)
        {
            return new EventInteractionTriggerSyntax(@event.Groups[1].Value, line.Location);
        }

        var interval = IntervalTriggerRegex().Match(text);
        if (interval.Success)
        {
            var amount = int.Parse(interval.Groups[1].Value);
            var unit = ParseIntervalUnit(interval.Groups[2].Value);
            if (ToSeconds(amount, unit) < MinimumIntervalSeconds)
            {
                context.Warning(
                    DiagnosticCodes.IntervalBelowFloor,
                    $"An interval of {amount} {interval.Groups[2].Value} is below the {MinimumIntervalSeconds} second floor - a client cannot usefully honour it",
                    line.Location);
            }

            return new IntervalInteractionTriggerSyntax(amount, unit, line.Location);
        }

        if (ApplicationTriggerRegex().IsMatch(text))
        {
            return new ApplicationTriggerInteractionTriggerSyntax(text, line.Location);
        }

        context.Error(
            DiagnosticCodes.InvalidInteractionTrigger,
            $"Invalid interaction trigger 'on {text}' - expected one of {string.Join(", ", BuiltInTriggerNames)}, 'event <Event>', 'interval <n> <unit>', or a declared application trigger name",
            line.Location);
        return null;
    }

    static InteractionActionSyntax? ParseAction(ParserContext context, SourceLine line, int depth)
    {
        if (depth > MaximumActionDepth)
        {
            context.Error(
                DiagnosticCodes.InteractionNestingTooDeep,
                $"Interaction nesting is deeper than {MaximumActionDepth} levels at '{line.Content}' - extract the inner actions into a named behavior",
                line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var action = ParseActionHeader(context, line);
        if (action is null)
        {
            context.SkipBlock(line.Indent);
            return null;
        }

        var arguments = new List<InteractionArgumentSyntax>();
        var onSuccess = new List<InteractionActionSyntax>();
        var onFailure = new List<InteractionActionSyntax>();
        var onResult = new List<InteractionActionSyntax>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();

            var with = WithRegex().Match(child.Content);
            if (with.Success)
            {
                arguments.Add(new(with.Groups[1].Value, with.Groups[2].Value.Trim(), child.Location));
                continue;
            }

            if (LineText.FirstWord(child.Content) == "with")
            {
                context.Error(DiagnosticCodes.InvalidInteractionArgument, $"Invalid argument '{child.Content}' - expected 'with <name> from <binding>'", child.Location);
                continue;
            }

            var continuation = ContinuationRegex().Match(child.Content);
            if (continuation.Success)
            {
                ParseContinuation(context, child, continuation.Groups[1].Value, action, depth, onSuccess, onFailure, onResult);
                continue;
            }

            context.Error(
                DiagnosticCodes.UnknownInteractionAction,
                $"Unexpected '{child.Content}' in '{line.Content}' - expected 'with <name> from <binding>', 'on success', 'on failure' or 'on result'",
                child.Location);
            context.SkipBlock(child.Indent);
        }

        return action with
        {
            Arguments = arguments,
            OnSuccess = onSuccess,
            OnFailure = onFailure,
            OnResult = onResult
        };
    }

    static void ParseContinuation(
        ParserContext context,
        SourceLine line,
        string keyword,
        InteractionActionSyntax action,
        int depth,
        List<InteractionActionSyntax> onSuccess,
        List<InteractionActionSyntax> onFailure,
        List<InteractionActionSyntax> onResult)
    {
        var target = keyword switch
        {
            "success" => onSuccess,
            "failure" => onFailure,
            _ => onResult
        };

        // A continuation only means something on an action with an outcome. Attaching one to a navigation or a
        // notification is reported rather than accepted, because the author expects those actions to run.
        if (string.Equals(keyword, "result", StringComparison.Ordinal) && action is not OpenDialogActionSyntax)
        {
            context.Error(
                DiagnosticCodes.ResultContinuationOnNonDialogAction,
                $"'on result' is only valid on 'open dialog' - {ActionText(action)} produces no result",
                line.Location);
            context.SkipBlock(line.Indent);
            return;
        }

        if (!string.Equals(keyword, "result", StringComparison.Ordinal) && !CanFail(action))
        {
            context.Error(
                DiagnosticCodes.ContinuationOnNonFailableAction,
                $"'on {keyword}' is not valid on {ActionText(action)} - it has no outcome to branch on, so the continuation could never run",
                line.Location);
            context.SkipBlock(line.Indent);
            return;
        }

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (ParseAction(context, child, depth + 1) is { } continuation)
            {
                target.Add(continuation);
            }
        }
    }

    static InteractionActionSyntax? ParseActionHeader(ParserContext context, SourceLine line)
    {
        var content = line.Content;
        switch (LineText.FirstWord(content))
        {
            case "execute":
                {
                    var match = ExecuteRegex().Match(content);
                    if (!match.Success)
                    {
                        context.Error(DiagnosticCodes.InvalidExecuteAction, $"Invalid execute action '{content}' - expected 'execute <Command>'", line.Location);
                        return null;
                    }

                    return new ExecuteCommandActionSyntax(match.Groups[1].Value, line.Location);
                }

            case "navigate":
                {
                    if (NavigateBackRegex().IsMatch(content))
                    {
                        return new NavigateBackActionSyntax(line.Location);
                    }

                    var match = NavigateToRegex().Match(content);
                    if (!match.Success)
                    {
                        context.Error(DiagnosticCodes.InvalidNavigateAction, $"Invalid navigate action '{content}' - expected 'navigate to <Screen>' or 'navigate back'", line.Location);
                        return null;
                    }

                    return new NavigateActionSyntax(match.Groups[1].Value, line.Location);
                }

            case "open":
                {
                    var match = OpenDialogRegex().Match(content);
                    if (!match.Success)
                    {
                        context.Error(DiagnosticCodes.InvalidOpenDialogAction, $"Invalid open dialog action '{content}' - expected 'open dialog <DialogTemplate>'", line.Location);
                        return null;
                    }

                    return new OpenDialogActionSyntax(match.Groups[1].Value, line.Location);
                }

            case "close":
                if (!CloseDialogRegex().IsMatch(content))
                {
                    context.Error(DiagnosticCodes.InvalidOpenDialogAction, $"Invalid close action '{content}' - expected 'close dialog'", line.Location);
                    return null;
                }

                return new CloseDialogActionSyntax(line.Location);

            case "refresh":
                {
                    var match = RefreshRegex().Match(content);
                    if (!match.Success)
                    {
                        context.Error(DiagnosticCodes.InvalidRefreshAction, $"Invalid refresh action '{content}' - expected 'refresh <Query>'", line.Location);
                        return null;
                    }

                    return new RefreshQueryActionSyntax(match.Groups[1].Value, line.Location);
                }

            case "set":
                {
                    var match = SetRegex().Match(content);
                    if (!match.Success)
                    {
                        context.Error(DiagnosticCodes.InvalidSetAction, $"Invalid set action '{content}' - expected 'set <target> to <value>'", line.Location);
                        return null;
                    }

                    return new SetStateActionSyntax(match.Groups[1].Value, match.Groups[2].Value.Trim(), line.Location);
                }

            case "notify":
                {
                    var match = NotifyRegex().Match(content);
                    if (!match.Success)
                    {
                        context.Error(DiagnosticCodes.InvalidNotifyAction, $"Invalid notify action '{content}' - expected 'notify <info|warning|error> \"<text>\"'", line.Location);
                        return null;
                    }

                    var level = match.Groups[1].Value switch
                    {
                        "info" => NotificationLevel.Info,
                        "warning" => NotificationLevel.Warning,
                        _ => NotificationLevel.Error
                    };

                    return new NotifyActionSyntax(level, MessageText(match, 2), line.Location) { MessageIsLiteral = match.Groups[2].Success };
                }

            case "confirm":
                {
                    var match = ConfirmRegex().Match(content);
                    if (!match.Success)
                    {
                        context.Error(DiagnosticCodes.InvalidConfirmAction, $"Invalid confirm action '{content}' - expected 'confirm \"<text>\"'", line.Location);
                        return null;
                    }

                    return new ConfirmActionSyntax(MessageText(match, 1), line.Location) { MessageIsLiteral = match.Groups[1].Success };
                }

            case "raise":
                {
                    var match = RaiseRegex().Match(content);
                    if (!match.Success)
                    {
                        context.Error(DiagnosticCodes.InvalidRaiseAction, $"Invalid raise action '{content}' - expected 'raise <ApplicationTrigger>'", line.Location);
                        return null;
                    }

                    return new RaiseTriggerActionSyntax(match.Groups[1].Value, line.Location);
                }

            default:
                context.Error(
                    DiagnosticCodes.UnknownInteractionAction,
                    $"Unexpected '{content}' where an action was expected - expected execute, navigate, open dialog, close dialog, refresh, set, notify, confirm or raise",
                    line.Location);
                return null;
        }
    }

    /// <summary>
    /// Whether the action has an outcome a continuation can branch on. <c>navigate</c>, <c>notify</c>,
    /// <c>set</c> and <c>close dialog</c> do not - they are effects that happen.
    /// </summary>
    static bool CanFail(InteractionActionSyntax action) =>
        action is ExecuteCommandActionSyntax or RefreshQueryActionSyntax or ConfirmActionSyntax or OpenDialogActionSyntax or RaiseTriggerActionSyntax;

    static string ActionText(InteractionActionSyntax action) => action switch
    {
        ExecuteCommandActionSyntax execute => $"'execute {execute.Command}'",
        NavigateActionSyntax navigate => $"'navigate to {navigate.Screen}'",
        NavigateBackActionSyntax => "'navigate back'",
        OpenDialogActionSyntax open => $"'open dialog {open.DialogTemplate}'",
        CloseDialogActionSyntax => "'close dialog'",
        RefreshQueryActionSyntax refresh => $"'refresh {refresh.Query}'",
        SetStateActionSyntax set => $"'set {set.Target}'",
        NotifyActionSyntax => "'notify'",
        ConfirmActionSyntax => "'confirm'",
        RaiseTriggerActionSyntax raise => $"'raise {raise.Trigger}'",
        _ => "the action"
    };

    static IntervalUnit ParseIntervalUnit(string text) => text switch
    {
        "second" or "seconds" => IntervalUnit.Seconds,
        "minute" or "minutes" => IntervalUnit.Minutes,
        "hour" or "hours" => IntervalUnit.Hours,
        _ => IntervalUnit.Days
    };

    static int ToSeconds(int amount, IntervalUnit unit) => unit switch
    {
        IntervalUnit.Seconds => amount,
        IntervalUnit.Minutes => amount * 60,
        IntervalUnit.Hours => amount * 3600,
        _ => amount * 86400
    };

    /// <summary>
    /// Reads a message operand in any of the three forms it is accepted in: a quoted literal, a
    /// <c>$strings.</c> reference, or a binding the surrounding scope supplies - a behavior's parameter, or a
    /// value from the interaction's context.
    /// </summary>
    static string MessageText(Match match, int quotedGroup)
    {
        if (match.Groups[quotedGroup].Success)
        {
            return StringLiteral.Unescape(match.Groups[quotedGroup].Value);
        }

        return match.Groups[quotedGroup + 1].Success
            ? match.Groups[quotedGroup + 1].Value
            : match.Groups[quotedGroup + 2].Value;
    }

    [GeneratedRegex(@"^behavior\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^parameter\s+([A-Za-z_]\w*)(?:\s+([\w.]+(?:\[\])?))?$", RegexOptions.None, 1000)]
    private static partial Regex ParameterRegex();

    [GeneratedRegex(@"^order\s+(-?\d+)$", RegexOptions.None, 1000)]
    private static partial Regex OrderRegex();

    [GeneratedRegex(@"^on\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex OnRegex();

    [GeneratedRegex(@"^where\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex WhereRegex();

    [GeneratedRegex(@"^event\s+([A-Za-z_]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex EventTriggerRegex();

    [GeneratedRegex(@"^interval\s+(\d+)\s+(seconds?|minutes?|hours?|days?)$", RegexOptions.None, 1000)]
    private static partial Regex IntervalTriggerRegex();

    [GeneratedRegex(@"^[A-Z]\w*$", RegexOptions.None, 1000)]
    private static partial Regex ApplicationTriggerRegex();

    [GeneratedRegex(@"^on\s+(success|failure|result)$", RegexOptions.None, 1000)]
    private static partial Regex ContinuationRegex();

    [GeneratedRegex(@"^with\s+([A-Za-z_]\w*)\s+from\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex WithRegex();

    [GeneratedRegex(@"^execute\s+([A-Za-z_]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex ExecuteRegex();

    [GeneratedRegex(@"^navigate\s+to\s+([A-Za-z_]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex NavigateToRegex();

    [GeneratedRegex(@"^navigate\s+back$", RegexOptions.None, 1000)]
    private static partial Regex NavigateBackRegex();

    [GeneratedRegex(@"^open\s+dialog\s+([A-Za-z_]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex OpenDialogRegex();

    [GeneratedRegex(@"^close\s+dialog$", RegexOptions.None, 1000)]
    private static partial Regex CloseDialogRegex();

    [GeneratedRegex(@"^refresh\s+([A-Za-z_]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex RefreshRegex();

    [GeneratedRegex(@"^set\s+([\w.]+)\s+to\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex SetRegex();

    [GeneratedRegex("^notify\\s+(info|warning|error)\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*)|([A-Za-z_][\\w.]*))$", RegexOptions.None, 1000)]
    private static partial Regex NotifyRegex();

    [GeneratedRegex("^confirm\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*)|([A-Za-z_][\\w.]*))$", RegexOptions.None, 1000)]
    private static partial Regex ConfirmRegex();

    [GeneratedRegex(@"^raise\s+([A-Za-z_]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex RaiseRegex();

    [GeneratedRegex(@"^uses\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex UsesRegex();

    [GeneratedRegex(@"^([A-Za-z_]\w*)\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex BehaviorArgumentRegex();
}
