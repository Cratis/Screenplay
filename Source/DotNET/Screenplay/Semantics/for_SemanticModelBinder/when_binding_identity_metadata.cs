// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_identity_metadata : given.a_semantic_binder
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId Uuid identifier
                produces ProjectRegistered
                  for projectId
                  name = "Example"
              event ProjectRegistered
                name String
        """;
    const string Metadata = "identity\n  department String from claim \"department\"\n";
    CompilationResult<SemanticCompilation> _without;
    CompilationResult<SemanticCompilation> _with;

    void Because()
    {
        _without = Bind(Source);
        _with = Bind(Metadata + Source);
    }

    [Fact] void should_bind_metadata_without_diagnostics() => _with.Diagnostics.ShouldBeEmpty();
    [Fact] void should_leave_the_executable_bytes_unchanged() => SemanticModelSerializer.Serialize(_with.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(_without.Value!.Model)).ShouldBeTrue();

    [Theory]
    [InlineData("produces ProjectRegistered\n          for projectId\n          name = $identity.department")]
    [InlineData("produces when projectId == $identity.department\n          ProjectRegistered\n            for projectId\n            name = \"Example\"")]
    [InlineData("produces ProjectRegistered\n          for projectId\n          tag $identity.department\n          name = \"Example\"")]
    void should_refuse_executable_detail_reads(string production)
    {
        var source = Metadata + "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      command RegisterProject\n        projectId Uuid identifier\n        " + production + "\n      event ProjectRegistered\n        name String";
        var parsed = new ScreenplayCompiler().Compile(source);
        parsed.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("department", StringComparison.Ordinal) && diagnostic.Message.Contains("#600", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
