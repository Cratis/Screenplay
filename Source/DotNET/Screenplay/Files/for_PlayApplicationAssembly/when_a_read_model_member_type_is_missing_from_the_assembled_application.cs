// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayApplicationAssembly;

public class when_a_read_model_member_type_is_missing_from_the_assembled_application : Specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => (_, _result) = PlayApplicationAssembly.Compile(new ScreenplayCompiler(), ["model.play"], new InMemoryPlayDocumentSource(new Dictionary<string, string>
    {
        ["model.play"] = "import \"values.play\"\nmodule M\n  feature F\n    slice StateView View\n      readmodel Parent\n        children Child[]",
        ["values.play"] = "type Other\n  value String"
    }));

    [Fact] void should_compile_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_not_report_an_unsupported_member_type() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0273").ShouldBeFalse();
}
