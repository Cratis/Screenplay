// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Files.for_ScreenVisualFiles;

public class when_checking_screen_visuals : Specification
{
    const string Source = """
        module Lending
          feature Books
            slice StateView ListBooks
              screen Books
                sketch Screens/books.sketch.json
                image Screens/books.png
                mockup ../outside.html
        """;

    string _root = null!;
    ImmutableArray<Diagnostic> _diagnostics;

    void Establish()
    {
        _root = Directory.CreateTempSubdirectory("screen-visuals").FullName;
        Directory.CreateDirectory(Path.Combine(_root, "Screens"));
        File.WriteAllText(Path.Combine(_root, "Screens", "books.sketch.json"), "{}");
    }

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        _diagnostics = ScreenVisualFiles.Check(_root, [SemanticSourceDocument.Create(catalog.ResolveDocument("library"), "library", "library.play", Source)]);
    }

    [Fact] void should_accept_a_file_that_is_there() => _diagnostics.ShouldNotContain(diagnostic => diagnostic.Message.Contains("books.sketch.json", StringComparison.Ordinal));
    [Fact] void should_warn_about_a_missing_image() => _diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.ScreenVisualMissing && diagnostic.Severity == DiagnosticSeverity.Warning && diagnostic.Message.Contains("books.png", StringComparison.Ordinal));
    [Fact] void should_warn_about_a_file_outside_the_model_root() => _diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.ScreenVisualMissing && diagnostic.Message.Contains("outside the model root", StringComparison.Ordinal));
    [Fact] void should_report_one_diagnostic_per_absent_visual() => _diagnostics.Length.ShouldEqual(2);

    void Destroy() => Directory.Delete(_root, true);
}
