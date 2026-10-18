// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

// The TypeScript compiler (Source/Screenplay/Compiler) is held to a set of invalid documents and the
// diagnostics they are reported with, in Conformance/diagnostics.json. This holds the C# compiler to the
// same file, so the two report the same codes on the same lines - an error path cannot drift on one side.
public class when_holding_the_typescript_compiler_to_its_diagnostics : Specification
{
    static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);
    readonly List<string> _mismatches = [];
    List<(string Name, string Source, string[] Expected, bool Validate, string[] RegisteredTriggers, string[]? Messages, DiagnosticArguments[]? Arguments, bool Bind, Dictionary<string, string[]>? Files, string[]? Roots, string[]? Locations)> _vectors;

    void Establish() => _vectors = [.. Vectors()];

    void Because()
    {
        foreach (var (name, source, expected, validate, registeredTriggers, messages, arguments, bind, files, roots, locations) in _vectors)
        {
            var compiler = new ScreenplayCompiler(new ScreenplayLanguageRegistry(triggers: registeredTriggers.Select(triggerName => new TriggerDefinition(triggerName))));
            CompilationResult<ApplicationSyntax> result;
            if (files is null)
            {
                result = validate ? compiler.Compile(source) : compiler.Parse(source);
            }
            else
            {
                result = PlayApplicationAssembly.Compile(
                    compiler,
                    roots ?? [.. files.Keys.Order(StringComparer.Ordinal)],
                    new InMemoryPlayDocumentSource(files.ToDictionary(file => file.Key, file => string.Join('\n', file.Value), StringComparer.Ordinal))).Result;
            }
            if (bind) result = result with { Diagnostics = [.. result.Diagnostics, .. BinderDiagnostics(result.Value!, source)] };
            var reported = result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}").ToArray();
            if (!reported.SequenceEqual(expected, StringComparer.Ordinal))
            {
                _mismatches.Add($"{name}: expected [{string.Join(", ", expected)}], C# reports [{string.Join(", ", reported)}]");
            }

            if (locations is not null && !result.Diagnostics.Select(diagnostic => $"{diagnostic.Location.Path}:{diagnostic.Location.Line}:{diagnostic.Location.Column}").SequenceEqual(locations, StringComparer.Ordinal))
            {
                _mismatches.Add($"{name}: diagnostic locations differ");
            }

            if (arguments is not null && !result.Diagnostics.Select(diagnostic => diagnostic.Arguments).SequenceEqual(arguments))
            {
                _mismatches.Add($"{name}: diagnostic arguments differ: {JsonSerializer.Serialize(result.Diagnostics.Select(diagnostic => diagnostic.Arguments), _options)}");
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

    // The TypeScript compiler reports the match-pattern codes while parsing a validation rule; the C# compiler
    // reports them in the binder. A vector marked "bind" is run through the binder here so both compilers are held
    // to the same codes, lines and arguments without moving either phase.
    static IEnumerable<Diagnostic> BinderDiagnostics(ApplicationSyntax syntax, string source)
    {
        var identity = ApplicationIdentity.Create("Conformance");
        var catalog = SemanticIdentityCatalog.Empty(identity);
        const string StableKey = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(StableKey), StableKey, "application.play", source);
        return new SemanticModelBinder().Bind("Conformance", syntax, SemanticDocumentSet.Create([document], catalog)).Diagnostics;
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }

    static IEnumerable<(string Name, string Source, string[] Expected, bool Validate, string[] RegisteredTriggers, string[]? Messages, DiagnosticArguments[]? Arguments, bool Bind, Dictionary<string, string[]>? Files, string[]? Roots, string[]? Locations)> Vectors()
    {
        var file = Path.Combine(Root(), "Source", "Screenplay", "Compiler", "Conformance", "diagnostics.json");
        using var document = JsonDocument.Parse(File.ReadAllText(file));
        return [.. document.RootElement.GetProperty("cases").EnumerateArray().Select(vector => (
            vector.GetProperty("name").GetString()!,
            string.Join('\n', vector.GetProperty("source").EnumerateArray().Select(line => line.GetString())),
            vector.GetProperty("diagnostics").EnumerateArray().Select(diagnostic => diagnostic.GetString()!).ToArray(),
            vector.TryGetProperty("validate", out var validate) && validate.GetBoolean(),
            vector.TryGetProperty("registeredTriggers", out var triggers) ? triggers.EnumerateArray().Select(trigger => trigger.GetString()!).ToArray() : [],
            vector.TryGetProperty("messages", out var messages) ? messages.EnumerateArray().Select(message => message.GetString()!).ToArray() : null,
            vector.TryGetProperty("arguments", out var arguments) ? JsonSerializer.Deserialize<DiagnosticArguments[]>(arguments.GetRawText(), _options) : null,
            vector.TryGetProperty("bind", out var bind) && bind.GetBoolean(),
            vector.TryGetProperty("files", out var files) ? JsonSerializer.Deserialize<Dictionary<string, string[]>>(files.GetRawText(), _options) : null,
            vector.TryGetProperty("roots", out var roots) ? roots.EnumerateArray().Select(root => root.GetString()!).ToArray() : null,
            vector.TryGetProperty("locations", out var locations) ? locations.EnumerateArray().Select(location => location.GetString()!).ToArray() : null))];
    }
}
