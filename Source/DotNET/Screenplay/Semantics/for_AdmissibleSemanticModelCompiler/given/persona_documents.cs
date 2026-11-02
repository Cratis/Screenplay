// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using NSubstitute;

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler.given;

public class persona_documents : security_documents
{
    protected const string Personas = "policy Member\n  require authenticated and role \"Member\"\npolicy Extra\n  require claim \"dept\" matches \"Finance\"\npersona Clerk\n  policy Member\n  policy Access\npersona Healthy\n  policy Extra\n  policy Member\n";
    protected readonly List<ApplicationSyntax> _rounds = [];

    protected static Diagnostic PolicyError(string message, SourceLocation location, string policy) =>
        Diagnostic.Error(DiagnosticCodes.InvalidSemanticBinding, message, location, DiagnosticArguments.Empty.With("subject", DiagnosticArgument.Declaration("policy", [policy])));

    protected void Record(Func<ApplicationSyntax, int, CompilationResult<SemanticCompilation>?>? failure = null)
    {
        var binder = Substitute.For<ISemanticModelBinder>();
        binder.Bind(Arg.Any<string>(), Arg.Any<ApplicationSyntax>(), Arg.Any<SemanticDocumentSet>()).Returns(call =>
        {
            var syntax = call.Arg<ApplicationSyntax>();
            _rounds.Add(syntax);
            return failure?.Invoke(syntax, _rounds.Count) ?? new SemanticModelBinder().Bind("Example", syntax, _documents);
        });
        _compiler = new SemanticModelCompiler(new ScreenplayCompiler(), binder);
    }
}
