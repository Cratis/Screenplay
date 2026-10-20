// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_PlayFileCompiler.when_compiling_an_application_from_memory;

public class and_the_entry_is_invalid : Specification
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("../outside.play")]
    [InlineData("/rooted.play")]
    [InlineData("missing.play")]
    void should_reject_an_invalid_or_missing_entry(string? path) =>
        Catch.Exception(() => new PlayFileCompiler().CompileApplication(path!, [])).ShouldBeOfExactType<ArgumentException>();
}
