// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_a_policy_file;

public class and_it_names_both_a_file_and_inline_code : given.a_printer
{
    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(new ApplicationSyntax(
        [],
        [],
        [new PolicySyntax("Access", null, new CodeBlockSyntax("csharp", "return true;", SourceLocation.Start), SourceLocation.Start)
        { File = new FileReferenceSyntax("Policies/Access.cs", SourceLocation.Start) }],
        [],
        SourceLocation.Start));

    [Fact] void should_keep_the_file() => _roundtrip.Reparsed.Value!.Policies.Single().File!.Path.ShouldEqual("Policies/Access.cs");
    [Fact] void should_omit_inline_code() => _roundtrip.Printed.ShouldNotContain("```csharp");
    [Fact] void should_explain_the_omission() => _roundtrip.Printed.ShouldContain("a policy is read back as a file reference or an inline block, not both");
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
}
