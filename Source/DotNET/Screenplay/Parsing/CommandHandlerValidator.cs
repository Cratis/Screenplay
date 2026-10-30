// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Prevents a command handler from being silently replaced by another handler.
/// </summary>
internal static class CommandHandlerValidator
{
    internal static bool IsPending(HandlerSyntax handler) => handler is { Implementation: not null, File: null, Code: null };

    internal static void Validate(IEnumerable<(HandlerSyntax? Handler, SourceLocation Location)> occurrences, ParserContext context)
    {
        var handlers = occurrences.ToList();
        var hasPending = handlers.Exists(entry => entry.Handler is { } handler && IsPending(handler));
        var code = hasPending ? DiagnosticCodes.PendingIntentWithOtherHandler : DiagnosticCodes.DuplicateDeclaration;
        var message = hasPending
            ? "A pending handler intent cannot coexist with another handler on the same command."
            : "A command cannot declare more than one handler.";
        foreach (var entry in handlers.Skip(1))
        {
            context.Error(code, message, entry.Location);
        }
    }

    // Programmatic composition may retain same-named command parts instead of parsing one body.
    internal static void Validate(IEnumerable<(HandlerSyntax? Handler, SourceLocation Location)> handlers, HandlerSyntax? handler, bool hasProduces, ParserContext context)
    {
        Validate(handlers, context);
        ValidatePossibleEvents(handler, hasProduces, context);
    }

    internal static void ValidatePossibleEvents(HandlerSyntax? handler, bool hasProduces, ParserContext context)
    {
        if (hasProduces && handler?.PossibleEvents is { } possible)
        {
            context.Error(DiagnosticCodes.InvalidPossibleEventsDeclaration, "A command cannot combine 'produces' with a handler 'may produce' set.", possible.Location);
        }
    }

    internal static void Validate(IEnumerable<CommandSyntax> commands, ParserContext context)
    {
        foreach (var group in commands.GroupBy(command => command.Name, StringComparer.Ordinal))
        {
            Validate(group.Where(command => command.Handler is not null).Select(command => (command.Handler, command.Handler!.Location)), context);
            if (group.Any(command => command.Produces.Any()))
            {
                foreach (var possible in group.Select(command => command.Handler?.PossibleEvents).OfType<PossibleEventsSyntax>())
                {
                    context.Error(DiagnosticCodes.InvalidPossibleEventsDeclaration, "A command cannot combine 'produces' with a handler 'may produce' set.", possible.Location);
                }
            }
        }
    }
}
