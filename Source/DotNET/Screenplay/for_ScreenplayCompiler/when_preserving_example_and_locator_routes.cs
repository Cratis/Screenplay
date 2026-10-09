// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_preserving_example_and_locator_routes
{
    const string Source = """
        eventsource Account
          identifier String
          stream Ledger
            streamId
              key Uuid
              period String
        module M
          feature F
            slice Automation S
              event E
              reaction Observer
                when E
              example Prior : E
                for "account"
                stream Account.Ledger
                  streamId
                    period = "a|b%"
                    key = "3FA85F64-5717-4562-B3FC-2C963F66AFA6"
              specification Recovery
                given Prior
                when redelivered E to Observer
                  stream Account.Ledger
                    streamId
                      key = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                      period = "a|b%"
                then no events
        """;

    [Fact]
    void should_preserve_routes_and_authored_mapping_order_through_printing()
    {
        var compiler = new ScreenplayCompiler();
        var original = compiler.Compile(Source);
        original.Diagnostics.ShouldBeEmpty();
        var printer = new ScreenplayPrinter();
        var printed = printer.Print(original.Value!);
        var reparsed = compiler.Compile(printed);
        reparsed.Diagnostics.ShouldBeEmpty();
        SyntaxJson.StructurallyEqual(reparsed.Value!, original.Value!).ShouldBeTrue();
        printer.Print(reparsed.Value!).ShouldEqual(printed);
        printed.ShouldContain("period = \"a|b%\"\n            key = \"3FA85F64-5717-4562-B3FC-2C963F66AFA6\"");
    }

    [Fact]
    void should_admit_routes_but_keep_redelivery_refused_independently()
    {
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(Source));
        var workspace = ScreenplayWorkspace.Create("A", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));
        var errors = workspace.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == "PLAY0268").ToArray();
        errors.Any(diagnostic => diagnostic.Message.Contains("#433", StringComparison.Ordinal) && diagnostic.Location.Line == 21).ShouldBeTrue();
        errors.Any(diagnostic => diagnostic.Message.Contains("#457", StringComparison.Ordinal)).ShouldBeFalse();
        errors.Any(diagnostic => diagnostic.Location.Line is 15 or 22).ShouldBeFalse();
    }

    [Fact]
    void should_admit_an_unused_routed_example()
    {
        var source = Source[..Source.IndexOf("      specification", StringComparison.Ordinal)];
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(source));
        var workspace = ScreenplayWorkspace.Create("A", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));
        workspace.Compilation.Diagnostics.ShouldBeEmpty();
        workspace.Compilation.Success.ShouldBeTrue();
        workspace.Compilation.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V8);
    }
}
