// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using NSubstitute;

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_security_attribution_is_uncertain : given.security_documents
{
    [Fact]
    void should_restore_the_original_failure_if_a_later_pass_cannot_be_attributed()
    {
        Source(Unsupported);
        var later = CompilationResult<SemanticCompilation>.Failed([Diagnostic.Error(DiagnosticCodes.InvalidSemanticBinding, "Unattributable policy", new(999, 1, "main.play"), DiagnosticArguments.Empty.With("subject", DiagnosticArgument.Declaration("policy", ["Unresolved"])))]);
        var binder = Substitute.For<ISemanticModelBinder>();
        binder.Bind(Arg.Any<string>(), Arg.Any<ApplicationSyntax>(), Arg.Any<SemanticDocumentSet>()).Returns(_normal, later);
        var result = new SemanticModelCompiler(new ScreenplayCompiler(), binder).CompileAdmissible("Example", _documents);
        result.Compilation.Value.ShouldBeNull();
        result.Compilation.Diagnostics.ShouldEqual(_normal.Diagnostics);
        result.Exclusions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("policy")]
    [InlineData("readmodel")]
    void should_return_the_original_failure_instead_of_a_partial_model(string kind)
    {
        Source(kind == "readmodel" ? Protected : Policy + Authorized);
        var failure = CompilationResult<SemanticCompilation>.Failed([Diagnostic.Error(DiagnosticCodes.InvalidSemanticBinding, "Unresolvable security attribution", new(999, 1, "main.play"), DiagnosticArguments.Empty.With("subject", DiagnosticArgument.Declaration(kind, ["Unresolved"])))]);
        var binder = Substitute.For<ISemanticModelBinder>();
        binder.Bind(Arg.Any<string>(), Arg.Any<ApplicationSyntax>(), Arg.Any<SemanticDocumentSet>()).Returns(failure);
        var compiler = new SemanticModelCompiler(new ScreenplayCompiler(), binder);
        var normal = compiler.Compile("Example", _documents);
        var result = compiler.CompileAdmissible("Example", _documents);
        result.Compilation.Success.ShouldBeFalse();
        result.Compilation.Value.ShouldBeNull();
        result.Compilation.Diagnostics.ShouldEqual(normal.Diagnostics);
        result.Exclusions.ShouldBeEmpty();
    }
}
