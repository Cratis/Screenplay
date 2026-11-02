// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_clock_operands;

public class without_publishing_the_clock_candidate : given.a_clock_binder
{
    const string Body = "        validate\n          require dueDate >= today\n";

    [Fact]
    void should_refuse_to_write_a_clock_graph_at_the_working_major()
    {
        var binding = BindClock(Source(Body)).Value!;
        Assert.Throws<InvalidSemanticContract>(() => SemanticModelCanonicalJson.SerializeWithoutRevision(binding.LanguageVersion, binding.SemanticVersion, binding.Application));
    }

    [Fact]
    void should_refuse_to_write_a_clock_graph_below_the_working_major()
    {
        var binding = BindClock(Source(Body)).Value!;
        Assert.Throws<InvalidSemanticContract>(() => SemanticModelCanonicalJson.SerializeWithoutRevision(LanguageVersion.V34, SemanticVersion.V34, binding.Application));
    }

    [Fact]
    void should_keep_the_public_factory_closed_to_the_working_major()
    {
        var binding = BindClock(Source(Body)).Value!;
        Assert.Throws<InvalidSemanticContract>(() => ExecutableSemanticModel.Create(binding.LanguageVersion, binding.SemanticVersion, binding.Application));
    }

    [Fact]
    void should_refuse_clock_operands_at_the_public_workspace_boundary()
    {
        var document = WorkspaceDocument.Create("application-document", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(Source("        validate\n          require dueDate >= today - 1 days\n")));
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(_applicationIdentity));
        workspace.Compilation.Success.ShouldBeFalse();
        workspace.Compilation.Diagnostics.ShouldContain(diagnostic =>
            diagnostic.Code == "PLAY0268" &&
            diagnostic.Message == "Clock operands (today, now and date of) are not yet admitted (decision 0106/0107) (#824).");
    }

    [Fact]
    void should_keep_the_existing_bare_today_refusal_at_the_public_workspace_boundary()
    {
        var document = WorkspaceDocument.Create("application-document", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(Source(Body)));
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(_applicationIdentity));
        workspace.Compilation.Success.ShouldBeFalse();
        workspace.Compilation.Diagnostics.ShouldContain(diagnostic =>
            diagnostic.Code == "PLAY0268" &&
            diagnostic.Message == "Date comparisons require same-primitive scalar required command properties or a literal in require or produces when; read paths, context and today are not admitted.");
    }

    [Fact]
    void should_not_register_the_working_major_in_the_frozen_tables()
    {
        Assert.Throws<InvalidSemanticContract>(() => ExecutableSemanticModel.Create(new LanguageVersion(37, 0), new SemanticVersion(37, 0), BindClock(Source(string.Empty)).Value!.Application));
    }

    [Fact]
    void should_keep_the_clock_candidate_outside_published_v36_support()
    {
        var binding = BindClock(Source(Body)).Value!;
        binding.LanguageVersion.ShouldEqual(new LanguageVersion(37, 0));
        binding.SemanticVersion.ShouldEqual(new SemanticVersion(37, 0));
        EsmSchemaV36Support.Supports(binding.LanguageVersion, binding.SemanticVersion).ShouldBeFalse();
        Assert.Throws<InvalidSemanticContract>(() => EsmSchemaV36Support.EnsureSupported(binding.LanguageVersion, binding.SemanticVersion));
    }
}
