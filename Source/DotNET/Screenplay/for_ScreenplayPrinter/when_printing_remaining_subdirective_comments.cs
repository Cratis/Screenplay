// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_remaining_subdirective_comments : given.a_printer
{
    [Fact]
    void should_keep_specification_comments()
    {
        const string source = """
            specification Checking
              given caller
                // signed in
                authenticated // authentication note
                // clerk role
                role "Clerk" // role note
              given OrderPlaced
                // event source
                for orderId // event source note
              when PlaceOrder
                // command source
                for orderId // command source note
              then query FindOrders
                // query inputs
                arguments // arguments note
                  id = orderId
            """;
        var parsed = _compiler.CompileSpecification(source);
        parsed.Diagnostics.ShouldBeEmpty();
        var printed = _printer.Print(parsed.Value!);
        AssertLines(
            printed,
            ("signed in", "authenticated // authentication note"),
            ("clerk role", "role \"Clerk\" // role note"),
            ("event source", "for orderId // event source note"),
            ("command source", "for orderId // command source note"),
            ("query inputs", "arguments // arguments note"));
        _printer.Print(_compiler.CompileSpecification(printed).Value!).ShouldEqual(printed);
    }

    [Fact]
    void should_keep_capture_comments()
    {
        const string source = """
            capture Import
              // top-level mappings
              map // map note
                split name by ","
                  // first target
                  first // first note
                  // second target
                  second // second note
              append NameImported
                // added condition
                when added // when note
                  name = first
              children items identified by id
                // child mappings
                map // child map note
                  id = id
              nested address
                // nested mappings
                map // nested map note
                  city = city
            """;
        var parsed = _compiler.CompileCapture(source);
        parsed.Diagnostics.ShouldBeEmpty();
        var printed = _printer.Print(parsed.Value!);
        AssertLines(
            printed,
            ("top-level mappings", "map // map note"),
            ("first target", "first // first note"),
            ("second target", "second // second note"),
            ("added condition", "when added // when note"),
            ("child mappings", "map // child map note"),
            ("nested mappings", "map // nested map note"));
        _printer.Print(_compiler.CompileCapture(printed).Value!).ShouldEqual(printed);
    }

    [Fact]
    void should_keep_projection_comments()
    {
        const string source = """
            projection Orders => OrderList
              // projection mapping
              automap // projection note
              from OrderPlaced
                // parent source
                parent orderId // parent note
              every
                // every mapping
                no automap // every note
                // child exclusion
                exclude children // exclusion note
              all
                // all mapping
                automap // all note
              children items identified by id
                // children mapping
                automap // children note
                from ItemAdded
              nested details
                // nested mapping
                no automap // nested note
                from DetailAdded
              join details on id
                with DetailsJoined
                  // joined mapping
                  automap // joined note
              remove with OrderRemoved
                // removal parent
                parent orderId // removal note
            """;
        var parsed = _compiler.CompileProjection(source);
        parsed.Diagnostics.ShouldBeEmpty();
        var printed = _printer.Print(parsed.Value!);
        AssertLines(
            printed,
            ("projection mapping", "automap // projection note"),
            ("parent source", "parent orderId // parent note"),
            ("every mapping", "no automap // every note"),
            ("child exclusion", "exclude children // exclusion note"),
            ("all mapping", "automap // all note"),
            ("children mapping", "automap // children note"),
            ("nested mapping", "no automap // nested note"),
            ("joined mapping", "automap // joined note"),
            ("removal parent", "parent orderId // removal note"));
        _printer.Print(_compiler.CompileProjection(printed).Value!).ShouldEqual(printed);
    }

    [Fact]
    void should_keep_command_comments()
    {
        const string source = """
            module Shop
              feature Orders
                slice StateChange Order
                  event OrderPlaced
                  command Place
                    id String
                    // validations
                    validate // validation note
                      // property rule
                      id not empty // rule note
                      // whole-command rule
                      require id == "a" // requirement note
                        // warning level
                        severity warning // severity note
                        // error text
                        message "Denied" // message note
                    produces when id == "a"
                      // event name
                      OrderPlaced // event note
                        // production target
                        for id // target note
            """;
        var roundtrip = RoundTrip(source);
        roundtrip.Original!.Diagnostics.ShouldBeEmpty();
        AssertLines(
            roundtrip.Printed,
            ("validations", "validate // validation note"),
            ("property rule", "id not empty // rule note"),
            ("whole-command rule", "require id == \"a\" // requirement note"),
            ("warning level", "severity warning // severity note"),
            ("error text", "message \"Denied\" // message note"),
            ("event name", "OrderPlaced // event note"),
            ("production target", "for id // target note"));
        roundtrip.PrintedAgain.ShouldEqual(roundtrip.Printed);
    }

    [Fact]
    void should_omit_explicit_default_requirement_severity_without_comments()
    {
        const string source = """
            module Shop
              feature Orders
                slice StateChange Order
                  command Place
                    id String
                    validate
                      require id == "a"
                        severity error
                        message "Denied"
                      require id == "b"
                        severity error
            """;
        var roundtrip = RoundTrip(source);
        roundtrip.Original!.Diagnostics.ShouldBeEmpty();
        roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
        roundtrip.Printed.ShouldNotContain("severity error");
        roundtrip.Printed.ShouldContain("require id == \"a\"\n            message \"Denied\"");
        roundtrip.Printed.ShouldContain("require id == \"b\"");
        roundtrip.PrintedAgain.ShouldEqual(roundtrip.Printed);
    }

    [Fact]
    void should_attach_comments_on_omitted_default_severity_to_requirement()
    {
        const string source = """
            module Shop
              feature Orders
                slice StateChange Order
                  command Place
                    id String
                    validate
                      require id == "a"
                        // explicit default
                        severity error // error note
            """;
        var roundtrip = RoundTrip(source);
        roundtrip.Original!.Diagnostics.ShouldBeEmpty();
        roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
        roundtrip.Printed.ShouldContain("// explicit default\n          require id == \"a\" // error note");
        roundtrip.Printed.ShouldNotContain("severity error");
        roundtrip.PrintedAgain.ShouldEqual(roundtrip.Printed);
    }

    [Fact]
    void should_omit_default_severity_after_typed_edit()
    {
        const string source = """
            module Shop
              feature Orders
                slice StateChange Order
                  command Place
                    id String
                    validate
                      require id == "a"
                        severity warning // severity note
            """;
        var parsed = _compiler.Compile(source);
        parsed.Diagnostics.ShouldBeEmpty();
        var module = parsed.Value!.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        var validation = (DeclarativeValidateSyntax)command.Validations.Single();
        var requirement = validation.Requirements!.Single();
        var edited = parsed.Value with
        {
            Modules = [module with
            {
                Features = [feature with
                {
                    Slices = [slice with
                    {
                        Commands = [command with
                        {
                            Validations = [validation with
                            {
                                Requirements = [requirement with { Severity = ValidationSeverity.Error }]
                            }]
                        }]
                    }]
                }]
            }]
        };
        var printed = _printer.Print(edited);
        printed.ShouldContain("require id == \"a\" // severity note");
        printed.ShouldNotContain("severity error");
        printed.ShouldNotContain("severity warning");
        _compiler.Compile(printed).Diagnostics.ShouldBeEmpty();
    }

    static void AssertLines(string printed, params (string Comment, string Directive)[] expected)
    {
        var lines = printed.Split('\n');
        foreach (var (comment, directive) in expected)
        {
            var preceding = $"// {comment}";
            lines.Count(line => line.Trim() == preceding).ShouldEqual(1);
            lines.Count(line => line.Trim() == directive).ShouldEqual(1);
            var index = Array.FindIndex(lines, line => line.Trim() == directive);
            lines[index - 1].Trim().ShouldEqual(preceding);
        }
    }
}
