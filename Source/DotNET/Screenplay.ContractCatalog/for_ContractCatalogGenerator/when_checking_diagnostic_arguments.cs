// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Cratis.Screenplay.ContractCatalog.for_ContractCatalogGenerator;

public class when_checking_diagnostic_arguments
{
    const string Catalog = """
        namespace Cratis.Screenplay.Diagnostics
        {
            public enum DiagnosticSeverity { Information, Warning, Error }
            public sealed class DiagnosticArgumentDefinitionsAttribute(params string[] definitions) : System.Attribute {}
            public static class DiagnosticCodes
            {
                /// <summary>A diagnostic with subjects.</summary>
                [DiagnosticArgumentDefinitions("subject:text;The authored subject.", "role:keyword?=given|when;The step role.")]
                public const string Structured = "PLAY0001";
                /// <summary>A diagnostic without subjects.</summary>
                public const string Plain = "PLAY0002";
            }
            public static class Diagnostic
            {
                public static void Error(string code, string message, object location, object arguments = null) {}
                public static void Warning(string code, string message, object location, object arguments = null) {}
                public static void Information(string code, string message, object location, object arguments = null) {}
            }
        }
        namespace Cratis.Screenplay.Parsing
        {
            public class ParserContext
            {
                public void Error(string code, string message, object location, object arguments = null) {}
                public void Warning(string code, string message, object location, object arguments = null) {}
                public void Information(string code, string message, object location, object arguments = null) {}
            }
            public static class Reporter
            {
                public static void Report()
                {
                    Cratis.Screenplay.Diagnostics.Diagnostic.Error(Cratis.Screenplay.Diagnostics.DiagnosticCodes.Plain, "plain", null);
                    EMISSION;
                }
            }
        }
        """;

    [Theory]
    [InlineData("Cratis.Screenplay.Diagnostics.Diagnostic", "Error")]
    [InlineData("Cratis.Screenplay.Diagnostics.Diagnostic", "Warning")]
    [InlineData("Cratis.Screenplay.Diagnostics.Diagnostic", "Information")]
    [InlineData("new ParserContext()", "Error")]
    [InlineData("new ParserContext()", "Warning")]
    [InlineData("new ParserContext()", "Information")]
    public void should_report_a_direct_emission_without_its_subjects(string sink, string severity)
    {
        var emission = $"{sink}.{severity}(Cratis.Screenplay.Diagnostics.DiagnosticCodes.Structured, \"message\", null)";
        var result = Generate(emission);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("SPCON003", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Structured", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Equal(emission, diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
    }

    [Theory]
    [InlineData("Cratis.Screenplay.Diagnostics.Diagnostic", "Error")]
    [InlineData("Cratis.Screenplay.Diagnostics.Diagnostic", "Warning")]
    [InlineData("Cratis.Screenplay.Diagnostics.Diagnostic", "Information")]
    [InlineData("new ParserContext()", "Error")]
    public void should_accept_a_direct_emission_with_subjects(string sink, string severity)
    {
        var result = Generate($"{sink}.{severity}(Cratis.Screenplay.Diagnostics.DiagnosticCodes.Structured, \"message\", null, new object())");
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void should_publish_the_declared_meaning_optionality_and_keyword_set()
    {
        var result = Generate("Cratis.Screenplay.Diagnostics.Diagnostic.Error(Cratis.Screenplay.Diagnostics.DiagnosticCodes.Structured, \"message\", null, new object())");
        var generated = result.GeneratedTrees.Single().GetText().ToString();
        Assert.Contains("new(\"subject\",\"text\",false,\"The authored subject.\", new string[] {})", generated, StringComparison.Ordinal);
        Assert.Contains("new(\"role\",\"keyword\",true,\"The step role.\", new string[] {\"given\",\"when\"})", generated, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Source/for_Reporter/when_reporting.cs")]
    [InlineData("Source\\for_Reporter\\when_reporting.cs")]
    [InlineData("Source/obj/Generated.cs")]
    [InlineData("Source\\obj\\Generated.cs")]
    public void should_not_require_subjects_in_specifications_or_build_outputs(string path)
    {
        const string emission = "Cratis.Screenplay.Diagnostics.Diagnostic.Error(Cratis.Screenplay.Diagnostics.DiagnosticCodes.Structured, \"message\", null, new object())";
        var fixture = CSharpSyntaxTree.ParseText("class Fixture { void Report() => Cratis.Screenplay.Diagnostics.Diagnostic.Error(Cratis.Screenplay.Diagnostics.DiagnosticCodes.Structured, \"message\", null); }", path: path);
        Assert.Empty(Generate(emission, fixture).Diagnostics);
    }

    [Theory]
    [InlineData("subject:unknown;The subject.", "unknown argument kind 'unknown'")]
    [InlineData(":text;The subject.", "nonempty identifier")]
    [InlineData("subject", "expected name:kind")]
    [InlineData("subject;text", "expected a name")]
    [InlineData("subject:text??;The subject.", "unknown argument kind")]
    [InlineData("subject:keyword;The subject.", "nonempty set of values")]
    [InlineData("subject:keyword=given|;The subject.", "nonempty set of values")]
    [InlineData("subject:keyword=given|given;The subject.", "unique")]
    [InlineData("subject:text=given;The subject.", "only keyword")]
    [InlineData("subject:text;", "meaning must not be empty")]
    public void should_report_a_clear_error_for_a_malformed_declaration(string declaration, string reason)
    {
        var result = GenerateWithDefinitions(declaration);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("SPCON004", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Structured", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains(declaration, diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains(reason, diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("Structured", diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan), StringComparison.Ordinal);
    }

    [Fact]
    public void should_reject_duplicate_argument_names()
    {
        var diagnostic = Assert.Single(GenerateWithDefinitions("subject:text;First.", "subject:index;Second.").Diagnostics);
        Assert.Equal("SPCON004", diagnostic.Id);
        Assert.Contains("duplicate argument name 'subject'", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("A meaning; with another clause.")]
    [InlineData("A meaning = the authored value.")]
    [InlineData("A meaning; with a = value.")]
    public void should_preserve_delimiters_in_the_meaning(string meaning)
    {
        var result = GenerateWithDefinitions($"subject:text;{meaning}");
        Assert.Empty(result.Diagnostics);
        Assert.Contains($"new(\"subject\",\"text\",false,{SymbolDisplay.FormatLiteral(meaning, true)}, new string[] {{}})", result.GeneratedTrees.Single().GetText().ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void should_accept_language_keywords_as_names_and_multiword_keyword_values()
    {
        var result = GenerateWithDefinitions("case:keyword?=given readmodel|when append;The step.");
        Assert.Empty(result.Diagnostics);
        Assert.Contains("new(\"case\",\"keyword\",true,\"The step.\", new string[] {\"given readmodel\",\"when append\"})", result.GeneratedTrees.Single().GetText().ToString(), StringComparison.Ordinal);
    }

    static GeneratorDriverRunResult GenerateWithDefinitions(params string[] definitions) => Generate(
        "Cratis.Screenplay.Diagnostics.Diagnostic.Error(Cratis.Screenplay.Diagnostics.DiagnosticCodes.Structured, \"message\", null, new object())", definitions: definitions);

    static GeneratorDriverRunResult Generate(string emission, SyntaxTree? fixture = null, string[]? definitions = null)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
        var catalog = Catalog.Replace("EMISSION", emission, StringComparison.Ordinal);
        if (definitions is not null) catalog = catalog.Replace("\"subject:text;The authored subject.\", \"role:keyword?=given|when;The step role.\"", string.Join(", ", definitions.Select(definition => SymbolDisplay.FormatLiteral(definition, true))), StringComparison.Ordinal);
        var source = CSharpSyntaxTree.ParseText(catalog);
        var compilation = CSharpCompilation.Create("ArgumentProbe", fixture is null ? [source] : [source, fixture], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ContractCatalogGenerator());
        return driver.RunGenerators(compilation).GetRunResult();
    }
}
