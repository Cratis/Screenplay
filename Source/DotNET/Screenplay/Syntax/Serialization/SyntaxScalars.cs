// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization;

internal static class SyntaxScalars
{
    internal const string TimePattern = "^([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9](\\.[0-9]{1,7})?$";

    internal static object Read(JsonElement value, Type type, string path)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(string) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString()!;
        }

        if (type == typeof(bool) && value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        if (type == typeof(int) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var integer))
        {
            return integer;
        }

        if (type.IsEnum && value.ValueKind == JsonValueKind.String && Enum.GetNames(type).Contains(value.GetString(), StringComparer.Ordinal))
        {
            return Enum.Parse(type, value.GetString()!, false);
        }

        if (type == typeof(TimeOnly) && value.ValueKind == JsonValueKind.String &&
            TimeOnly.TryParseExact(value.GetString(), ["HH:mm:ss", "HH:mm:ss.FFFFFFF"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            return time;
        }

        if (type == typeof(object))
        {
            return SyntaxLiterals.Read(value, path);
        }

        throw new InvalidSyntaxJson($"{path}: expected {type.Name}, received {value.ValueKind}.");
    }

    internal static object Write(object value, Type type, string path)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(object))
        {
            return SyntaxLiterals.Write(value, path);
        }

        if (type.IsEnum && Enum.IsDefined(type, value))
        {
            return value.ToString()!;
        }

        if (value is TimeOnly time)
        {
            return time.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
        }

        if (value is string or bool or int)
        {
            return value;
        }

        throw new InvalidSyntaxJson($"{path}: unsupported {type.Name} value '{value}'.");
    }
}
