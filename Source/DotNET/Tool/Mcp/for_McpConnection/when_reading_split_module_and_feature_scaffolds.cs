// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Tool.Mcp.for_McpConnection;

public class when_reading_split_module_and_feature_scaffolds : given.a_connection
{
    McpSnapshot _snapshot = null!;
    JsonElement _module;
    JsonElement _feature;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "listing.play"), """
            module Projects
              description "Project management"
              feature Registration
                description "Registration workflows"
                slice StateView ListProjects
                  screen Projects
                    action RegisterProject
            """);
        Initialize();
    }

    void Because()
    {
        _snapshot = new(Root.Read());
        _module = Call("find-references", new { address = "Projects", kind = "Module" }).GetProperty("result").GetProperty("structuredContent");
        _feature = Call("find-references", new { address = "Projects.Registration", kind = "Feature" }).GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_compile_the_split_application() => _snapshot.Compilation.Success.ShouldBeTrue();
    [Fact] void should_have_one_logical_module() => _snapshot.Index.Declarations.Count(declaration => declaration.Kind == "Module").ShouldEqual(1);
    [Fact] void should_have_one_logical_feature() => _snapshot.Index.Declarations.Count(declaration => declaration.Kind == "Feature").ShouldEqual(1);
    [Fact] void should_keep_both_module_locations() => _module.GetProperty("declaration").GetProperty("locations").GetArrayLength().ShouldEqual(2);
    [Fact] void should_keep_both_feature_locations() => _feature.GetProperty("declaration").GetProperty("locations").GetArrayLength().ShouldEqual(2);
    [Fact] void should_keep_the_later_module_description() => _snapshot.Index.Declarations.Single(declaration => declaration.Kind == "Module").Description.ShouldEqual("Project management");
    [Fact] void should_keep_the_later_feature_description() => _snapshot.Index.Declarations.Single(declaration => declaration.Kind == "Feature").Description.ShouldEqual("Registration workflows");
    [Fact] void should_expose_the_complete_logical_feature_syntax() => ((FeatureSyntax)_snapshot.Index.Declarations.Single(declaration => declaration.Kind == "Feature").Syntax).Slices.Count().ShouldEqual(2);
    [Fact] void should_keep_both_physical_feature_parts() => _snapshot.Index.Declarations.Single(declaration => declaration.Kind == "Feature").Parts.Count.ShouldEqual(2);
    [Fact] void should_resolve_a_cross_file_command_reference() => _snapshot.Index.Resolve(_snapshot.Index.References.Single(reference => reference.Role == "action")).Single().Address.ShouldEqual("Projects.Registration.RegisterProject.RegisterProject");
}
