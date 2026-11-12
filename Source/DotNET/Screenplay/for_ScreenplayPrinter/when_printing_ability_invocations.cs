// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_ability_invocations : Specification
{
    string _source;
    string _printed;
    string _reprinted;

    void Because()
    {
        var root = Directory.GetParent(DocumentationExamples.Root())!.FullName;
        _source = File.ReadAllText(Path.Combine(root, "Documentation/screenplay/fixtures/ability-invocations.play"));
        var compiler = new ScreenplayCompiler();
        _printed = new ScreenplayPrinter().Print(compiler.Compile(_source).Value!);
        _reprinted = new ScreenplayPrinter().Print(compiler.Compile(_printed).Value!);
    }

    [Fact] void should_print_the_same_text_again() => _reprinted.ShouldEqual(_printed);
    [Fact] void should_keep_the_result_branches() => _printed.ShouldContain("on NoCapacity");
    [Fact] void should_keep_the_invocation_values() => _printed.ShouldContain("trackingNumber = $result.trackingNumber");
    [Fact] void should_keep_the_action_mappings() => _printed.ShouldContain("text = BookPickup.trackingNumber");
    [Fact] void should_keep_the_compute_node() => _printed.ShouldContain("compute Risk");
    [Fact] void should_keep_the_approval_ability() => _printed.ShouldContain("approval Review by Anyone via Carrier.Ask expires after 2 days");
    [Fact] void should_keep_the_entering_invocation() => _printed.ShouldContain("entering invokes Chat.Post");
    [Fact] void should_keep_the_entering_body() => _printed.ShouldContain("return Empty;");
}
