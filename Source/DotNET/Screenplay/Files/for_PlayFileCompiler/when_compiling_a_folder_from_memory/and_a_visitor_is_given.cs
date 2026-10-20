// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_a_folder_from_memory;

public class and_a_visitor_is_given : given.documents_in_memory
{
    IApplicationSyntaxVisitor<string> _visitor;
    ApplicationCompilation<string> _compilation;

    void Establish()
    {
        _visitor = Substitute.For<IApplicationSyntaxVisitor<string>>();
        _visitor.Visit(Arg.Any<ApplicationSyntax>()).Returns("visited");
    }

    void Because() => _compilation = _compiler.CompileFolder(_documents, _visitor);

    [Fact] void should_return_the_visitor_result() => _compilation.Result.Value.ShouldEqual("visited");
    [Fact] void should_visit_the_merged_application() => _visitor.Received(1).Visit(Arg.Is<ApplicationSyntax>(syntax => syntax.EventSources.Count() == 1 && syntax.Modules.Count() == 1));
    [Fact] void should_preserve_sources() => _compilation.Sources.Count().ShouldEqual(2);
}
