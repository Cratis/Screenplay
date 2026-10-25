// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson.given;

// Only the living Library sample uses this projection. The new action-subject vectors protect its
// current metadata; its pre-0110 frozen bytes still protect every preexisting member and value.
internal static class legacy_library_action_subjects
{
    internal static ApplicationSyntax Without(ApplicationSyntax application) => application with
    {
        Modules = application.Modules.Select(Module)
    };

    static ModuleSyntax Module(ModuleSyntax module) => module with { Features = module.Features.Select(Feature) };

    static FeatureSyntax Feature(FeatureSyntax feature) => feature with { Features = feature.Features.Select(Feature), Slices = feature.Slices.Select(Slice) };

    static SliceSyntax Slice(SliceSyntax slice) => slice with { Screens = slice.Screens.Select(Screen) };

    static ScreenSyntax Screen(ScreenSyntax screen) => screen with { Directives = screen.Directives.Select(Directive) };

    static ScreenDirectiveSyntax Directive(ScreenDirectiveSyntax directive) => directive is ScreenActionSyntax action
        ? action with { Subject = null, Inputs = [] }
        : directive;
}
