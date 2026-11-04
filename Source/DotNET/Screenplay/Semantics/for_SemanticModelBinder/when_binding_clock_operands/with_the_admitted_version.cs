// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_clock_operands;

public class with_the_admitted_version : given.a_clock_binder
{
    const string Body = "        validate\n          require dueDate >= today - 1 days\n";

    [Fact]
    void should_select_v37()
    {
        var binding = BindClock(Source(Body)).Value!;
        binding.LanguageVersion.ShouldEqual(LanguageVersion.V37);
        binding.SemanticVersion.ShouldEqual(SemanticVersion.V37);
    }

    [Fact]
    void should_create_an_executable_model_with_a_revision()
    {
        var binding = BindClock(Source(Body)).Value!;
        var model = ExecutableSemanticModel.Create(binding.LanguageVersion, binding.SemanticVersion, binding.Application);
        model.Revision.ShouldNotBeNull();
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(model))).ShouldEqual(SemanticModelSerializer.Serialize(model));
    }

    [Fact]
    void should_refuse_to_write_a_clock_graph_below_v37()
    {
        var binding = BindClock(Source(Body)).Value!;
        Assert.Throws<InvalidSemanticContract>(() => SemanticModelCanonicalJson.SerializeWithoutRevision(LanguageVersion.V36, SemanticVersion.V36, binding.Application));
    }

    [Fact]
    void should_keep_the_version_of_a_model_without_the_clock()
    {
        var binding = BindClock(Source(string.Empty)).Value!;
        binding.SemanticVersion.ShouldNotEqual(SemanticVersion.V37);
        binding.Application.TimeZone.ShouldBeNull();
    }

    [Fact]
    void should_admit_clock_operands_at_the_public_workspace_boundary()
    {
        var document = WorkspaceDocument.Create("application-document", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(Source(Body)));
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(_applicationIdentity));
        workspace.Compilation.Success.ShouldBeTrue();
    }
}
