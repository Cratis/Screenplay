// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

internal static class ScreenActionPresentation
{
    const string InvalidValueMessage = "Action presentation requires a key and string value.";

    internal static string? InvalidReason(ScreenActionSyntax action)
    {
        var presentation = action.Presentation;
        if (presentation is null) return "Action presentation must be a collection.";
        if (presentation.Any() && action.Subject != ScreenActionSubjectKind.Item) return "Action presentation requires an explicitly item-scoped action ('for item').";
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in presentation)
        {
            if (InvalidValue(value, names) is { } reason) return reason;
        }

        return null;
    }

    static string? InvalidValue(PresentationValueSyntax? value, HashSet<string> names)
    {
        if (value is null) return InvalidValueMessage;
        if (string.IsNullOrWhiteSpace(value.Name)) return InvalidValueMessage;
        if (value.Value is null) return InvalidValueMessage;
        if (!names.Add(value.Name)) return $"Duplicate action presentation key '{value.Name}'.";
        return InvalidPlacement(value);
    }

    static string? InvalidPlacement(PresentationValueSyntax value)
    {
        if (value.Name != "placement") return null;
        return string.IsNullOrWhiteSpace(value.Value) ? "Action presentation placement must not be empty." : null;
    }
}
