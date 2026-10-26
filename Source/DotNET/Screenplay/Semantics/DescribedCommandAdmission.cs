// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

internal static class DescribedCommandAdmission
{
    static readonly IReadOnlyDictionary<string, string> _handlerLanguages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".cs"] = "csharp",
        [".ts"] = "typescript",
        [".js"] = "javascript",
        [".java"] = "java",
        [".kt"] = "kotlin",
        [".ex"] = "elixir",
        [".go"] = "go"
    };

    internal static bool HasImplementationContent(SemanticImplementationRole role, string? content) =>
        content is not null && (role != SemanticImplementationRole.CommandHandler || !string.IsNullOrWhiteSpace(content));

    internal static string? ImplementationLanguage(SemanticImplementationRole role, FileReferenceSyntax? file, CodeBlockSyntax? code) =>
        code?.Language ?? (role == SemanticImplementationRole.CommandHandler ? HandlerLanguage(file?.Path) : null);

    internal static bool IsDescribed(CommandSyntax command) => command.Handler is { } handler && CommandHandlerValidator.IsPending(handler);

    internal static bool HasResultContract(CommandSyntax command) =>
        command.Handler is not null && command.Produces.Any() && command.Produces.All(production => production.When is null);

    internal static bool IsResolvedBody(SemanticImplementationRequirement requirement) =>
        requirement.AttachmentResolution == SemanticAttachmentResolution.Resolved &&
        !string.IsNullOrWhiteSpace(requirement.Language) &&
        IsLowercaseHash(requirement.ContentHash) && HasBodyReference(requirement) &&
        requirement.Hints.IsEmpty && !requirement.IsAmbiguous;

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

    static string? HandlerLanguage(string? path) => _handlerLanguages.GetValueOrDefault(Path.GetExtension(path) ?? string.Empty);

    static bool IsLowercaseHash(string hash) => hash.Length == 64 && hash.All(char.IsAsciiHexDigitLower);

    static bool HasBodyReference(SemanticImplementationRequirement requirement) =>
        (requirement.BodySpan is { } span && span.End > span.Start) || !string.IsNullOrWhiteSpace(requirement.File);
}
