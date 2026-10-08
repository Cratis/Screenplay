// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Dependencies.for_DependencyGraph.given;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Dependencies.for_DeclaredDependencies;

public class when_holding_the_compilers_to_declared_dependencies : Specification
{
    (JsonElement Vector, CompilationResult<ApplicationSyntax> Compilation, DeclaredDependencyReport Report)[] _cases;

    void Because()
    {
        using var vectors = JsonDocument.Parse(File.ReadAllText(Path.Combine(a_conformance_suite.Root(), "Source/Screenplay/Compiler/Conformance/declared-dependencies.json")));
        _cases = [.. vectors.RootElement.GetProperty("cases").EnumerateArray().Select(vector =>
        {
            var files = vector.GetProperty("files").EnumerateObject().ToDictionary(file => file.Name, file => file.Value.GetString()!, StringComparer.Ordinal);
            if (vector.TryGetProperty("sample", out var sample))
            {
                var folder = Path.Combine(a_conformance_suite.Root(), "Samples", sample.GetString()!);
                files = Directory.GetFiles(folder, "*.play", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
                    .ToDictionary(path => Path.GetRelativePath(folder, path).Replace('\\', '/'), File.ReadAllText, StringComparer.Ordinal);
                foreach (var insertion in vector.GetProperty("insertions").EnumerateArray())
                {
                    var file = insertion.GetProperty("file").GetString()!;
                    var header = insertion.GetProperty("header").GetString()!;
                    var indent = header.Length - header.TrimStart().Length + 2;
                    var lines = string.Concat(insertion.GetProperty("targets").EnumerateArray().Select(target => new string(' ', indent) + "depends on " + target.GetString() + "\n"));
                    files[file] = files[file].Replace(header + "\n", header + "\n" + lines, StringComparison.Ordinal);
                }
            }
            var compiler = new ScreenplayCompiler();
            var compilation = vector.TryGetProperty("document", out var document) && document.GetBoolean()
                ? compiler.Compile(files["application.play"])
                : PlayApplicationAssembly.Compile(compiler, files.Keys, new InMemoryPlayDocumentSource(files), compiler.Languages, out _).Result;

            return (vector.Clone(), compilation, DeclaredDependencies.For(compilation.Value!));
        })];
    }

    [Fact]
    void should_match_findings_and_locations()
    {
        foreach (var (vector, compilation, _) in _cases)
        {
            var actual = compilation.Diagnostics.Where(diagnostic => new[] { "PLAY0022", "PLAY0198", "PLAY0552", "PLAY0553", "PLAY0554", "PLAY0555", "PLAY0556" }.Contains(diagnostic.Code, StringComparer.Ordinal))
                .Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Path ?? "application.play"}:{diagnostic.Location.Line}").ToArray();
            Assert.True(actual.SequenceEqual(vector.GetProperty("diagnostics").EnumerateArray().Select(item => item.GetString())), $"{vector.GetProperty("name").GetString()}: {string.Join(", ", actual)}");
        }
    }

    [Fact]
    void should_match_declaration_and_per_evidence_coverage()
    {
        foreach (var (vector, _, report) in _cases)
        {
            var actual = report.Containers.SelectMany(item => item.Declarations.Select(declaration => $"{item.Container.Address}|{declaration.Syntax.Target}|{declaration.Resolved ?? string.Empty}|{JsonNamingPolicy.CamelCase.ConvertName(declaration.Status.ToString())}")
                .Concat((vector.TryGetProperty("sample", out _) ? [] : item.Edges).Select(edge => $"{item.Container.Address}|{edge.Evidence.Consumer.Address}|{edge.Evidence.Producer.Address}|{edge.Evidence.Kind}|{JsonNamingPolicy.CamelCase.ConvertName(edge.Status.ToString())}|{string.Join(',', edge.CoveringDeclarations.Select(declaration => declaration.Target))}"))).ToArray();
            Assert.True(actual.SequenceEqual(vector.GetProperty("report").EnumerateArray().Select(item => item.GetString())), $"{vector.GetProperty("name").GetString()}: {string.Join(", ", actual)}");
        }
    }

    [Fact]
    void should_name_uncovered_producers_with_source_evidence()
    {
        foreach (var (vector, _, report) in _cases.Where(item => item.Vector.TryGetProperty("uncovered", out _)))
        {
            var findings = report.Diagnostics.Where(item => item.Code == DiagnosticCodes.UndeclaredDependency).ToArray();
            findings.Length.ShouldEqual(vector.GetProperty("uncovered").GetArrayLength());
            foreach (var target in vector.GetProperty("uncovered").EnumerateArray()) findings.Any(item => item.Message.Contains($"depends on '{target.GetString()}'", StringComparison.Ordinal)).ShouldBeTrue();
            if (!vector.TryGetProperty("evidence", out var evidence)) continue;
            foreach (var fragment in evidence.EnumerateArray()) findings.Any(item => item.Message.Contains(fragment.GetString()!, StringComparison.Ordinal)).ShouldBeTrue();
        }
    }

    [Fact]
    void should_use_the_agreed_severities()
    {
        foreach (var diagnostic in _cases.SelectMany(item => item.Report.Diagnostics))
        {
            diagnostic.Severity.ShouldEqual(diagnostic.Code == DiagnosticCodes.UndeclaredDependency ? DiagnosticSeverity.Warning : DiagnosticSeverity.Information);
        }
    }
}
