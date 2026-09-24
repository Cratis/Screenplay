// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Reads JSON-shaped values, retaining the exact authored spans of keys and nested literals.
/// </summary>
internal sealed class StructuredValueParser(string text, SourceLocation start, ParserContext context)
{
    int _position;

    internal ExpressionSyntax Parse()
    {
        using var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 64 });
        return Value(document.RootElement);
    }

    static double Number(JsonElement element)
    {
        if (!element.TryGetDouble(out var number) || !double.IsFinite(number))
        {
            throw new InvalidStructuredNumber("JSON number is outside the finite Double range.");
        }

        return number;
    }

    SourceLocation At() => start with { Column = start.Column + _position };

    void Whitespace()
    {
        while (_position < text.Length && char.IsWhiteSpace(text[_position]))
        {
            _position++;
        }
    }

    void String()
    {
        _position++;
        while (_position < text.Length)
        {
            if (text[_position++] == '\\')
            {
                _position++;
            }
            else if (text[_position - 1] == '"')
            {
                return;
            }
        }
    }

    ExpressionSyntax Value(JsonElement element)
    {
        Whitespace();
        var location = At();
        if (element.ValueKind == JsonValueKind.Array)
        {
            _position++;
            var items = new List<ExpressionSyntax>();
            foreach (var item in element.EnumerateArray())
            {
                items.Add(Value(item));
                Whitespace();
                if (text[_position] == ',')
                {
                    _position++;
                }
            }

            _position++;
            return new ListExpressionSyntax(items, location);
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            _position++;
            var members = new List<ObjectMemberSyntax>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                Whitespace();
                var keyLocation = At();
                var keyStart = _position;
                String();
                var keyLength = _position - keyStart;
                Whitespace();
                _position++; // colon
                var memberValue = Value(property.Value);
                if (!names.Add(property.Name))
                {
                    context.Error(DiagnosticCodes.DuplicateStructuredValueMember, $"Duplicate property '{property.Name}' in structured value", keyLocation);
                }
                else
                {
                    members.Add(new ObjectMemberSyntax(property.Name, memberValue, keyLocation)
                    {
                        RawLocation = keyLocation,
                        RawLength = keyLength
                    });
                }
                Whitespace();
                if (text[_position] == ',')
                {
                    _position++;
                }
            }

            _position++;
            return new ObjectExpressionSyntax(members, location);
        }

        object? value = element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => Number(element),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
        if (element.ValueKind == JsonValueKind.String)
        {
            String();
        }
        else
        {
            _position += element.GetRawText().Length;
        }

        return new LiteralExpressionSyntax(value, location) { RawLocation = location, RawLength = _position - (location.Column - start.Column) };
    }
}
