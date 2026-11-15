// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_a_provide_takes_its_value_from_an_inline_code_block : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;
    CompilationResult<SemanticCompilation> _hostOnly;

    void Because()
    {
        _result = Bind(Source("        ```csharp\n        return 5;\n        ```\n"));
        _hostOnly = Bind(Source(""));
    }

    static string Source(string body) =>
        "module Fleet\n  feature Quality\n    slice StateView Vessels\n" +
        (body.Length == 0 ? "      provide limit Int from host\n" : "      provide limit Int\n" + body) +
        "      event VesselRegistered\n        vesselId Uuid\n";

    [Fact] void should_admit_it() => _result.Success.ShouldBeTrue();
    [Fact] void should_select_esm_v54() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V54);
    [Fact] void should_attach_a_resolved_csharp_body() => _result.ImplementationRequirements.Single().Language.ShouldEqual("csharp");
    [Fact] void should_use_the_provide_value_role() => _result.ImplementationRequirements.Single().Role.ShouldEqual(SemanticImplementationRole.ProvideValue);
    [Fact] void should_not_select_v54_without_a_code_provide() => _hostOnly.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V53);
    [Fact] void should_attach_nothing_without_a_code_provide() => _hostOnly.ImplementationRequirements.ShouldBeEmpty();
}
