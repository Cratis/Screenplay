// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_qualified_capture_sources;

public class with_a_retained_local_import : given.a_capture_source
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(new ScreenplayCompiler().Parse("import Products.Catalog.Facts.ProductPublished\n" + Source("Products.Catalog.Facts.ProductPublished")).Value);

    [Fact] void should_preserve_existing_import_admission() => _result.Success.ShouldBeFalse();
    [Fact] void should_not_produce_an_executable_model() => _result.Value.ShouldBeNull();
    [Fact] void should_only_report_the_unsupported_import() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_keep_the_import_error_severity() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_not_report_an_unknown_qualified_event() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownEvent).ShouldBeFalse();
}
