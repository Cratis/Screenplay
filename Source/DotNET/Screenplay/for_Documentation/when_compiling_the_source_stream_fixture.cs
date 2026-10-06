// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.for_Documentation;

public class when_compiling_the_source_stream_fixture : Specification
{
    CompilationResult<ApplicationSyntax> _syntax;
    ScreenplayWorkspace _workspace;
    string _source;

    void Because()
    {
        _source = File.ReadAllText(Path.Combine(DocumentationExamples.Root(), "screenplay", "fixtures", "source-streams.play"));
        _syntax = new ScreenplayCompiler().Compile(_source);
        _workspace = ScreenplayWorkspace.Create("Banking", [WorkspaceDocument.Create("source-streams", PortablePlayPath.Parse("source-streams.play"), Encoding.UTF8.GetBytes(_source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Banking")));
    }

    [Fact] void should_compile_without_source_diagnostics() => _syntax.Diagnostics.ShouldBeEmpty();
    [Fact] void should_accept_the_source_fixture() => _syntax.Success.ShouldBeTrue();
    [Fact] void should_refuse_executable_admission() => _workspace.Compilation.Success.ShouldBeFalse();
    [Fact] void should_name_unadmitted_sources_and_streams_in_the_refusal() => _workspace.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0268" && diagnostic.Message.Contains("not admitted by any supported executable model (ESM) version yet (#302)", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_enroll_source_requirements() => _workspace.Compilation.ImplementationRequirements.ShouldBeEmpty();
    [Fact] void should_keep_the_documented_example_equal_to_the_fixture() => File.ReadAllText(Path.Combine(DocumentationExamples.Root(), "screenplay", "event-sources.md")).ShouldContain("```screenplay\n" + _source + "```");
}
