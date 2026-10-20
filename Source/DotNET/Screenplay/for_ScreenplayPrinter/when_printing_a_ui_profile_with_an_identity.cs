// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_ui_profile_with_an_identity : given.a_printer
{
    const string Source =
        """
        ui profile Shop
          identity
            name "Acme \"Shop\""
            logo image "data:image/svg+xml;base64,PHN2Zz48L3N2Zz4="
            show name false

          appearance
            color scheme light
            accent "#c2410c"
            corners round
            font source-serif-4

        ui profile Floor
          identity
            logo icon package

          appearance
            corners sharp

        ui profile Back
          identity
            name "Back office"
            logo initials "BO"
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_byte_identical_canonical_source() => _roundtrip.Printed.TrimEnd().ShouldEqual(Source.TrimEnd());
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_unescape_the_name() => Shop.Identity!.Name.ShouldEqual("Acme \"Shop\"");
    [Fact] void should_keep_the_image_logo() => (Shop.Identity!.LogoKind, Shop.Identity.LogoValue).ShouldEqual(("image", "data:image/svg+xml;base64,PHN2Zz48L3N2Zz4="));
    [Fact] void should_keep_that_the_name_is_hidden() => Shop.Identity!.ShowName.ShouldEqual(false);
    [Fact] void should_keep_the_corners_and_font() => (Shop.Appearance!.Corners, Shop.Appearance.Font).ShouldEqual(("round", "source-serif-4"));
    [Fact] void should_keep_an_icon_logo_as_a_word() => (Floor.Identity!.LogoKind, Floor.Identity.LogoValue, Floor.Identity.Name, Floor.Identity.ShowName).ShouldEqual(("icon", "package", null, null));
    [Fact] void should_keep_an_initials_logo() => (Back.Identity!.LogoKind, Back.Identity.LogoValue).ShouldEqual(("initials", "BO"));

    UiProfileSyntax Shop => _roundtrip.Reparsed.Value!.UiProfiles!.First(profile => profile.Name == "Shop");
    UiProfileSyntax Floor => _roundtrip.Reparsed.Value!.UiProfiles!.First(profile => profile.Name == "Floor");
    UiProfileSyntax Back => _roundtrip.Reparsed.Value!.UiProfiles!.First(profile => profile.Name == "Back");
}
