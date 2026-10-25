// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_a_slice_publishes_what_another_slice_appends.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan.when_isolating_to_a_slice.given;

public class a_plan_with_two_slices : a_publishing_model
{
    protected SemanticExecutionPlan _plan;
    protected SemanticSlice _commandSlice;
    protected SemanticSlice _translationSlice;

    void Establish()
    {
        var compilation = Compile(Model);
        Assert.True(compilation.Success, string.Join('\n', compilation.Diagnostics.Select(_ => _.Message)));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var slices = _plan.Model.Application.Modules.SelectMany(_ => _.Features).SelectMany(_ => _.Slices).ToArray();
        _commandSlice = slices.Single(_ => _.Name == "SetPrice");
        _translationSlice = slices.Single(_ => _.Name == "PublishPrice");
    }
}
