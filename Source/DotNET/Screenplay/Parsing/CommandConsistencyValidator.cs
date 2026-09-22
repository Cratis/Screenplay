// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks validation subjects and the types supplied to declared read queries.
/// </summary>
internal static class CommandConsistencyValidator
{
    /// <summary>
    /// Validates every command against declarations in its scope.
    /// </summary>
    /// <param name="declarations">The application declarations.</param>
    /// <param name="context">The diagnostic sink.</param>
    public static void Validate(ConsistencyDeclarations declarations, ParserContext context)
    {
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var command in slice.Commands)
            {
                ValidateSubjects(command, declarations, context);
                ValidateReads(command, scope, declarations, context);
            }
        }
    }

    static void ValidateSubjects(CommandSyntax command, ConsistencyDeclarations declarations, ParserContext context)
    {
        foreach (var rule in command.Validations.OfType<DeclarativeValidateSyntax>().SelectMany(validation => validation.Rules))
        {
            declarations.Property(command.Properties, rule.Property, out var missing);
            if (missing)
            {
                context.Error(
                    DiagnosticCodes.UnknownValidationTarget,
                    $"Validation of '{rule.Property}' targets no declared field of command '{command.Name}'",
                    rule.Location);
            }
        }
    }

    static void ValidateReads(CommandSyntax command, DeclarationScope scope, ConsistencyDeclarations declarations, ParserContext context)
    {
        foreach (var read in (command.Reads ?? []).Where(read => read.By is not null))
        {
            var property = declarations.Property(command.Properties, read.By!, out _);
            var view = declarations.View(read.ReadModel, scope);
            if (property is null || view is null)
            {
                continue;
            }

            var queries = declarations.Slices.SelectMany(entry => entry.Slice.Queries
                .Where(query => declarations.View(query.ReturnType.Name, entry.Scope) == view)).ToList();
            var parameters = queries.Select(query => query.By).OfType<QueryParameterSyntax>().ToList();

            // No query signature is not evidence of incompatibility: the read may be realized externally.
            if (queries.Count > 0 && parameters.Count > 0 && parameters.TrueForAll(parameter => declarations.Compatible(property.Type, parameter.Type) == false))
            {
                context.Error(
                    DiagnosticCodes.IncompatibleReadsKey,
                    $"Command '{command.Name}' reads '{read.ReadModel}' by '{read.By}' of type '{property.Type.Name}', which no declared query of that view accepts as its key",
                    read.Location);
            }
        }
    }
}
