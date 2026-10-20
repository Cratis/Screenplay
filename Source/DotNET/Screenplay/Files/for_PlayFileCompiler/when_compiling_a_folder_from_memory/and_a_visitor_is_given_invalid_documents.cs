// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_a_folder_from_memory;

public class and_a_visitor_is_given_invalid_documents : given.documents_in_memory
{
    IApplicationSyntaxVisitor<string> _visitor;
    ApplicationCompilation<string> _compilation;

    void Establish()
    {
        _documents = [new("a.play", "concept Id : Uuid"), new("b.play", "concept Id : String")];
        _visitor = Substitute.For<IApplicationSyntaxVisitor<string>>();
    }

    void Because() => _compilation = _compiler.CompileFolder(_documents, _visitor);

    [Fact] void should_fail() => _compilation.Result.Success.ShouldBeFalse();
    [Fact] void should_not_visit_invalid_syntax() => _visitor.DidNotReceive().Visit(Arg.Any<ApplicationSyntax>());
    [Fact] void should_preserve_diagnostics() => _compilation.Result.Diagnostics.Single().Location.Path.ShouldEqual("b.play");
}
