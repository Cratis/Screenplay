// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_diagnostics_are_indexed : given.a_command_production
{
    [Fact]
    void should_keep_compilation_diagnostics_out_of_the_parser_diagnostics()
    {
        Create(DestinationSource);
        var index = WorkspaceSyntaxIndex.Create(Workspace);
        index.Diagnostics.ShouldBeEmpty();
        index.RepairableDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.OmittedProductionDestination).ShouldBeTrue();
    }

    [Fact]
    void should_keep_distinct_unknown_policies_at_one_location()
    {
        Create(DestinationSource.Replace("      command Register", "      command Register\n        authorize FirstUnknown or SecondUnknown", StringComparison.Ordinal));
        var index = WorkspaceSyntaxIndex.Create(Workspace);
        var diagnostics = index.RepairableDiagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownPolicy).ToArray();
        diagnostics.Length.ShouldEqual(2);
        diagnostics[0].Location.ShouldEqual(diagnostics[1].Location);
        diagnostics[0].Message.ShouldNotEqual(diagnostics[1].Message);
        index.RepairableDiagnostics.Length.ShouldEqual(index.RepairableDiagnostics.Distinct().Count());
    }

    [Theory]
    [InlineData("Uuid?")]
    [InlineData("Uuid[]")]
    void should_not_advise_for_an_optional_or_collection_identifier(string type)
    {
        Create(DestinationSource.Replace("projectId Uuid identifier", $"projectId {type} identifier", StringComparison.Ordinal));
        Workspace.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.OmittedProductionDestination).ShouldBeFalse();
    }

    [Theory]
    [InlineData("command Register // explanation", "projectId Uuid identifier")]
    [InlineData("command Register", "projectId Uuid identifier // explanation")]
    [InlineData("command Register // explanation", "projectId Uuid identifier // explanation")]
    void should_advise_with_trailing_comments(string command, string property)
    {
        Create(DestinationSource.Replace("command Register", command, StringComparison.Ordinal).Replace("projectId Uuid identifier", property, StringComparison.Ordinal));
        Workspace.Compilation.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.OmittedProductionDestination).ShouldEqual(1);
    }

    [Theory]
    [InlineData("// outside")]
    [InlineData("      // beside the command")]
    [InlineData("        // beside the production")]
    void should_skip_comment_only_ancestry_lines(string comment)
    {
        Create(DestinationSource.Replace("        produces ProjectRegistered", $"{comment}\n        produces ProjectRegistered", StringComparison.Ordinal));
        WorkspaceSyntaxIndex.Create(Workspace).Diagnostics.ShouldBeEmpty();
        Workspace.Compilation.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.OmittedProductionDestination).ShouldEqual(1);
    }

    [Theory]
    [InlineData("description")]
    [InlineData("handler")]
    [InlineData("concurrency")]
    [InlineData("validate")]
    [InlineData("template")]
    [InlineData("profile")]
    [InlineData("when")]
    [InlineData("identifier")]
    [InlineData("sourceType")]
    [InlineData("streamType")]
    void should_advise_for_command_properties_named_like_keywords(string name)
    {
        Create(DestinationSource.Replace("projectId", name, StringComparison.Ordinal));
        WorkspaceSyntaxIndex.Create(Workspace).Diagnostics.ShouldBeEmpty();
        Workspace.Compilation.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.OmittedProductionDestination).ShouldEqual(1);
        Find(DiagnosticCodes.OmittedProductionDestination).ShouldNotBeNull();
    }

    [Fact]
    void should_not_discover_an_identifier_inside_a_fence()
    {
        Create("module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        produces ProjectRegistered\n        validate\n          ```csharp\n          ghostId Uuid identifier\n          ```\n      event ProjectRegistered\n");
        Workspace.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.OmittedProductionDestination).ShouldBeFalse();
    }
}
