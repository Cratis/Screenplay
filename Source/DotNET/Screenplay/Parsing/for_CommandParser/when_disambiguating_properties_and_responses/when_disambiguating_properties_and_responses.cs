// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Parsing.for_CommandParser.when_disambiguating_properties_and_responses;

public class when_disambiguating_properties_and_responses
{
    const string Prefix = "concept AccountId : Uuid\n\neventsource Account\n  identifier AccountId\n  stream Transactions\n  stream Other\n\nmodule M\n  feature F\n    slice StateChange S\n      command C\n";

    public static TheoryData<string, string, string> Cases => [.. CommandCases.All.Select(c => ((string)c[0], (string)c[1], (string)c[2]))];

    [Theory]
    [MemberData(nameof(Cases))]
    void should_keep_the_diagnostics_recovery_and_syntax(string name, string source, string expected)
    {
        Assert.NotEmpty(name);
        Snapshot(source).ShouldEqual(expected);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    void should_recover_into_the_sibling_marker_command(string name, string source, string expected)
    {
        Assert.NotEmpty(name);
        Assert.NotEmpty(expected);
        MarkerProperty(source).ShouldEqual("marker String");
    }

    internal static string Snapshot(string source)
    {
        var result = Parse(source);
        var command = FindCommand(SyntaxJson.Serialize(result.Value!), "C");
        return string.Join('\n', [.. result.Diagnostics.Select(d => $"{d.Severity.ToString().ToLowerInvariant()} {d.Code} {d.Location.Line}:{d.Location.Column} {d.Message}"), command?.GetRawText() ?? "no command"]);
    }

    static string MarkerProperty(string source)
    {
        if (FindCommand(SyntaxJson.Serialize(Parse(source).Value!), "Marker") is not { } marker)
        {
            return "no marker command";
        }

        return string.Join(',', marker.GetProperty("properties").EnumerateArray().Select(p => $"{p.GetProperty("name").GetString()} {p.GetProperty("type").GetProperty("name").GetString()}"));
    }

    static CompilationResult<ApplicationSyntax> Parse(string source) =>
        new ScreenplayCompiler().Parse($"{Prefix}{source}\n      command Marker\n        marker String\n", "fragment.play");

    static JsonElement? FindCommand(JsonElement node, string name)
    {
        switch (node.ValueKind)
        {
            case JsonValueKind.Object:
                if (node.TryGetProperty("kind", out var kind) && kind.GetString() == "CommandSyntax" && node.GetProperty("name").GetString() == name)
                {
                    return node;
                }

                foreach (var property in node.EnumerateObject())
                {
                    if (FindCommand(property.Value, name) is { } found)
                    {
                        return found;
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in node.EnumerateArray())
                {
                    if (FindCommand(item, name) is { } found)
                    {
                        return found;
                    }
                }

                break;
        }

        return null;
    }
}
