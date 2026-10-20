// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_a_folder_from_memory;

public class and_input_is_invalid : Specification
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("../x.play")]
    [InlineData("a/../../x.play")]
    [InlineData("/x.play")]
    [InlineData("\\x.play")]
    [InlineData("C:\\x.play")]
    [InlineData("x.txt")]
    [InlineData("*.play")]
    void should_reject_invalid_document_paths(string? path) =>
        Catch.Exception(() => new PlayFileCompiler().CompileFolder([new PlayFileContent(path!, "")])).ShouldBeOfExactType<ArgumentException>();

    [Fact] void should_reject_a_null_set() => Catch.Exception(() => new PlayFileCompiler().CompileFolder((IEnumerable<PlayFileContent>)null!)).ShouldBeOfExactType<ArgumentNullException>();
    [Fact] void should_reject_null_documents() => Catch.Exception(() => new PlayFileCompiler().CompileFolder(new PlayFileContent[] { null! })).ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_reject_null_source_text() => Catch.Exception(() => new PlayFileCompiler().CompileFolder([new PlayFileContent("a.play", null!)])).ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_reject_duplicate_paths() => Catch.Exception(() => new PlayFileCompiler().CompileFolder([new PlayFileContent("a.play", ""), new PlayFileContent("a.play", "")])).ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_reject_duplicate_normalized_paths() => Catch.Exception(() => new PlayFileCompiler().CompileFolder([new PlayFileContent("a/b.play", ""), new PlayFileContent("a\\b.play", "")])).ShouldBeOfExactType<ArgumentException>();
}
