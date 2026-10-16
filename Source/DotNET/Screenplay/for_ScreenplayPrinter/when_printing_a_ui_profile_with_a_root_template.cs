// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_ui_profile_with_a_root_template : given.a_printer
{
    const string Source =
        """
        theme Aurora
          compatible with Cratis.Components
          compatible with Cratis.Blueprint.Default
          compatible with crm

        layout AppFrame
          category Shell
          content

        ui profile Desktop
          target platform web
          target size regular

          packages
            Cratis.Components
            Cratis.Blueprint.Default
            crm

          theme Aurora

          root template blueprint Cratis.Blueprint.Default AppShell version "^4.5"
            option navigationMode slim
            option showFooter true
            option density 2
            option title "Contoso \"CRM\""

          appearance
            color scheme dark
            accent "#4f46e5"

          new screens use template blueprint crm Workspace version "~1.2"

          sample data
            active blueprint crm-nordic-smb

        ui profile Phone
          root template AppFrame

          new screens use template Sheet

        module Shell

          screen template Sheet
            category Page
            scopes feature
            body
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_byte_identical_canonical_source() => _roundtrip.Printed.TrimEnd().ShouldEqual(Source.TrimEnd());
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_preserve_a_blueprint_root_template() => (Desktop.Root!.Blueprint, Desktop.Root.Name, Desktop.Root.Version).ShouldEqual(("Cratis.Blueprint.Default", "AppShell", "^4.5"));
    [Fact] void should_preserve_the_options_in_order() => Desktop.Root!.Options.Select(option => option.Name).ShouldContainOnly("navigationMode", "showFooter", "density", "title");
    [Fact] void should_keep_a_word_option_as_a_word() => (Desktop.Root!.Options.First().Value, Desktop.Root.Options.First().Kind).ShouldEqual(("slim", ShellOptionValueKind.Word));
    [Fact] void should_keep_a_flag_option() => Desktop.Root!.Options.Skip(1).First().Kind.ShouldEqual(ShellOptionValueKind.Flag);
    [Fact] void should_keep_a_number_option() => Desktop.Root!.Options.Skip(2).First().Kind.ShouldEqual(ShellOptionValueKind.Number);
    [Fact] void should_unescape_a_text_option() => (Desktop.Root!.Options.Last().Value, Desktop.Root.Options.Last().Kind).ShouldEqual(("Contoso \"CRM\"", ShellOptionValueKind.Text));
    [Fact] void should_preserve_the_appearance() => (Desktop.Appearance!.ColorScheme, Desktop.Appearance.Accent).ShouldEqual(("dark", "#4f46e5"));
    [Fact] void should_preserve_the_new_screen_template() => (Desktop.NewScreenTemplate!.Blueprint, Desktop.NewScreenTemplate.Name, Desktop.NewScreenTemplate.Version).ShouldEqual(("crm", "Workspace", "~1.2"));
    [Fact] void should_preserve_an_own_root_template() => (Phone.Root!.Blueprint, Phone.Root.Name, Phone.Root.Version).ShouldEqual((null, "AppFrame", null));
    [Fact] void should_preserve_an_own_new_screen_template() => (Phone.NewScreenTemplate!.Blueprint, Phone.NewScreenTemplate.Name).ShouldEqual((null, "Sheet"));
    [Fact] void should_keep_the_sample_data_selection() => Desktop.SampleData!.Active!.Name.ShouldEqual("crm-nordic-smb");

    UiProfileSyntax Desktop => _roundtrip.Reparsed.Value!.UiProfiles!.First(profile => profile.Name == "Desktop");
    UiProfileSyntax Phone => _roundtrip.Reparsed.Value!.UiProfiles!.First(profile => profile.Name == "Phone");
}
