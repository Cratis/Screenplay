// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides the executed Stage render evidence for the canonical RegisterProject legacy corpus.
/// </summary>
public static partial class RegisterProjectCorpus
{
    const string StageResourcePrefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.RegisterProject.v1_legacy.stage._4._43._0";

    /// <summary>
    /// Gets the executed Stage render of the legacy source form on the exact released screens vector.
    /// </summary>
    public static CanonicalStageExecutionVector ExecutedStagePlan { get; } = new()
    {
        Name = "register-project/v1-legacy/stage-4.43.0",
        ApplicationName = "Projects",
        Target = "cratis",
        TargetVersion = "22.50.5",
        Renderer = "Cratis.Stage.Rendering.Cratis",
        RendererVersion = "1",
        SemanticRevision = "rev1:ebe0c17eebc8aa64c573afe1ee639298e2874dd33406b8a9a663dd70aad820e4",
        VersionVector = "Screenplay 4.97.0 + CLI 3.39.0 (bundled compiler 4.93.0) + Stage 4.43.0 (cratis/stage:4.43.0) + Scene 4.10.0 + Studio 0.135.1",
        Manifest = Document("stage-render-manifest", "stage/4.43.0/cratis-render.json", $"{StageResourcePrefix}.cratis-render.json"),
        ScenePlan = Document("stage-scene-plan", "stage/4.43.0/scene.json", $"{StageResourcePrefix}.scene.json"),
        Bindings = Document("stage-typed-bindings", "stage/4.43.0/bindings.ts", $"{StageResourcePrefix}.bindings.ts"),
        Artifacts =
        [
            Artifact(".frontend/index.css", "c43098e7d8431ead00fe49644e97b14cbdb4e97a994850e810acb8a801bc24b2", 436),
            Artifact(".frontend/index.html", "9001e43194105a4b49a847bc562647c454a931a6575646d9e5235a6c061160b0", 327),
            Artifact(".frontend/main.tsx", "e22844dd5715b58b06cb63cfd4a3fd6a610aebeaf623f0e60e4c0e7f02943ef6", 3308),
            Artifact(".frontend/tsconfig.json", "8188daef31c13c81639fd8e50c46f580322575bb5f34d033b486df2a14366e21", 939),
            Artifact(".frontend/tsconfig.node.json", "3b2517722082109462da51cbd66142f937bfcbf3f7ce89aeab9991ff12140342", 290),
            Artifact(".frontend/vite.config.ts", "54adb7473f5e230c9ded78b7549357a6e7abb4a547cf11d6094d2742df4002e5", 1088),
            Artifact(".gitignore", "74a92d28e98b0f259cc5b02bf9eac97fe103b30a10746f0315fba7fa0bfb0df9", 253),
            Artifact("Common/ProjectId.cs", "679cf35bd307d4aec2353c2457c76c6b2e29edbab60212aa2f59cb98da82f1eb", 660),
            Artifact("Common/ProjectName.cs", "11b0f6bf30644aeef78c920f76921848e1df10b94a1f7139a7c6b58afe5aae2b", 577),
            Artifact("Directory.Build.props", "17284cb74605517cbec8a8153505da71a685b28694b67b5ef80608cd79b1c0ea", 12),
            Artifact("Directory.Build.targets", "17284cb74605517cbec8a8153505da71a685b28694b67b5ef80608cd79b1c0ea", 12),
            Artifact("Directory.Packages.props", "af799a176bf7be0de5e660924f4bde3444170788e9633399a1f65f0b72c9602f", 133),
            Artifact("GeneratedPolicyRegistration.cs", "b57e45477a56d13da38a2ae0403485fc42a8c3ad475915f22e978179f4d9ee04", 740),
            Artifact("Program.cs", "11c06f1db1225ad04204050bcd4d1a4cb3ca0f92fa5e9f838f4186d33bf89c6c", 994),
            Artifact("Projects.csproj", "de903e5b8123792cd04e0c96dd3bbe25c9af53862aa33741297ace222c1d1295", 2377),
            Artifact("Projects.slnx", "db5c02ba1f186365950aa69c76412f2f175514175748ae5f2f3e94afb7bdabc5", 60),
            Artifact("Projects/Registration/ProjectLookup/ProjectLookup.cs", "f52fbb0c9741f3dc969aff3a0a5383b04359598f153cdffb7a8ea94722097049", 1186),
            Artifact("Projects/Registration/RegisterProject/RegisterProject.cs", "40dd2ea586581626b38ee1bfbf1e0b437250197b3bdd3b9bec6226b420b0d900", 1629),
            Artifact("Projects/Registration/RegisterProject/when_registering_aproject.cs", "d363026e9d56b3b6276c94af3b813ad9a03bc724553ba50ff45bc2a2fbf3ff14", 1887),
            Artifact("Projects/Registration/RegisterProject/when_registering_aproject_is_projected.cs", "2e8880f847912d7bebe6fc307ce79c0a1180cc1f104c25f626b91ce28bbf7566", 1781),
            Artifact("Projects/Registration/RegisterProject/when_registering_aproject_is_queried.cs", "de23bc497ced0307ce971c9e41113019d59289a5078d1ebc8cbc3008c8cff30c", 2595),
            Artifact("Projects/Registration/RegisterProject/when_rejecting_an_empty_project_name.cs", "79c9f5738df7b298e55214d17f1c73bc13e9c0747250c0830adba6f11795ffdc", 1725),
            Artifact("appsettings.json", "fab4d53ed0e88ffa0ad0dbbea7022a0c5fd88fde0c60048b1e6d7b8caeae04ac", 438),
            Artifact("docker-compose.yml", "d780b4894c6936918f81d04adeef5444c3a7b589883a5fefb06762a6308f2222", 259),
            Artifact("package.json", "4379a8e8f08e8b173693678f4ba988e7da708791ecb1ac42492d1da7e24df329", 1429),
            Artifact("scene.json", "4823dc11c628f99a3ead73006dc330bc9f54409b0cf05d60c58379cdc16a5396", 1556),
            Artifact("src/bindings.ts", "4823d01d04ac795068ac52d87afc3e7be57c826dda63b005f1c3e7a057196f2f", 569),
            Artifact("tsconfig.json", "d52a81eda975fa981b1cda931dfec65026821a2a753f29e643e5247bb0a312bb", 47)
        ],
        Assertions =
        [
            Assertion("manifest.schemaVersion", "equals", "1"),
            Assertion("manifest.artifactPlanSchemaVersion", "equals", "1"),
            Assertion("manifest.target", "equals", "cratis"),
            Assertion("manifest.renderer", "equals", "Cratis.Stage.Rendering.Cratis"),
            Assertion("manifest.semanticRevision", "equals", "rev1:ebe0c17eebc8aa64c573afe1ee639298e2874dd33406b8a9a663dd70aad820e4"),
            Assertion("manifest.artifacts.count", "equals", "28"),
            Assertion("manifest.artifacts.paths", "unique", "true"),
            Assertion("manifest.artifacts.sha256", "matches", "^[0-9a-f]{64}$"),
            Assertion("plan.scene.layout.name", "equals", "Application"),
            Assertion("plan.scene.layout.slots", "contains", "content"),
            Assertion("plan.scene.screen.layout", "equals", "Application"),
            Assertion("plan.scene.screens.commandForm.command", "equals", "RegisterProject"),
            Assertion("plan.scene.screens.commandForm.inputs", "contains", "name"),
            Assertion("plan.scene.screens.queryInputForm.id", "startsWith", "query:sem1:"),
            Assertion("plan.scene.placeholderComponents", "equals", "0"),
            Assertion("bindings.registerQueryIdentity", "contains", "ProjectById"),
            Assertion("bindings.package", "contains", "@cratis/scene.components")
        ]
    };

    static CanonicalStageArtifactExpectation Artifact(string path, string sha256, int byteCount) => new()
    {
        Path = path,
        Sha256 = sha256,
        ByteCount = byteCount
    };

    static CanonicalScreenAssertion Assertion(string path, string operation, string value) => new()
    {
        Path = path,
        Operation = operation,
        Value = value
    };
}
