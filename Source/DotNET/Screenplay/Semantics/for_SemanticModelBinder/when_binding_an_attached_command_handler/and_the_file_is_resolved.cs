// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_an_attached_command_handler;

public class and_the_file_is_resolved : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = BindFile("Choose.cs", and_the_inline_body_is_constrained.Body);

    [Theory]
    [InlineData("Choose.CS", "csharp")]
    [InlineData("Choose.ts", "typescript")]
    [InlineData("Choose.js", "javascript")]
    [InlineData("Choose.java", "java")]
    [InlineData("Choose.kt", "kotlin")]
    [InlineData("Choose.ex", "elixir")]
    [InlineData("Choose.go", "go")]
    void should_infer_only_the_attachment_language(string path, string language)
    {
        var result = BindFile(path, and_the_inline_body_is_constrained.Body);
        result.Success.ShouldBeTrue();
        result.ImplementationRequirements.Single().Language.ShouldEqual(language);
    }

    [Theory]
    [InlineData("Choose.unknown", "opaque")]
    [InlineData("Choose.cs", "")]
    [InlineData("Choose.cs", " \n\t")]
    void should_refuse_an_unknown_language_or_empty_body(string path, string content) => BindFile(path, content).Success.ShouldBeFalse();

    CompilationResult<SemanticCompilation> BindFile(string path, string content)
    {
        var source = and_the_inline_body_is_constrained.Prefix + "        produces Chosen\n        handler\n          file " + path + "\n";
        var workspace = ScreenplayWorkspace.Create("Projects", [WorkspaceDocument.Create("application-document", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(source))], SemanticIdentityCatalog.Empty(_applicationIdentity));
        return workspace.WithAttachmentContents(ImmutableDictionary<string, string>.Empty.Add(path, content), []).Compilation;
    }

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_infer_the_language() => _result.ImplementationRequirements.Single().Language.ShouldEqual("csharp");
    [Fact] void should_keep_the_authored_path() => _result.ImplementationRequirements.Single().File.ShouldEqual("Choose.cs");
    [Fact] void should_resolve_the_attachment() => _result.ImplementationRequirements.Single().AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.Resolved);
    [Fact] void should_select_v23() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V23);
}
