// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_an_attached_command_handler;

public class and_its_shape_is_not_admitted : given.a_semantic_binder
{
    [Theory]
    [InlineData("        produces Chosen\n        handler\n          file Missing.cs\n")]
    [InlineData("        handler\n          ```csharp\n          return null;\n          ```\n")]
    [InlineData("        produces Chosen when true\n        handler\n          ```csharp\n          return null;\n          ```\n")]
    [InlineData("        produces Chosen\n        handler\n          ```csharp\n          \n          ```\n")]
    void should_refuse_an_unconstrained_body(string body)
    {
        var result = Bind(and_the_inline_body_is_constrained.Prefix + body);
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.CommandHandlerRequiresAttachment);
    }

    [Theory]
    [InlineData("          ```csharp\n          return null;\n          ```\n")]
    [InlineData("          file First.cs\n")]
    void should_refuse_multiple_attached_handlers(string first)
    {
        var source = and_the_inline_body_is_constrained.Prefix + "        produces Chosen\n        handler\n" + first + "        handler\n          ```csharp\n          return new Chosen(\"second\");\n          ```\n";
        var result = new ScreenplayCompiler().Compile(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateDeclaration);
        var workspace = ScreenplayWorkspace.Create("Projects", [WorkspaceDocument.Create("application-document", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(source))], SemanticIdentityCatalog.Empty(_applicationIdentity));
        var compilation = workspace.WithAttachmentContents(ImmutableDictionary<string, string>.Empty.Add("First.cs", "return null;"), []).Compilation;
        compilation.Success.ShouldBeFalse();
        compilation.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateDeclaration);
    }

    [Fact]
    void should_refuse_an_unclosed_fence()
    {
        var result = new ScreenplayCompiler().Compile(and_the_inline_body_is_constrained.Source.Replace("          ```\n", string.Empty, StringComparison.Ordinal));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnclosedCodeBlock);
    }

    [Fact]
    void should_refuse_an_ambiguous_handler()
    {
        var result = new ScreenplayCompiler().Compile(and_the_inline_body_is_constrained.Source + "        handler\n          implementation\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.PendingIntentWithOtherHandler);
    }
}
