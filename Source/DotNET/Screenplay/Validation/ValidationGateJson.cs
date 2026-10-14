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
        if (issue is not null) return new(root, path, null, ValidationGateLocations.Failure(path, bytes, ValidationGateLocations.Offset(bytes, issue.Value.Parent, issue.Value.Member), issue.Value.Reason));
        var declaration = new DeclaredValidationGate();
        if (document.RootElement.TryGetProperty("validation", out var validation)) declaration = Declaration(validation);
        return new(root, path, declaration, null);
    }

    static GateIssue? Validate(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) return new(null, string.Empty, "Expected a JSON object.");
        return root.EnumerateObject().Select(TopLevelIssue).FirstOrDefault(issue => issue is not null);
    }

    static GateIssue? TopLevelIssue(JsonProperty property) => property.Name switch
    {
        "$schema" => Located(null, property.Name, StringReason(property.Value)),
        "validation" => ValidateSettings(property.Value),
        _ => Located(null, property.Name, "Unknown top-level member.")
    };

    static GateIssue? ValidateSettings(JsonElement settings)
    {
        if (settings.ValueKind != JsonValueKind.Object) return new(null, "validation", "Expected a validation object.");
        return settings.EnumerateObject().Select(SettingIssue).FirstOrDefault(issue => issue is not null);
    }

    static GateIssue? SettingIssue(JsonProperty property) => Located("validation", property.Name, SettingReason(property));

    static string? SettingReason(JsonProperty property) => property.Name switch
    {
        "checks" => ChecksReason(property.Value),
        "warningsAsErrors" => BooleanReason(property.Value),
        "scope" => ScopeReason(property.Value),
        "openQuestions" => QuestionsReason(property.Value),
        _ => "Unknown validation member."
    };

    static GateIssue? Located(string? parent, string member, string? reason) => reason is null ? null : new(parent, member, reason);

    static string? BooleanReason(JsonElement value) => value.ValueKind is JsonValueKind.True or JsonValueKind.False ? null : "Expected a boolean.";

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
        return value.EnumerateArray().Select(CheckReason).FirstOrDefault(reason => reason is not null);
    }

    static string? CheckReason(JsonElement item)
    {
        if (StringReason(item) is { } reason) return reason;
        var name = item.GetString()!;
        if (name.Contains(',')) return "Each checks item must contain one selection, not comma-separated names.";
        return IsKnownCheck(name) ? null : $"Unknown check selection '{name}'.";
    }

    static bool IsKnownCheck(string name) => CompletenessChecks.AcceptedNames.Contains(name, StringComparer.Ordinal) && CompletenessChecks.TryParse(name, out _);

    static DeclaredValidationGate Declaration(JsonElement value) => new(
        value.TryGetProperty("checks", out var checks) ? [.. checks.EnumerateArray().Select(item => item.GetString()!)] : null,
        value.TryGetProperty("warningsAsErrors", out var warnings) ? warnings.GetBoolean() : null,
        value.TryGetProperty("scope", out var scope) ? scope.GetString() : null,
        value.TryGetProperty("openQuestions", out var questions) ? questions.GetString() : null);

    readonly record struct GateIssue(string? Parent, string Member, string Reason);
}
