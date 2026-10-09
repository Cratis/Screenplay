// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Screenplay.CanonicalVectors.for_RegisterProjectCorpus;

public class when_verifying_the_executed_stage_plan : Specification
{
    CanonicalStageExecutionVector _plan = null!;
    CanonicalCorpusVector _corpus = null!;
    JsonDocument _manifest = null!;
    JsonDocument _scene = null!;
    string _bindings = null!;
    List<string> _unmetAssertions = [];

    void Because()
    {
        _plan = RegisterProjectCorpus.ExecutedStagePlan;
        _corpus = RegisterProjectCorpus.LegacyV1;
        _manifest = JsonDocument.Parse(_plan.Manifest.Text);
        _scene = JsonDocument.Parse(_plan.ScenePlan.Text);
        _bindings = _plan.Bindings.Text;

        foreach (var assertion in _plan.Assertions)
        {
            if (!IsMet(assertion))
            {
                _unmetAssertions.Add($"{assertion.Path} {assertion.Operation} {assertion.Value}");
            }
        }
    }

    bool IsMet(CanonicalScreenAssertion assertion)
    {
        var manifestArtifacts = _manifest.RootElement.GetProperty("artifacts");
        return assertion.Path switch
        {
            "manifest.schemaVersion" => Scalar(_manifest.RootElement, "schemaVersion").Equals(assertion.Value, StringComparison.Ordinal),
            "manifest.artifactPlanSchemaVersion" => Scalar(_manifest.RootElement, "artifactPlanSchemaVersion").Equals(assertion.Value, StringComparison.Ordinal),
            "manifest.target" => Scalar(_manifest.RootElement, "target").Equals(assertion.Value, StringComparison.Ordinal),
            "manifest.renderer" => Scalar(_manifest.RootElement, "renderer").Equals(assertion.Value, StringComparison.Ordinal),
            "manifest.semanticRevision" => Scalar(_manifest.RootElement, "semanticRevision").Equals(assertion.Value, StringComparison.Ordinal),
            "manifest.artifacts.count" => assertion.Operation == "equals" && manifestArtifacts.GetArrayLength().ToString().Equals(assertion.Value, StringComparison.Ordinal),
            "manifest.artifacts.paths" => assertion.Operation == "unique" && manifestArtifacts.EnumerateArray().Select(artifact => artifact.GetProperty("path").GetString()!).Distinct().Count() == manifestArtifacts.GetArrayLength(),
            "manifest.artifacts.sha256" => assertion.Operation == "matches" && manifestArtifacts.EnumerateArray().All(artifact => IsSha256Hex(artifact.GetProperty("sha256").GetString()!)),
            "plan.scene.layout.name" => _scene.RootElement.GetProperty("layouts").EnumerateArray().Any(layout => layout.GetProperty("name").GetString()!.Equals(assertion.Value, StringComparison.Ordinal)),
            "plan.scene.layout.slots" => assertion.Operation == "contains" && _scene.RootElement.GetProperty("layouts").EnumerateArray().Any(layout => layout.GetProperty("slots").EnumerateArray().Any(slot => slot.GetProperty("name").GetString()!.Equals(assertion.Value, StringComparison.Ordinal))),
            "plan.scene.screen.layout" => _scene.RootElement.GetProperty("screens").EnumerateArray().Any(screen => screen.GetProperty("layout").GetString()!.Equals(assertion.Value, StringComparison.Ordinal)),
            "plan.scene.screens.commandForm.command" => CommandFormComponent().Properties.GetProperty("command").GetString()!.Equals(assertion.Value, StringComparison.Ordinal),
            "plan.scene.screens.commandForm.inputs" => assertion.Operation == "contains" && CommandFormComponent().Properties.GetProperty("inputs").EnumerateArray().Any(input => input.GetProperty("property").GetString()!.Equals(assertion.Value, StringComparison.Ordinal)),
            "plan.scene.screens.queryInputForm.id" => assertion.Operation == "startsWith" && AllSceneComponents().Any(component => component.ComponentName.EndsWith(":queryInputForm", StringComparison.Ordinal) && component.Id.StartsWith(assertion.Value, StringComparison.Ordinal)),
            "plan.scene.placeholderComponents" => assertion.Operation == "equals" && AllSceneComponents().Count(component => component.ComponentName.Contains("placeholder", StringComparison.OrdinalIgnoreCase)).ToString().Equals(assertion.Value, StringComparison.Ordinal),
            "bindings.registerQueryIdentity" => assertion.Operation == "contains" && _bindings.Contains(assertion.Value, StringComparison.Ordinal),
            "bindings.package" => assertion.Operation == "contains" && _bindings.Contains(assertion.Value, StringComparison.Ordinal),
            _ => false
        };
    }

    (string ComponentName, string Id, JsonElement Properties) CommandFormComponent()
    {
        var component = AllSceneComponents().Single(component => component.ComponentName.EndsWith(":commandForm", StringComparison.Ordinal));
        return (component.ComponentName, component.Id, component.Properties);
    }

    IEnumerable<(string ComponentName, string Id, JsonElement Properties)> AllSceneComponents()
    {
        foreach (var screen in _scene.RootElement.GetProperty("screens").EnumerateArray())
        {
            foreach (var slot in screen.GetProperty("slotContent").EnumerateObject())
            {
                foreach (var component in slot.Value.EnumerateArray())
                {
                    var id = component.TryGetProperty("id", out var idElement) ? idElement.GetString()! : string.Empty;
                    yield return (component.GetProperty("componentName").GetString()!, id, component.GetProperty("properties"));
                }
            }
        }
    }

    static bool IsSha256Hex(string value) =>
        value.Length == 64 && value.All(character => character is (>= '0' and <= '9') or (>= 'a' and <= 'f'));

    static string Scalar(JsonElement element, string name) => element.GetProperty(name).GetString()!;

    [Fact] void should_pin_the_same_semantic_revision_as_the_corpus() => _plan.SemanticRevision.ShouldEqual(_corpus.SemanticRevision.ToString());

    [Fact] void should_pin_the_same_application_name_as_the_corpus() => _plan.ApplicationName.ShouldEqual(_corpus.ApplicationName);

    [Fact] void should_satisfy_every_authored_assertion() => _unmetAssertions.ShouldBeEmpty();

    [Fact] void should_pin_artifacts_matching_the_manifest_exactly()
    {
        var manifestArtifacts = _manifest.RootElement.GetProperty("artifacts").EnumerateArray()
            .Select(artifact => new ArtifactReference(
                artifact.GetProperty("path").GetString() ?? string.Empty,
                artifact.GetProperty("sha256").GetString() ?? string.Empty))
            .ToArray();
        var pinned = _plan.Artifacts.Select(artifact => new ArtifactReference(artifact.Path, artifact.Sha256)).ToArray();
        manifestArtifacts.Length.ShouldEqual(pinned.Length);
        for (var index = 0; index < pinned.Length; index++)
        {
            manifestArtifacts[index].ShouldEqual(pinned[index]);
        }
    }

    [Fact] void should_pin_the_render_plan_among_the_artifacts() => _plan.Artifacts.Select(artifact => artifact.Path).ShouldContain("scene.json");

    [Fact] void should_pin_the_generated_bindings_among_the_artifacts() => _plan.Artifacts.Select(artifact => artifact.Path).ShouldContain("src/bindings.ts");

    [Fact] void should_pin_the_frontend_application_among_the_artifacts() => _plan.Artifacts.Select(artifact => artifact.Path).ShouldContain(".frontend/main.tsx");

    [Fact]
    void should_pin_a_complete_solution_among_the_artifacts()
    {
        var paths = _plan.Artifacts.Select(artifact => artifact.Path).ToArray();
        paths.ShouldContain("Program.cs");
        paths.ShouldContain("Projects.slnx");
        paths.ShouldContain("Projects.csproj");
        paths.ShouldContain("docker-compose.yml");
    }

    [Fact]
    void should_record_the_exact_released_version_vector()
    {
        _plan.VersionVector.ShouldContain("CLI 3.39.0");
        _plan.VersionVector.ShouldContain("Stage 4.43.0");
    }

    sealed record ArtifactReference(string Path, string Sha256);
}
