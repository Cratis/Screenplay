// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_PlayFiles;

public class when_finding_files_in_a_tree : Specification
{
    DirectoryInfo _root;
    IEnumerable<PlayFile> _files;

    void Establish()
    {
        _root = Directory.CreateTempSubdirectory("playfiles");
        File.WriteAllText(Path.Combine(_root.FullName, "invoicing.play"), "module Invoicing");
        Directory.CreateDirectory(Path.Combine(_root.FullName, "nested", "deeper"));
        File.WriteAllText(Path.Combine(_root.FullName, "nested", "deeper", "orders.play"), "module Orders");
        File.WriteAllText(Path.Combine(_root.FullName, "notes.txt"), "not a play file");
    }

    void Because() => _files = new PlayFiles().FindIn(_root.FullName);

    [Fact] void should_find_both_play_files() => _files.Count().ShouldEqual(2);
    [Fact] void should_find_the_root_file() => _files.First().RelativePath.ShouldEqual("invoicing.play");
    [Fact] void should_find_the_nested_file() => _files.Last().RelativePath.ShouldEqual(Path.Combine("nested", "deeper", "orders.play"));
    [Fact] void should_not_find_other_files() => _files.Any(_ => _.Path.EndsWith(".txt", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_read_content() => new PlayFiles().ReadContent(_files.First()).ShouldEqual("module Invoicing");

    void Destroy() => _root.Delete(true);
}
