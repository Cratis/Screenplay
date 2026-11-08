// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Tool.for_ExportCommand;

public class when_exporting_a_slice_scope : Tool.given.a_renderable_model
{
    int _exitCode;
    ExecutableSemanticModel _exported;

    void Because()
    {
        _exitCode = ToolCommands.Run(["export", Root, "--scope", "Projects.Registration.List"], Output, Error);
        _exported = SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(Output.ToString()));
    }

    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_keep_only_the_addressed_module() => _exported.Application.Modules.Select(module => module.Name).ShouldContainOnly(["Projects"]);
    [Fact] void should_keep_only_the_addressed_feature() => _exported.Application.Modules[0].Features.Select(feature => feature.Name).ShouldContainOnly(["Registration"]);
    [Fact] void should_keep_only_the_addressed_slice() => _exported.Application.Modules[0].Features[0].Slices.Select(slice => slice.Name).ShouldContainOnly(["List"]);
    [Fact] void should_keep_the_application_wide_concepts() => _exported.Application.Concepts.Length.ShouldEqual(2);
}
