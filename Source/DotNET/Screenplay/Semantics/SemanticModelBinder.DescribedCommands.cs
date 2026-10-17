// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        SemanticCommandHandler? BindCommandHandler(SemanticAddress address, CommandSyntax command, DeclarationScope scope)
        {
            if (command.Handler is not { } handler) return null;
            if (!DescribedCommandAdmission.IsDescribed(command))
            {
                RequireImplementation(SemanticImplementationRole.CommandHandler, address, handler.File, handler.Code);
                var subject = DiagnosticArgument.Declaration("command", [.. scope.Segments, command.Name]);
                Error(DiagnosticCodes.CommandHandlerRequiresAttachment, $"Command '{command.Name}' handler requires a constrained implementation attachment.", handler.Location, DiagnosticArguments.Empty.With("subject", subject));
                return null;
            }

            if (DescribedCommandAdmission.Refusal(command) is { } refusal)
            {
                if (refusal.Code == DiagnosticCodes.DescribedCommandCardinalityNotAdmitted)
                {
                    foreach (var production in command.Produces.Where(production => production.When is not null)) Error(DiagnosticCodes.DescribedCommandCardinalityNotAdmitted, refusal.Message, production.Location);
                }
                else
                {
                    Error(DiagnosticCodes.DescribedCommandWithoutProductions, refusal.Message, refusal.Location);
                }

                return null;
            }

            var pending = RequirePendingImplementation(address, handler);
            if (pending is null) return null;
            Information(DiagnosticCodes.DescribedCommandNotImplemented, $"Command '{command.Name}' is described but not implemented (requirement {pending.RequirementId}).", handler.Location);

            return new(pending.RequirementId, 1);
        }
    }
}
