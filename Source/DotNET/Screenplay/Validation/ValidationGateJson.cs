// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Completeness;

namespace Cratis.Screenplay.Validation;

static class ValidationGateJson
{
    internal static ValidationGateReadResult Read(string root, string path, byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        var duplicate = ValidationGateLocations.Duplicate(bytes);
        if (duplicate is not null) return new(root, path, null, ValidationGateLocations.Failure(path, bytes, duplicate.Value.Offset, $"Duplicate member '{duplicate.Value.Name}'."));
        var issue = Validate(document.RootElement);
        if (issue is not null) return new(root, path, null, ValidationGateLocations.Failure(path, bytes, ValidationGateLocations.Offset(bytes, issue.Value.Member), issue.Value.Reason));
        var declaration = new DeclaredValidationGate();
        if (document.RootElement.TryGetProperty("validation", out var validation)) declaration = Declaration(validation);
        return new(root, path, declaration, null);
    }

    static (string Member, string Reason)? Validate(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) return (string.Empty, "Expected a JSON object.");
        foreach (var property in root.EnumerateObject())
        {
            var reason = property.Name switch
            {
                "$schema" => StringReason(property.Value),
                "validation" => null,
                _ => "Unknown top-level member."
            };
            if (reason is not null) return (property.Name, reason);
            if (property.Name == "validation")
            {
                var issue = ValidateSettings(property.Value);
                if (issue is not null) return issue;
            }
        }

        return null;
    }

    static (string Member, string Reason)? ValidateSettings(JsonElement settings)
    {
        if (settings.ValueKind != JsonValueKind.Object) return ("validation", "Expected a validation object.");
        foreach (var property in settings.EnumerateObject())
        {
            var reason = property.Name switch
            {
                "checks" => ChecksReason(property.Value),
                "warningsAsErrors" => property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False ? null : "Expected a boolean.",
                "scope" => ScopeReason(property.Value),
                "openQuestions" => QuestionsReason(property.Value),
                _ => "Unknown validation member."
            };
            if (reason is not null) return (property.Name, reason);
        }

        return null;
    }

    static string? StringReason(JsonElement value) => value.ValueKind == JsonValueKind.String ? null : "Expected a string.";

    static string? ScopeReason(JsonElement value)
    {
        if (StringReason(value) is { } reason) return reason;
        var scope = value.GetString();
        return string.IsNullOrWhiteSpace(scope) || scope.StartsWith('-') ? "Expected a module, feature or slice address." : null;
    }

    static string? QuestionsReason(JsonElement value)
    {
        if (StringReason(value) is { } reason) return reason;
        var mode = value.GetString();
        return string.Equals(mode, "report", StringComparison.Ordinal) || string.Equals(mode, "fail", StringComparison.Ordinal) ? null : "Expected report or fail.";
    }

    static string? ChecksReason(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array) return "Expected an array of check selections.";
        foreach (var item in value.EnumerateArray())
        {
            if (StringReason(item) is { } reason) return reason;
            var name = item.GetString()!;
            if (name.Contains(',')) return "Each checks item must contain one selection, not comma-separated names.";
            if (!CompletenessChecks.AcceptedNames.Contains(name, StringComparer.Ordinal)) return $"Unknown check selection '{name}'.";
            if (!CompletenessChecks.TryParse(name, out _)) return $"Unknown check selection '{name}'.";
        }

        return null;
    }

    static DeclaredValidationGate Declaration(JsonElement value) => new(
        value.TryGetProperty("checks", out var checks) ? [.. checks.EnumerateArray().Select(item => item.GetString()!)] : null,
        value.TryGetProperty("warningsAsErrors", out var warnings) ? warnings.GetBoolean() : null,
        value.TryGetProperty("scope", out var scope) ? scope.GetString() : null,
        value.TryGetProperty("openQuestions", out var questions) ? questions.GetString() : null);
}
