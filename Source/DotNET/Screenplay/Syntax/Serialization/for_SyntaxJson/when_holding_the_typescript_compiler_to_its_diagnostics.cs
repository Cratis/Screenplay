// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.Languages;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

// The TypeScript compiler (Source/Screenplay/Compiler) is held to a set of invalid documents and the
// diagnostics they are reported with, in Conformance/diagnostics.json. This holds the C# compiler to the
// same file, so the two report the same codes on the same lines - an error path cannot drift on one side.
public class when_holding_the_typescript_compiler_to_its_diagnostics : Specification
{
    readonly List<string> _mismatches = [];
    List<(string Name, string Source, string[] Expected, bool Validate, string[] RegisteredTriggers, string[]? Messages)> _vectors;

    void Establish() => _vectors = [.. Vectors()];

    void Because()
    {
        foreach (var (name, source, expected, validate, registeredTriggers, messages) in _vectors)
        {
            var compiler = new ScreenplayCompiler(new ScreenplayLanguageRegistry(triggers: registeredTriggers.Select(triggerName => new TriggerDefinition(triggerName))));
            var result = validate ? compiler.Compile(source) : compiler.Parse(source);
            var reported = result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}").ToArray();
            if (!reported.SequenceEqual(expected, StringComparer.Ordinal))
            {
                _mismatches.Add($"{name}: expected [{string.Join(", ", expected)}], C# reports [{string.Join(", ", reported)}]");
            }

            if (messages is not null && !result.Diagnostics.Select(diagnostic => diagnostic.Message).SequenceEqual(messages, StringComparer.Ordinal))
            {
                _mismatches.Add($"{name}: expected messages [{string.Join(", ", messages)}], C# reports [{string.Join(", ", result.Diagnostics.Select(diagnostic => diagnostic.Message))}]");
            }
        }
    }

    // Without this the comparison could pass by reading nothing - a moved or emptied file would otherwise
    // turn it into a check of nothing against nothing.
    [Fact] void should_hold_vectors() => _vectors.Count.ShouldBeGreaterThan(100);

    [Fact] void should_report_what_the_typescript_compiler_reports() => string.Join(Environment.NewLine, _mismatches).ShouldEqual(string.Empty);

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }

    static IEnumerable<(string Name, string Source, string[] Expected, bool Validate, string[] RegisteredTriggers, string[]? Messages)> Vectors()
    {
        var file = Path.Combine(Root(), "Source", "Screenplay", "Compiler", "Conformance", "diagnostics.json");
        using var document = JsonDocument.Parse(File.ReadAllText(file));
        return [.. document.RootElement.GetProperty("cases").EnumerateArray().Select(vector => (
            vector.GetProperty("name").GetString()!,
            string.Join('\n', vector.GetProperty("source").EnumerateArray().Select(line => line.GetString())),
            vector.GetProperty("diagnostics").EnumerateArray().Select(diagnostic => diagnostic.GetString()!).ToArray(),
            vector.TryGetProperty("validate", out var validate) && validate.GetBoolean(),
            vector.TryGetProperty("registeredTriggers", out var triggers) ? triggers.EnumerateArray().Select(trigger => trigger.GetString()!).ToArray() : [],
            vector.TryGetProperty("messages", out var messages) ? messages.EnumerateArray().Select(message => message.GetString()!).ToArray() : null))];
    }
}
