// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_invalid_navigation_screen_target : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(when_compiling_a_navigation_screen_target.Layout + """
        module Shop
          outlet sidebar extends
            item unknown title "Unknown" target Shop.Projects screen Missing
            item unowned title "Unowned" target Shop.Projects screen Free
            item descendant title "Descendant" target Shop.Projects screen Archived
            item secondOwner title "Not the owner" target Shop.Other screen Shared
          screen Free
          screen Shared
          feature Projects
            slice StateView Listing
              uses screen Shared
            feature Archive
              slice StateView Listing
                screen Archived
          feature Other
            slice StateView Listing
              uses screen Shared
        """);

    [Fact] void should_reject_the_document() => _result.Success.ShouldBeFalse();
    [Fact] void should_not_fall_back_for_missing_unowned_descendant_or_second_adopter_screens() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownNavigationScreen).ShouldEqual(4);
}
