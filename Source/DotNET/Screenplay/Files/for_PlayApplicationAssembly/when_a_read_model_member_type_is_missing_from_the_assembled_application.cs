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

    [Fact] void should_fail_validation() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_only_one_error() => _result.Diagnostics.Single().Code.ShouldEqual("PLAY0273");
    [Fact] void should_locate_the_member_file() => _result.Diagnostics.Single().Location.Path.ShouldEqual("model.play");
    [Fact] void should_locate_the_member_line() => _result.Diagnostics.Single().Location.Line.ShouldEqual(6);
    [Fact] void should_locate_the_member_column() => _result.Diagnostics.Single().Location.Column.ShouldEqual(18);
}
