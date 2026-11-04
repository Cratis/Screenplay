// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_PublicEventUsageValidator.when_validating_qualified_capture_sources;

public class with_an_unresolved_qualifier : given.a_capture_source
{
    CompilationResult<ApplicationSyntax>[] _results;

    void Because() => _results = [.. new[]
    {
        "Wrong.ProductPublished",
        "Domain.Model.Products.Catalog.Facts.ProductPublished",
        "Products.ProductPublished"
    }.Select(name => new ScreenplayCompiler().Compile(Source(name)))];

    [Fact] void should_fail_compilation() => _results.All(result => !result.Success).ShouldBeTrue();
    [Fact] void should_report_one_diagnostic_per_source() => _results.All(result => result.Diagnostics.Count() == 1).ShouldBeTrue();
    [Fact] void should_report_unknown_event_errors() => _results.All(result => result.Diagnostics.Single().Code == DiagnosticCodes.UnknownEvent && result.Diagnostics.Single().Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_locate_the_from_setting() => _results.All(result => result.Diagnostics.Single().Location == new SourceLocation(12, 11)).ShouldBeTrue();
}
