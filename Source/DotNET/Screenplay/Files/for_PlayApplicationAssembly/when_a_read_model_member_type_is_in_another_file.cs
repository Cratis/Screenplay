// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayApplicationAssembly;

public class when_a_read_model_member_type_is_in_another_file : Specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => (_, _result) = PlayApplicationAssembly.Compile(new ScreenplayCompiler(), ["model.play"], new InMemoryPlayDocumentSource(new Dictionary<string, string>
    {
        ["model.play"] = "import \"values.play\"\nmodule M\n  feature F\n    slice StateView View\n      readmodel Parent\n        children Child[]",
        ["values.play"] = "type Child\n  value String"
    }));

    [Fact] void should_validate_the_complete_application() => _result.Success.ShouldBeTrue();
    [Fact] void should_resolve_the_imported_file_before_member_validation() => _result.Diagnostics.ShouldBeEmpty();
}
