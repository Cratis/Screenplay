// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Files.for_PlayImports;

public class when_compiling_imported_example_types : Specification
{
    const string Declarations =
        """
        module Billing
          feature Facts
            slice StateChange Recording
              command Record
                id String identifier
                amount Int
                produces Recorded
                  for id
                  amount = amount
              event Recorded
                amount Int
              readmodel Balance
                balanceId String
                amount Int
              query BalanceById => Balance?
                by balanceId String
              specification Recording
                given Fact
                given readmodel View
                when Input
                then Fact amount = 20
                then readmodel View
        """;

    const string Importing =
        """
        import Billing.Facts.Recording.Recorded
        import Billing.Facts.Recording.Balance
        import Billing.Facts.Recording.Record
        import "declarations.play"
        example Fact : Recorded
          amount = 10
          for "one"
        example View : Balance
          balanceId = "one"
          amount = 10
        example Input : Record
          id = "one"
          amount = 20
        """;

    CompilationResult<ApplicationSyntax> _single;
    CompilationResult<ApplicationSyntax> _parsed;
    CompilationResult<ApplicationSyntax> _assembled;
    EffectiveSpecificationApplication _expanded;
    ScreenplayWorkspace _workspace;
    CompilationResult<SemanticCompilation> _semantic;
    CompilationResult<ApplicationSyntax> _unresolved;
    ScreenplayWorkspace _unresolvedWorkspace;
    CompilationResult<ApplicationSyntax> _invalidValues;
    ScreenplayWorkspace _fileImportWorkspace;
    CompilationResult<SemanticCompilation> _fileImportSemantic;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _single = compiler.Compile(Importing);
        _parsed = compiler.Parse(Importing, "application.play");
        (_, _assembled) = PlayApplicationAssembly.Compile(compiler, ["application.play"], new InMemoryPlayDocumentSource(new Dictionary<string, string>
        {
            ["application.play"] = Importing,
            ["declarations.play"] = Declarations
        }));
        _expanded = SpecificationExamples.Expand(_assembled.Value!);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        _workspace = ScreenplayWorkspace.Create("Billing",
        [
            WorkspaceDocument.Create("application", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(Importing)),
            WorkspaceDocument.Create("declarations", PortablePlayPath.Parse("declarations.play"), Encoding.UTF8.GetBytes(Declarations))
        ], catalog);
        _semantic = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create(
        [
            SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", Importing),
            SemanticSourceDocument.Create(catalog.ResolveDocument("declarations"), "declarations", "declarations.play", Declarations)
        ], catalog));
        // Named imports remain syntax-only in ESM v1. File imports assemble the same declarations
        // without that admission restriction; fully qualified example types still resolve across documents.
        var fileImporting = Importing
            .Replace("import Billing.Facts.Recording.Recorded\n", string.Empty, StringComparison.Ordinal)
            .Replace("import Billing.Facts.Recording.Balance\n", string.Empty, StringComparison.Ordinal)
            .Replace("import Billing.Facts.Recording.Record\n", string.Empty, StringComparison.Ordinal)
            .Replace(" : Recorded", " : Billing.Facts.Recording.Recorded", StringComparison.Ordinal)
            .Replace(" : Balance", " : Billing.Facts.Recording.Balance", StringComparison.Ordinal)
            .Replace(" : Record\n", " : Billing.Facts.Recording.Record\n", StringComparison.Ordinal);
        _fileImportWorkspace = ScreenplayWorkspace.Create("Billing",
        [
            WorkspaceDocument.Create("application", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(fileImporting)),
            WorkspaceDocument.Create("declarations", PortablePlayPath.Parse("declarations.play"), Encoding.UTF8.GetBytes(Declarations))
        ], catalog);
        _fileImportSemantic = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create(
        [
            SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", fileImporting),
            SemanticSourceDocument.Create(catalog.ResolveDocument("declarations"), "declarations", "declarations.play", Declarations)
        ], catalog));
        var unresolved = Importing.Replace("import \"declarations.play\"\n", string.Empty, StringComparison.Ordinal);
        (_, _unresolved) = PlayApplicationAssembly.Compile(compiler, ["application.play"], new InMemoryPlayDocumentSource(new Dictionary<string, string>
        {
            ["application.play"] = unresolved
        }));
        _unresolvedWorkspace = ScreenplayWorkspace.Create("Billing",
        [
            WorkspaceDocument.Create("application", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(unresolved))
        ], catalog);
        (_, _invalidValues) = PlayApplicationAssembly.Compile(compiler, ["application.play"], new InMemoryPlayDocumentSource(new Dictionary<string, string>
        {
            ["application.play"] = Importing.Replace("amount = 10", "missing = 10", StringComparison.Ordinal),
            ["declarations.play"] = Declarations
        }));
    }

    [Fact] void should_defer_imported_types_in_single_document_validation() => _single.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedSpecificationExampleType).ShouldBeEmpty();
    [Fact] void should_parse_the_importing_document_without_resolution_errors() => _parsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_compile_the_assembled_application_without_resolution_errors()
    {
        _assembled.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        _assembled.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedSpecificationExampleType).ShouldBeEmpty();
    }
    [Fact] void should_expand_the_imported_event() => _expanded.Specifications.Single().Effective.Given.Single().EventType.ShouldEqual("Billing.Facts.Recording.Recorded");
    [Fact] void should_expand_the_imported_read_model() => _expanded.Specifications.Single().Effective.GivenReadModels!.Single().Name.ShouldEqual("Billing.Facts.Recording.Balance");
    [Fact] void should_expand_the_imported_command() => _expanded.Specifications.Single().Effective.When!.CommandType.ShouldEqual("Billing.Facts.Recording.Record");
    [Fact] void should_open_the_workspace_without_example_resolution_errors() => _workspace.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedSpecificationExampleType).ShouldBeEmpty();
    [Fact] void should_keep_named_import_admission_separate_from_example_resolution() => _workspace.Compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnsupportedSemanticSyntax, DiagnosticCodes.UnsupportedSemanticSyntax, DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_resolve_example_types_through_the_public_semantic_api() => _semantic.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedSpecificationExampleType).ShouldBeEmpty();
    [Fact] void should_keep_named_import_admission_in_the_public_semantic_api() => _semantic.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnsupportedSemanticSyntax, DiagnosticCodes.UnsupportedSemanticSyntax, DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_compile_the_file_import_workspace() => Assert.True(_fileImportWorkspace.Compilation.Success, string.Join("; ", _fileImportWorkspace.Compilation.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    [Fact] void should_compile_file_imports_through_the_public_semantic_api() => Assert.True(_fileImportSemantic.Success, string.Join("; ", _fileImportSemantic.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    [Fact] void should_report_unresolved_example_types_after_assembly() => _unresolved.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedSpecificationExampleType).Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(4, 7, 10);
    [Fact] void should_report_unresolved_example_types_in_a_workspace() => _unresolvedWorkspace.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedSpecificationExampleType).Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(4, 7, 10);
    [Fact] void should_check_imported_example_values_after_assembly() => _invalidValues.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSpecificationExampleValue);

    [Theory]
    [InlineData("Recorded")]
    [InlineData("Billing.Facts.Recording.Recorded")]
    void should_defer_a_bare_or_qualified_import(string type) => new ScreenplayCompiler().Compile($"import Billing.Facts.Recording.Recorded\nexample Fact : {type}").Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedSpecificationExampleType).ShouldBeEmpty();

    [Theory]
    [InlineData("example Fact : Missing")]
    [InlineData("import Billing.Facts.Recording.Missing\nexample Fact : Missing\nmodule M\n  feature F\n    slice StateChange A\n      event Missing\n    slice StateChange B\n      event Missing")]
    [InlineData("import Billing.Facts.Recording.Shape\nconcept Shape : String\nexample Fact : Shape")]
    [InlineData("import A.F.S.Missing\nimport B.F.S.Missing\nexample Fact : Missing")]
    void should_not_defer_unknown_ambiguous_or_wrong_kind_types(string source) => new ScreenplayCompiler().Compile(source).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedSpecificationExampleType);
}
