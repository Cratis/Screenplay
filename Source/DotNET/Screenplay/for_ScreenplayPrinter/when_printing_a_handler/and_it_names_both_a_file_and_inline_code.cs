// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_a_handler;

public class and_it_names_both_a_file_and_inline_code : given.a_printer
{
    const string Path = "Customers/ApproveCustomer.cs";
    const string Code = "return new CustomerApproved();";

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(
        given.a_hand_built_application.WithHandler(
            new HandlerSyntax(
                new FileReferenceSyntax(Path, SourceLocation.Start),
                new CodeBlockSyntax("csharp", Code, SourceLocation.Start),
                SourceLocation.Start)));

    [Fact] void should_write_the_file_directive() => _roundtrip.Printed.ShouldContain($"file {Path}");
    [Fact] void should_not_write_a_fence() => _roundtrip.Printed.ShouldNotContain("```");
    [Fact] void should_not_write_the_code() => _roundtrip.Printed.ShouldNotContain(Code);
    [Fact] void should_note_the_omitted_block() => _roundtrip.Printed.ShouldContain("// TODO: the inline 'csharp' block is not written here");
    [Fact] void should_say_why_it_is_not_written() => _roundtrip.Printed.ShouldContain("a handler is read back as a file reference or an inline block, not both");
    [Fact] void should_reparse_successfully() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_the_file() => Handler!.File!.Path.ShouldEqual(Path);
    [Fact] void should_reparse_without_the_code() => Handler!.Code.ShouldBeNull();
    [Fact] void should_have_nothing_left_to_note_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldNotContain("// TODO:");

    HandlerSyntax? Handler => given.a_hand_built_application.Handler(_roundtrip.Reparsed);
}
