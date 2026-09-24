// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileCompiler;

public class when_restating_a_module_without_own_members : Specification
{
    CompilationResult<ApplicationSyntax> _result;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _result = PlayFolderMerge.Merge([
            compiler.Parse("module Sales\n  description \"Sales\"", "first.play"),
            compiler.Parse("module Sales\n  feature Orders\n    slice StateChange PlaceOrder", "second.play")]);
    }

    [Fact] void should_accept_the_restated_header() => _result.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_owners_description() => _result.Value!.Modules.Single().Description.ShouldEqual("Sales");
}
