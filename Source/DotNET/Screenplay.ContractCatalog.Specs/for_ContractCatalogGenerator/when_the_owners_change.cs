// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Cratis.Screenplay.ContractCatalog.for_ContractCatalogGenerator;

public class when_the_owners_change
{
    const string Catalog = """
        namespace Cratis.Screenplay.Diagnostics
        {
            public enum DiagnosticSeverity { Information, Warning, Error }
            public sealed class DiagnosticReservationAttribute(DiagnosticSeverity severity, bool retired = false) : System.Attribute {}
            public static class DiagnosticCodes
            {
                /// <summary>A reserved diagnostic.</summary>
                [DiagnosticReservation(DiagnosticSeverity.Error)]
                public const string Reserved = "PLAY0001";
                /// <summary>A live diagnostic.</summary>
                public const string Live = "PLAY0002";
            }
            public static class Diagnostic
            {
                public static void Warning(string code) {}
            }
        }
        namespace Cratis.Screenplay.Parsing
        {
            public static partial class ScreenplayParser
            {
                public static void Parse(string word)
                {
                    if (word == "newKeyword") Helper(Cratis.Screenplay.Diagnostics.DiagnosticCodes.Live);
                }
                [System.Text.RegularExpressions.GeneratedRegex(@"^require\s+(?<operand>not\s+empty)$")]
                public static partial System.Text.RegularExpressions.Regex Grammar();
                static void Helper(string code) => Cratis.Screenplay.Diagnostics.Diagnostic.Warning(code);
            }
        }
        """;

    [Fact]
    public void should_derive_a_new_if_dispatched_keyword_and_indirect_warning()
    {
        var result = Generate(Catalog);
        Assert.Empty(result.Diagnostics);
        Assert.Contains("\"newKeyword\"", result.GeneratedTrees.Single().GetText(TestContext.Current.CancellationToken).ToString(), StringComparison.Ordinal);
        Assert.Contains("\"empty\"", result.GeneratedTrees.Single().GetText(TestContext.Current.CancellationToken).ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("\"operand\"", result.GeneratedTrees.Single().GetText(TestContext.Current.CancellationToken).ToString(), StringComparison.Ordinal);
        Assert.Contains("\"Live\", \"A live diagnostic.\", new Cratis.Screenplay.Diagnostics.DiagnosticSeverity[] {(Cratis.Screenplay.Diagnostics.DiagnosticSeverity)1}", result.GeneratedTrees.Single().GetText(TestContext.Current.CancellationToken).ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void should_fail_when_a_reserved_code_starts_emitting_with_another_severity()
    {
        var result = Generate(Catalog.Replace("DiagnosticCodes.Live", "DiagnosticCodes.Reserved", StringComparison.Ordinal));
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "SPCON001");
    }

    [Theory]
    [InlineData("Source/for_Compiler/when_testing.cs")]
    [InlineData("Source\\for_Compiler\\when_testing.cs")]
    [InlineData("Source/obj/Generated.cs")]
    [InlineData("Source\\obj\\Generated.cs")]
    public void should_exclude_specifications_and_build_outputs_on_every_platform(string path)
    {
        var fixture = CSharpSyntaxTree.ParseText("class Fixture { void Test() => Cratis.Screenplay.Diagnostics.Diagnostic.Warning(Cratis.Screenplay.Diagnostics.DiagnosticCodes.Reserved); }", path: path, cancellationToken: TestContext.Current.CancellationToken);
        var result = Generate(Catalog, fixture);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(Generate(Catalog).GeneratedTrees.Single().GetText(TestContext.Current.CancellationToken).ToString(), result.GeneratedTrees.Single().GetText(TestContext.Current.CancellationToken).ToString());
    }

    [Theory]
    [InlineData("static readonly RootRule[]", "new(\"rootEntry\", \"otherArgument\")", true)]
    [InlineData("static readonly RootRule[]", "new RootRule(\"rootEntry\", \"otherArgument\")", true)]
    [InlineData("readonly RootRule[]", "new(\"rootEntry\", \"otherArgument\")", false)]
    [InlineData("static RootRule[]", "new(\"rootEntry\", \"otherArgument\")", false)]
    [InlineData("static readonly Other.RootRule[]", "new(\"rootEntry\", \"otherArgument\")", false)]
    public void should_classify_only_root_rule_table_keywords_as_constructs(string declaration, string entry, bool isRoot)
    {
        var source = $$"""
            namespace Other { public record RootRule(string Keyword, string Other); }
            namespace Cratis.Screenplay.Parsing
            {
                public record RootRule(string Keyword, string Other);
                public class Tables
                {
                    {{declaration}} _rules = [{{entry}}];
                }
            }
            """;
        var result = Generate(Catalog, CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Empty(result.Diagnostics);
        var output = result.GeneratedTrees.Single().GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Equal(isRoot, Words(output, "Constructs").Contains("\"rootEntry\"", StringComparison.Ordinal));
        Assert.Equal(isRoot, Words(output, "TopLevelConstructs").Contains("\"rootEntry\"", StringComparison.Ordinal));
        Assert.DoesNotContain("\"otherArgument\"", Words(output, "Constructs"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("new([\"commandEntry\", \"anotherEntry\"], \"otherArgument\")")]
    [InlineData("new CommandDirectiveRule(new[] { \"commandEntry\", \"anotherEntry\" }, \"otherArgument\")")]
    public void should_keep_command_directives_as_keywords_and_root_clauses_out_of_constructs(string entry)
    {
        var source = $$"""
            namespace Cratis.Screenplay.Parsing
            {
                public record RootRule(string Keyword);
                public record CommandDirectiveRule(string[] Keywords, string Other);
                public static class Tables
                {
                    static readonly RootRule[] _roots = [new("description")];
                    static readonly CommandDirectiveRule[] _commands = [{{entry}}];
                }
            }
            """;
        var result = Generate(Catalog, CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Empty(result.Diagnostics);
        var output = result.GeneratedTrees.Single().GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("\"commandEntry\"", Words(output, "Keywords"), StringComparison.Ordinal);
        Assert.Contains("\"anotherEntry\"", Words(output, "Keywords"), StringComparison.Ordinal);
        Assert.Contains("\"description\"", Words(output, "TopLevelConstructs"), StringComparison.Ordinal);
        Assert.Equal("public static System.Collections.Generic.IReadOnlyList<string> Constructs { get; } = new string[] {\"newKeyword\"};", Words(output, "Constructs"));
        Assert.DoesNotContain("\"commandEntry\"", Words(output, "TopLevelConstructs"), StringComparison.Ordinal);
    }

    static string Words(string output, string name) => output.Split('\n').Single(line => line.Contains(" " + name + " {", StringComparison.Ordinal));

    static GeneratorDriverRunResult Generate(string source, SyntaxTree? fixture = null)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("ContractProbe", fixture is null ? [CSharpSyntaxTree.ParseText(source)] : [CSharpSyntaxTree.ParseText(source), fixture], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ContractCatalogGenerator());
        return driver.RunGenerators(compilation).GetRunResult();
    }
}
