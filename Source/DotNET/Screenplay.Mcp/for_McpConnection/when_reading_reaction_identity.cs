// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reading_reaction_identity : Specification
{
    McpSnapshot _snapshot;
    JsonElement _identity;
    McpAuthoringReadiness _readiness;
    ReactionSyntax _reaction;

    void Establish()
    {
        const string Source = "policy Local\n  require role \"Local\"\npolicy External\n  require role \"External\"\nmodule M\n  feature F\n    slice Automation S\n      command C\n        authorize Local\n      reaction R\n        runs as system role \"Local\" and role \"External\"\n        every 1 day\n          invokes C\n          invokes ExternalCommand\nmodule Other\n  feature F\n    slice StateChange S\n      command ExternalCommand\n        authorize External";
        _snapshot = new(new Dictionary<string, string> { ["application.play"] = Source });
        _reaction = _snapshot.Compilation.Value!.Modules.First().Features.Single().Slices.Single().Reactions.Single();
        _readiness = _snapshot.Index.Readiness;
    }

    void Because() => _identity = JsonSerializer.SerializeToElement(McpDeclarationDetails.Read(_snapshot, JsonSerializer.SerializeToElement(new { address = "M.F.S.R", kind = "Reaction" })), McpJson.Options).GetProperty("details").GetProperty("runsAs");

    [Fact] void should_expose_system_kind() => _identity.GetProperty("kind").GetString().ShouldEqual("system");
    [Fact] void should_expose_exact_roles() => _identity.GetProperty("roles").EnumerateArray().Select(role => role.GetString()).ShouldContainOnly("Local", "External");
    [Fact] void should_disclose_only_cross_boundary_roles() => _identity.GetProperty("crossBoundaryRoles").EnumerateArray().Select(role => role.GetString()).ShouldContainOnly("External");
    [Fact] void should_expose_source_location() => _identity.GetProperty("location").GetProperty("line").GetInt32().ShouldEqual(11);
    [Fact] void should_name_the_admission_issue() => _identity.GetProperty("readiness").GetString().ShouldContain("#383");
    [Fact] void should_disclose_clock_and_application_trigger_readiness() => _identity.GetProperty("readiness").GetString().ShouldContain("Clock and application triggers");
    [Fact] void should_mark_the_reaction_as_syntax_only() => _readiness.SyntaxOnly(_reaction).ShouldBeTrue();
    [Fact] void should_mark_the_model_as_syntax_only() => _readiness.ModelSyntaxOnly.ShouldBeTrue();
    [Fact] void should_name_reaction_identity_in_readiness() => _readiness.ExecutionReadiness(_reaction).ShouldContain("reaction command identity");
    [Fact] void should_publish_the_init_member_in_syntax_schema() => SyntaxSchema.For(nameof(ReactionSyntax)).GetProperty("properties").TryGetProperty("runsAs", out _).ShouldBeTrue();
}
