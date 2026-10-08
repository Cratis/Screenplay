// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_authoring_documentation : given.a_compiler
{
    const string Source = """
        module M
          documentation
            ```markdown
            Module **reasoning**.
            ```
          feature F
            documentation
              ```markdown
              Feature reasoning.
              ```
            slice StateChange S
              documentation
                ```markdown
                Slice reasoning.
                ```
              command C
                documentation String
                documentation
                  ```markdown
                  Command reasoning.
                  ```
              readmodel V
                documentation String
                documentation
                  ```markdown
                  View reasoning.
                  ```
              reaction R
                documentation
                  ```markdown
                  Reaction reasoning.
                  ```
                when E
              specification Case
                description "The rule this case witnesses"
              specification Multiline
                description
                  ```text
                  A case with
                  several lines.
                  ```
        """;

    ApplicationSyntax _application;
    ApplicationSyntax _roundTripped;
    string _printed;

    void Because()
    {
        var result = _compiler.Parse(Source);
        result.Diagnostics.ShouldBeEmpty();
        _application = result.Value!;
        _printed = new ScreenplayPrinter().Print(_application);
        _roundTripped = _compiler.Parse(_printed).Value!;
    }

    [Fact] void should_keep_module_documentation() => _application.Modules.Single().Documentation.ShouldEqual("Module **reasoning**.");
    [Fact] void should_keep_feature_documentation() => _application.Modules.Single().Features.Single().Documentation.ShouldEqual("Feature reasoning.");
    [Fact] void should_keep_slice_documentation() => Slice.Documentation.ShouldEqual("Slice reasoning.");
    [Fact] void should_keep_command_documentation() => Slice.Commands.Single().Documentation.ShouldEqual("Command reasoning.");
    [Fact] void should_keep_view_documentation() => Slice.ReadModels!.Single().Documentation.ShouldEqual("View reasoning.");
    [Fact] void should_keep_reaction_documentation() => Slice.Reactions.Single().Documentation.ShouldEqual("Reaction reasoning.");
    [Fact] void should_keep_the_specification_description() => Slice.Specifications.First().Description.ShouldEqual("The rule this case witnesses");
    [Fact] void should_keep_the_multiline_description() => Slice.Specifications.Last().Description.ShouldEqual("A case with\nseveral lines.");
    [Fact] void should_keep_typed_documentation_properties() => Slice.Commands.Single().Properties.Single().Name.ShouldEqual("documentation");
    [Fact] void should_round_trip_every_metadata_owner() => SyntaxJson.StructurallyEqual(_application, _roundTripped).ShouldBeTrue();
    [Fact] void should_print_idempotently() => new ScreenplayPrinter().Print(_roundTripped).ShouldEqual(_printed);
    [Fact] void should_describe_a_standalone_specification() => _compiler.CompileSpecification("specification Case\n  description \"A standalone witness\"\n  when C\n  then E\n").Value!.Description.ShouldEqual("A standalone witness");

    SliceSyntax Slice => _application.Modules.Single().Features.Single().Slices.Single();
}
