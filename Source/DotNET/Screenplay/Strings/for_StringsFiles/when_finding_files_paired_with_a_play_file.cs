// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Strings.for_StringsFiles;

public class when_finding_files_paired_with_a_play_file : Specification
{
    DirectoryInfo _root;
    List<StringsFileReference> _files;
    StringsFile _english;

    void Establish()
    {
        _root = Directory.CreateTempSubdirectory("stringsfiles");
        File.WriteAllText(Path.Combine(_root.FullName, "invoicing.play"), for_ScreenplayCompiler.given.Samples.Invoicing);
        File.WriteAllText(Path.Combine(_root.FullName, "invoicing.en.strings"), for_ScreenplayCompiler.given.Samples.InvoicingStrings);
        Directory.CreateDirectory(Path.Combine(_root.FullName, "nested"));
        File.WriteAllText(Path.Combine(_root.FullName, "nested", "orders.nb.strings"), "ordre.tittel = \"Ordrer\"");
    }

    void Because()
    {
        _files = [.. new StringsFiles().FindIn(_root.FullName)];
        _english = StringsFile.Parse(new StringsFiles().ReadContent(_files[0]));
    }

    [Fact] void should_find_both_strings_files() => _files.Count.ShouldEqual(2);
    [Fact] void should_not_find_the_play_file() => _files.Exists(_ => _.Path.EndsWith(".play", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_pair_the_english_file_with_its_play_file() => _files[0].BaseName.ShouldEqual("invoicing");
    [Fact] void should_parse_the_english_locale() => _files[0].Locale.ShouldEqual("en");
    [Fact] void should_pair_the_nested_file_with_its_base() => _files[1].BaseName.ShouldEqual("orders");
    [Fact] void should_parse_the_norwegian_locale() => _files[1].Locale.ShouldEqual("nb");
    [Fact] void should_parse_the_sample_strings() => _english.Entries.Select(_ => _.Key).ShouldContain("invoices.validation.reasonRequired");

    void Destroy() => _root.Delete(true);
}
