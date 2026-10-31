// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_qualified_capture_sources;

public class with_unresolved_parsed_sources : given.a_capture_source
{
    CompilationResult<SemanticCompilation>[] _results;

    void Because() => _results = [.. new[] { "Wrong.ProductPublished", "Domain.Model.Products.Catalog.Facts.ProductPublished", "Products.ProductPublished" }
        .Select(name => Bind(new ScreenplayCompiler().Parse(Source(name), "capture.play").Value))];

    [Fact] void should_reject_the_syntax_without_relying_on_compile() => _results.All(result => !result.Success).ShouldBeTrue();
    [Fact] void should_not_produce_a_model() => _results.All(result => result.Value is null).ShouldBeTrue();
    [Fact] void should_report_one_unknown_event_error() => _results.All(result => result.Diagnostics.Single().Code == DiagnosticCodes.UnknownEvent && result.Diagnostics.Single().Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_locate_the_from_setting() => _results.All(result => result.Diagnostics.Single().Location == new SourceLocation(12, 11, "capture.play")).ShouldBeTrue();
}
