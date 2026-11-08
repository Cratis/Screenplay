// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.for_DesignSystem.given;

namespace Cratis.Screenplay.for_DesignSystem;

public class when_compiling_a_design_system_that_is_wrong : given.a_compiler
{
    const string Document = a_design_system_document.Source;

    static string Mutate(string find, string replace)
    {
        var index = Document.IndexOf(find, StringComparison.Ordinal);
        index.ShouldNotEqual(-1);
        return Document.Remove(index, find.Length).Insert(index, replace);
    }

    public static TheoryData<string, string, string> Cases => new()
    {
        { "a missing default theme", Mutate("  default theme T\n", string.Empty), DiagnosticCodes.InvalidDefaultTheme },
        { "an undeclared default theme", Mutate("default theme T", "default theme Nope"), DiagnosticCodes.InvalidDefaultTheme },
        { "a second design system", Document + "\n\ndesign system E\n", DiagnosticCodes.DuplicateDesignSystem },
        { "an unknown token category", Mutate("    space\n      sm\n", "    gap\n      sm\n"), DiagnosticCodes.InvalidDesignSystemDeclaration },
        { "an unknown component part", Mutate("parts text, background, radius", "parts text, glow, radius"), DiagnosticCodes.InvalidDesignSystemDeclaration },
        { "an unknown state", Mutate("state hover", "state squint"), DiagnosticCodes.InvalidDesignSystemDeclaration },
        { "an unknown mode block", Mutate("\n\nui profile P", "\n  dusk\n    color.text = palette.white\n\nui profile P"), DiagnosticCodes.UnknownDesignMode },
        { "a duplicate token", Mutate("    space\n      sm\n", "    space\n      sm\n      sm\n"), DiagnosticCodes.DuplicateDesignToken },
        { "a value for an undeclared token", Mutate("space.sm       = \"8px\"", "space.xx       = \"8px\""), DiagnosticCodes.ThemeValueForUndeclaredToken },
        { "a color token without a value in a mode", Mutate("    color.on-primary = palette.white\n\nui profile P", "\nui profile P"), DiagnosticCodes.ColorTokenMissingModeValue },
        { "an unresolved palette reference", Mutate("color.text       = palette.ink", "color.text       = palette.nope"), DiagnosticCodes.UnresolvedDesignReference },
        { "a reference to an undeclared token", Mutate("background = color.primary\n        radius", "background = color.nothing\n        radius"), DiagnosticCodes.UnresolvedDesignReference },
        { "a reference cycle", Mutate("color.text       = palette.ink", "color.text       = color.surface").Replace("color.surface    = palette.white", "color.surface    = color.text"), DiagnosticCodes.DesignReferenceCycle },
        { "a part referencing the wrong category", Mutate("background = color.primary\n        radius", "background = space.sm\n        radius"), DiagnosticCodes.DesignPartCategoryMismatch },
        { "a theme implementing an unknown design system", Mutate("implements D", "implements Other"), DiagnosticCodes.ThemeImplementsUnknownDesignSystem },
        { "a profile selecting a theme that does not implement it", Mutate("ui profile P\n", "theme U\n  compatible with core\n\nui profile P\n  theme U\n"), DiagnosticCodes.ProfileThemeDoesNotImplementDesignSystem },
        { "text and background below the contrast rule", Mutate("blue  = \"#1D4ED8\"", "blue  = \"#93C5FD\""), DiagnosticCodes.ContrastBelowRule },
        { "a deprecated appearance member", Mutate("color scheme dark", "color scheme dark\n    accent \"#1D4ED8\""), DiagnosticCodes.DeprecatedAppearanceMember },
        { "a malformed theme value line", Mutate("  values\n", "  values\n    odd line\n"), DiagnosticCodes.InvalidThemeBlock },
        { "a theme implementing twice", Mutate("  implements D\n", "  implements D\n  implements D\n"), DiagnosticCodes.InvalidThemeBlock }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    void should_report_the_diagnostic(string description, string source, string code) =>
        _compiler.Compile(source).Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(code);
}
