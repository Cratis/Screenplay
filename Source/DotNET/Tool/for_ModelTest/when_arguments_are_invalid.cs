// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelTest;

public class when_arguments_are_invalid : given.a_model
{
    [Theory]
    [InlineData("--unknown")]
    [InlineData("--filter")]
    [InlineData("--filter", "Missing")]
    [InlineData("--format", "yaml")]
    [InlineData("--format", "json", "--format", "text")]
    [InlineData("--filter", "M.F.Register.Correct", "--filter", "M.F.Register.Wrong")]
    void should_refuse_usage_errors(params string[] arguments) => ModelTest.Run([Root, .. arguments], Output, Error).ShouldEqual(2);
}
