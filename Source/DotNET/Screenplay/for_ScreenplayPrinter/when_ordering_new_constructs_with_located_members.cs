// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Languages;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_ordering_new_constructs_with_located_members : given.a_printer
{
    [Fact]
    void should_insert_a_new_nested_feature_after_features_not_examples()
    {
        var application = _compiler.Parse("module M\n  feature F\n    feature Existing\n    example Fixture : E\n      p = 1").Value!;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var existing = feature.Features.Single();
        var inserted = existing with { Name = "Inserted", Location = SourceLocation.Start };
        var printed = _printer.Print(application with
        {
            Modules = [module with { Features = [feature with { Features = [existing, inserted] }] }]
        });

        (printed.IndexOf("feature Existing", StringComparison.Ordinal) < printed.IndexOf("feature Inserted", StringComparison.Ordinal)).ShouldBeTrue();
        (printed.IndexOf("feature Inserted", StringComparison.Ordinal) < printed.IndexOf("example Fixture", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    void should_insert_a_new_operation_after_operations_not_events()
    {
        var application = _compiler.Parse("module M\n  feature F\n    slice StateChange S\n      operation Existing\n        uses Mailer\n      event Recorded").Value!;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var existing = slice.Operations.Single();
        var inserted = existing with { Name = "Inserted", Location = SourceLocation.Start };
        var printed = _printer.Print(application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with { Operations = [existing, inserted] }] }] }]
        });

        (printed.IndexOf("operation Existing", StringComparison.Ordinal) < printed.IndexOf("operation Inserted", StringComparison.Ordinal)).ShouldBeTrue();
        (printed.IndexOf("operation Inserted", StringComparison.Ordinal) < printed.IndexOf("event Recorded", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("example Fixture : E // Header\n  p = 1\nfeature View", false)]
    [InlineData("example Fixture : E // Header\n  p = 1\nslice StateView View", true)]
    void should_keep_first_line_new_constructs_located_with_their_header_comments(string source, bool featurePlacement)
    {
        var placement = featurePlacement ? new PlayPlacement(["M", "F"]) : new PlayPlacement(["M"]);
        var application = ScreenplayCompiler.ParsePlaced(source, "body.play", placement, ScreenplayLanguageRegistry.Default);
        application.Diagnostics.ShouldBeEmpty();
        var printed = _printer.Print(application.Value!);

        (printed.IndexOf(source.Split('\n')[0], StringComparison.Ordinal) < printed.IndexOf(source.Split('\n')[2], StringComparison.Ordinal)).ShouldBeTrue();
        _printer.Print(ScreenplayCompiler.ParsePlaced(printed, "body.play", placement, ScreenplayLanguageRegistry.Default).Value!).ShouldEqual(printed);
    }

    [Fact]
    void should_keep_slice_examples_and_operations_in_authored_order_with_header_comments()
    {
        const string Source = "module M\n  feature F\n    slice StateChange S\n      example Fixture : Recorded // Fixture header\n        p = 1\n      operation Send // Operation header\n        uses Mailer\n      event Recorded\n        p Integer";
        var application = _compiler.Parse(Source);
        application.Diagnostics.ShouldBeEmpty();
        var printed = _printer.Print(application.Value!);

        (printed.IndexOf("example Fixture", StringComparison.Ordinal) < printed.IndexOf("operation Send", StringComparison.Ordinal)).ShouldBeTrue();
        (printed.IndexOf("operation Send", StringComparison.Ordinal) < printed.IndexOf("event Recorded", StringComparison.Ordinal)).ShouldBeTrue();
        printed.ShouldContain("example Fixture : Recorded // Fixture header");
        printed.ShouldContain("operation Send // Operation header");
        _printer.Print(_compiler.Parse(printed).Value!).ShouldEqual(printed);
    }

    [Fact]
    void should_order_an_import_position_among_feature_examples_without_moving_physical_comments()
    {
        var application = ScreenplayCompiler.ParsePlaced("feature F\n  example Before : E\n    p = 1\n  import \"View.play\"\n  example After : E\n    p = 2", "feature.play", new(["M"]), ScreenplayLanguageRegistry.Default).Value!;
        var child = ScreenplayCompiler.ParsePlaced("feature View // Physical header", "View.play", new(["M", "F"]), ScreenplayLanguageRegistry.Default).Value!.Modules.Single().Features.Single().Features.Single();
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var printed = _printer.Print(application with
        {
            Modules = [module with { Features = [feature with { FileImports = [], Features = [child with { PrintingLocation = feature.FileImports.Single().Location }] }] }]
        });

        (printed.IndexOf("example Before", StringComparison.Ordinal) < printed.IndexOf("feature View", StringComparison.Ordinal)).ShouldBeTrue();
        (printed.IndexOf("feature View", StringComparison.Ordinal) < printed.IndexOf("example After", StringComparison.Ordinal)).ShouldBeTrue();
        printed.ShouldContain("feature View // Physical header");
    }
}
