// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Prevents a pending command handler intent from being replaced by another handler.
/// </summary>
internal static class CommandHandlerValidator
{
    internal static bool IsPending(HandlerSyntax handler) => handler is { Implementation: not null, File: null, Code: null };

    internal static void Validate(IEnumerable<(HandlerSyntax? Handler, SourceLocation Location)> occurrences, ParserContext context)
    {
        var handlers = occurrences.ToList();
        if (!handlers.Exists(entry => entry.Handler is { } handler && IsPending(handler))) return;
        foreach (var entry in handlers.Skip(1))
        {
            context.Error(DiagnosticCodes.PendingIntentWithOtherHandler, "A pending handler intent cannot coexist with another handler on the same command.", entry.Location);
        }
    }

    // Programmatic composition may retain same-named command parts instead of parsing one body.
    internal static void Validate(IEnumerable<CommandSyntax> commands, ParserContext context)
    {
        foreach (var group in commands.GroupBy(command => command.Name, StringComparer.Ordinal))
        {
            Validate(group.Where(command => command.Handler is not null).Select(command => (command.Handler, command.Handler!.Location)), context);
        }
    }
}
