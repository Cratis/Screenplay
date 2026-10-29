// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Files.for_AttachmentFiles;

public class when_loading_imperative_validation_files : Specification
{
    const string Body = "if (context.Artifact.Value < 0) yield return \"Must be positive\";";
    string _root;

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), "screenplay-validation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "Validations"));
        File.WriteAllText(Path.Combine(_root, "Validations", "Positive.cs"), Body);
    }

    [Theory]
    [InlineData(true, SemanticImplementationRole.ConceptValidation)]
    [InlineData(false, SemanticImplementationRole.CommandValidation)]
    void should_bind_the_same_opaque_body_as_inline_validation(bool concept, SemanticImplementationRole role)
    {
        var file = Bind(Source(concept, "file Validations/Positive.cs"));
        var inline = Bind(Source(concept, $"```csharp\n{Indent(concept)}{Body}\n{Indent(concept)}```"));
        var requirement = file.Result.ImplementationRequirements.Single();
        var inlineRequirement = inline.Result.ImplementationRequirements.Single();
        file.Result.Success.ShouldBeTrue();
        requirement.Role.ShouldEqual(role);
        requirement.Language.ShouldBeNull();
        requirement.ContentHash.ShouldEqual(inlineRequirement.ContentHash);
        requirement.RequirementId.ShouldEqual(inlineRequirement.RequirementId);
        requirement.RequiredCapability.ShouldEqual("pure");
        requirement.AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.Resolved);
        requirement.File.ShouldEqual("Validations/Positive.cs");
        requirement.BodySpan!.Value.End.ShouldEqual(Body.Length);
        file.Contents["Validations/Positive.cs"].ShouldEqual(Body);
        file.Diagnostics.ShouldBeEmpty();
        Validation(file.Syntax, concept).ShouldBeOfExactType<FileValidateSyntax>();
        ((FileValidateSyntax)Validation(file.Syntax, concept)).File.Path.ShouldEqual("Validations/Positive.cs");
        ((CodeValidateSyntax)Validation(inline.Syntax, concept)).Code.ShouldNotBeNull();
        Rule(file.Result, concept).ShouldEqual(Rule(inline.Result, concept));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_round_trip_the_file_form(bool concept)
    {
        var bound = Bind(Source(concept, "file Validations/Positive.cs"));
        var revived = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(bound.Syntax));
        Validation(revived, concept).ShouldBeOfExactType<FileValidateSyntax>();
        var printed = new ScreenplayPrinter().Print(revived);
        var reparsed = new ScreenplayCompiler().Compile(printed);
        reparsed.Success.ShouldBeTrue();
        ((FileValidateSyntax)Validation(reparsed.Value!, concept)).File.Path.ShouldEqual("Validations/Positive.cs");
    }

    [Theory]
    [InlineData(true, "missing.cs", DiagnosticCodes.AttachmentMissing)]
    [InlineData(false, "missing.cs", DiagnosticCodes.AttachmentMissing)]
    [InlineData(true, "../outside.cs", DiagnosticCodes.AttachmentPathRefused)]
    [InlineData(false, "../outside.cs", DiagnosticCodes.AttachmentPathRefused)]
    [InlineData(true, "/outside.cs", DiagnosticCodes.AttachmentPathRefused)]
    [InlineData(false, "/outside.cs", DiagnosticCodes.AttachmentPathRefused)]
    void should_use_the_existing_file_diagnostic(bool concept, string path, string code)
    {
        var bound = Bind(Source(concept, $"file {path}"));
        bound.Result.ImplementationRequirements.Single().AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.UnresolvedFile);
        bound.Result.ImplementationRequirements.Single().ContentHash.ShouldBeEmpty();
        bound.Diagnostics.Single(diagnostic => diagnostic.Code == code).Severity.ShouldEqual(DiagnosticSeverity.Warning);
    }

    static object Rule(CompilationResult<SemanticCompilation> result, bool concept) => concept
        ? result.Value!.Model.Application.Concepts.Single().Validations.Single()
        : result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().CodeValidations.Single();

    static ValidateSyntax Validation(ApplicationSyntax syntax, bool concept) => concept
        ? syntax.Concepts.Single().Validations.Single()
        : syntax.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single();

    (CompilationResult<SemanticCompilation> Result, ApplicationSyntax Syntax, ImmutableDictionary<string, string> Contents, ImmutableArray<Diagnostic> Diagnostics) Bind(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "application.play", source);
        var loaded = AttachmentFiles.Load(_root, [document]);
        var parsed = new ScreenplayCompiler().Parse(source, document.DisplayPath);
        parsed.Success.ShouldBeTrue();
        var bound = new SemanticModelBinder().Bind("Projects", parsed.Value!, SemanticDocumentSet.Create([document], catalog, loaded.Contents));
        return (bound, parsed.Value!, loaded.Contents, loaded.Diagnostics);
    }

    static string Indent(bool concept) => concept ? "    " : "          ";

    static string Source(bool concept, string body) => concept
        ? $"concept Amount : Decimal\n  validate\n    {body}\nmodule Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        amount Amount"
        : $"module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        amount Decimal\n        validate\n          {body}";

    [Fact]
    void should_keep_the_inline_code_property_non_nullable() =>
        new NullabilityInfoContext().Create(typeof(CodeValidateSyntax).GetProperty(nameof(CodeValidateSyntax.Code))!).ReadState.ShouldEqual(NullabilityState.NotNull);

    [Fact]
    void should_keep_the_inline_constructor_parameter_non_nullable() =>
        new NullabilityInfoContext().Create(typeof(CodeValidateSyntax).GetConstructors().Single().GetParameters()[0]).ReadState.ShouldEqual(NullabilityState.NotNull);

    void Destroy() => Directory.Delete(_root, true);
}
