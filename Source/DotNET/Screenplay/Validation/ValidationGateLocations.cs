// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Validation;

static class ValidationGateLocations
{
    internal static (string Name, long Offset)? Duplicate(byte[] bytes)
    {
        var reader = new Utf8JsonReader(bytes);
        var objects = new Stack<HashSet<string>>();
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    objects.Push(new(StringComparer.Ordinal));
                    break;
                case JsonTokenType.EndObject:
                    objects.Pop();
                    break;
                case JsonTokenType.PropertyName:
                    if (!objects.Peek().Add(reader.GetString()!)) return (reader.GetString()!, reader.TokenStartIndex);
                    break;
            }
        }

        return null;
    }

    internal static long Offset(byte[] bytes, string member)
    {
        var reader = new Utf8JsonReader(bytes);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == member) return reader.TokenStartIndex;
        }

        return 0;
    }

    internal static ValidationGateFailure Failure(string path, byte[] bytes, long offset, string reason)
    {
        var prefix = Encoding.UTF8.GetString(bytes.AsSpan(0, (int)offset));
        var line = prefix.Count(character => character == '\n') + 1;
        var column = prefix.Length - prefix.LastIndexOf('\n');
        return new(ValidationGateFailureKind.Invalid, path, line, column, reason);
    }
}
