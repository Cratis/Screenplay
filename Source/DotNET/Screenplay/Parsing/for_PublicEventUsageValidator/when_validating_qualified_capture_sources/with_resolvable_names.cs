// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_PublicEventUsageValidator.when_validating_qualified_capture_sources;

public class with_resolvable_names : given.a_capture_source
{
    CompilationResult<ApplicationSyntax>[] _results;

    void Because()
    {
        var sources = new[]
        {
            Source("Products.Catalog.Facts.ProductPublished"),
            Source("Catalog.Facts.ProductPublished"),
            Source("Facts.ProductPublished"),
            Source("ProductPublished"),
            "import Products.Catalog.Facts.ProductPublished\n" + Source("ProductPublished"),
            Source("Catalog.Nested.Facts.ProductPublished").Replace("\n    ", "\n      ").Replace("  feature Catalog", "  feature Catalog\n    feature Nested"),
            Source("Facts.ProductPublished").Replace("      event ProductPublished from", "      event ProductPublished generation 1 from \"catalog\"\n        productId ProductId\n      event ProductPublished generation 2 from"),
            "import Outside.ProductPublished from \"catalog\"\n" + Source("Outside.ProductPublished"),
            "import Outside.ProductPublished\n" + Source("Outside.ProductPublished")
        };
        _results = [.. sources.Select(source => new ScreenplayCompiler().Compile(source))];
    }

    [Fact] void should_compile_every_resolvable_spelling() => _results.All(result => result.Success).ShouldBeTrue();
    [Fact] void should_only_preserve_redundant_import_warnings() => _results.SelectMany(result => result.Diagnostics).All(diagnostic => diagnostic.Code == DiagnosticCodes.RedundantImport && diagnostic.Severity == DiagnosticSeverity.Warning).ShouldBeTrue();
}
