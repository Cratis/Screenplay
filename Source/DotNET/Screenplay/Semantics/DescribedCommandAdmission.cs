// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

internal static class DescribedCommandAdmission
{
    internal static bool IsDescribed(CommandSyntax command) => command.Handler is { } handler && CommandHandlerValidator.IsPending(handler);

    internal static Diagnostic? Refusal(CommandSyntax command)
    {
        if (!command.Produces.Any())
        {
            return Diagnostic.Error(DiagnosticCodes.DescribedCommandWithoutProductions, $"Described command '{command.Name}' requires at least one production.", command.Handler!.Location);
        }

        return command.Produces.FirstOrDefault(production => production.When is not null) is { } conditional
            ? Diagnostic.Error(DiagnosticCodes.DescribedCommandCardinalityNotAdmitted, $"Described command '{command.Name}' requires unconditional productions in result version 1.", conditional.Location)
            : null;
    }
}
