// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_a_policy_file;

public class and_it_names_a_file : given.a_printer
{
    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip("policy Access\n  file Policies/Access.cs");

    [Fact] void should_compile_the_original() => _roundtrip.Original!.Success.ShouldBeTrue();
    [Fact] void should_print_the_path() => _roundtrip.Printed.ShouldContain("file Policies/Access.cs");
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_the_file() => _roundtrip.Reparsed.Value!.Policies.Single().File!.Path.ShouldEqual("Policies/Access.cs");
    [Fact] void should_not_invent_inline_code() => _roundtrip.Reparsed.Value!.Policies.Single().Code.ShouldBeNull();
    [Fact] void should_print_identically_twice() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
