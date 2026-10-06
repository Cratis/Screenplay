// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.for_Documentation;

public class when_compiling_the_operation_fixture : Specification
{
    CompilationResult<ApplicationSyntax> _syntax;
    ScreenplayWorkspace _workspace;

    void Because()
    {
        var path = Path.Combine(DocumentationExamples.Root(), "screenplay", "fixtures", "operations.play");
        var source = File.ReadAllText(path);
        _syntax = new ScreenplayCompiler().Compile(source);
        _workspace = ScreenplayWorkspace.Create("Projects", [WorkspaceDocument.Create("operations", PortablePlayPath.Parse("operations.play"), Encoding.UTF8.GetBytes(source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
    }

    [Fact] void should_compile_the_documented_syntax() => _syntax.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_declared_production_sequence() => _syntax.Value!.Modules.Single().Features.Single().Slices.First().Commands.Single().Produces.Select(production => production.Event).SequenceEqual(["ProjectRegistered", "SendWelcomeEmail", "NotifyAccounting"]).ShouldBeTrue();
    [Fact] void should_reject_executable_admission() => _workspace.Compilation.Success.ShouldBeFalse();
    [Fact] void should_name_unadmitted_operations_in_the_semantic_refusal() => _workspace.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0268" && diagnostic.Message.Contains("not admitted by any supported executable model (ESM) version yet (#301)", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_emit_operation_requirements() => _workspace.Compilation.ImplementationRequirements.ShouldBeEmpty();
}
