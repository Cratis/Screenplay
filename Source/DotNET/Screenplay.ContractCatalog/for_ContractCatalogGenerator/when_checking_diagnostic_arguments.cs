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

    static GeneratorDriverRunResult Generate(string emission, SyntaxTree? fixture = null)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
        var source = CSharpSyntaxTree.ParseText(Catalog.Replace("EMISSION", emission, StringComparison.Ordinal));
        var compilation = CSharpCompilation.Create("ArgumentProbe", fixture is null ? [source] : [source, fixture], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ContractCatalogGenerator());
        return driver.RunGenerators(compilation).GetRunResult();
    }
}
