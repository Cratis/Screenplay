// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_admitted_read_path_positions : given.a_protected_read_model
{
    [Theory]
    [InlineData("require value == View.value")]
    [InlineData("require View.value == value")]
    [InlineData("require value != \"\" and View.value == value")]
    public void should_bind_requirements_on_either_side_in_command_validate_blocks(string requirement)
    {
        var result = Bind(Source.Replace("require View.value != \"\"", requirement, StringComparison.Ordinal));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Command(result.Value!.Model).Requirements.Length.ShouldEqual(1);
    }

    [Theory]
    [InlineData("String")]
    [InlineData("Uuid")]
    public void should_accept_string_concepts_and_uuid_keys(string primitive)
    {
        var result = Bind($"concept Id : {primitive}\n" + Source.Replace("id String", "id Id", StringComparison.Ordinal));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Command(result.Value!.Model).Reads.Length.ShouldEqual(1);
    }
}
