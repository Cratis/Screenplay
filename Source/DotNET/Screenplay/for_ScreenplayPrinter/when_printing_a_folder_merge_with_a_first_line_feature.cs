// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_folder_merge_with_a_first_line_feature : Specification
{
    string _printed = null!;

    void Because()
    {
        var module = ScreenplayCompiler.ParsePlaced("""
            module Example
              contribute to Navigation
                navigate to List
              screen template Shell
                main
            """, "module.play", PlayPlacement.Document, ScreenplayLanguageRegistry.Default);
        var feature = ScreenplayCompiler.ParsePlaced("""
            feature View
              slice StateView List
            """, "feature.play", new(["Example"]), ScreenplayLanguageRegistry.Default);
        var application = PlayFolderMerge.Merge([module, feature]).Value!;
        _printed = new ScreenplayPrinter().Print(application);
    }

    [Fact] void should_keep_the_module_files_contribution_before_its_template() => (_printed.IndexOf("contribute to Navigation", StringComparison.Ordinal) < _printed.IndexOf("screen template Shell", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_still_print_the_feature_from_the_other_file() => _printed.ShouldContain("feature View");
}
