// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_PublicEventUsageValidator.when_validating_qualified_capture_sources;

public class with_preserved_resolution_boundaries : given.a_capture_source
{
    Diagnostic[] _bare;
    Diagnostic[] _output;
    Diagnostic[] _import;
    Diagnostic[] _ambiguous;

    void Because()
    {
        _bare = [.. new ScreenplayCompiler().Compile(Source("Missing")).Diagnostics];
        _output = Validate(Source("ProductPublished") + "\n      reaction Receive\n        when ProductPublished\n          produces Wrong.ProductReceived");
        _import = Validate("import Products.Catalog.Facts.ProductPublished\n" + Source("Wrong.ProductPublished"));
        _ambiguous = Validate("import One.ProductPublished from \"one\"\nimport Two.ProductPublished from \"two\"\n" + Source("ProductPublished").Replace("      event ProductPublished from \"catalog\"\n        productId ProductId\n", string.Empty));
    }

    static Diagnostic[] Validate(string source)
    {
        var context = ParserContext.ForDiagnostics();
        PublicEventUsageValidator.Validate(new ScreenplayCompiler().Parse(source).Value, context);
        return [.. context.Diagnostics];
    }

    [Fact] void should_keep_unknown_bare_inputs_as_warnings() => _bare.Single().ShouldEqual(Diagnostic.Warning(DiagnosticCodes.UnknownEvent, "Unknown event 'Missing' - declare its contract before classifying its usage.", _bare.Single().Location));
    [Fact] void should_not_promote_unrelated_qualified_outputs() => _output.All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_report_the_unknown_output() => _output.Single().Code.ShouldEqual(DiagnosticCodes.UnknownEvent);
    [Fact] void should_not_let_a_real_import_bless_a_wrong_qualifier() => _import.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_keep_genuine_ambiguity_unclassified() => _ambiguous.Single().Code.ShouldEqual(DiagnosticCodes.AmbiguousReference);
    [Fact] void should_not_promote_ambiguity() => _ambiguous.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
}
