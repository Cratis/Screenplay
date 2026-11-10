// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Tool.for_ExportCommand;

[PerFactSpecificationLifecycle]
public class when_exporting_the_capability_manifest : given.a_renderable_model
{
    string _target;
    string SourcePath => Path.Combine(Root, "application.play");

    void Establish() => _target = Path.Combine(Root, "capabilities.json");

    [Fact]
    void should_write_stdout_without_a_banner()
    {
        Expose();
        var output = new StringWriter();
        ExportCommand.Run(["--format", "capability-manifest", SourcePath], output, new StringWriter()).ShouldEqual(0);
        using var document = JsonDocument.Parse(output.ToString());
        document.RootElement.GetProperty("operations").GetArrayLength().ShouldEqual(1);
        output.ToString().EndsWith("\n", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    void should_atomically_write_bytes_and_leave_an_identical_file_untouched()
    {
        Expose();
        Export().ShouldEqual(0);
        var bytes = File.ReadAllBytes(_target);
        bytes.Take(3).SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF }).ShouldBeFalse();
        var timestamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(_target, timestamp);
        Export().ShouldEqual(0);
        File.ReadAllBytes(_target).ShouldEqual(bytes);
        File.GetLastWriteTimeUtc(_target).ShouldEqual(timestamp);
        Directory.GetFiles(Root, "*.tmp").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("no-agents")]
    [InlineData("syntax-error")]
    [InlineData("missing-reference")]
    [InlineData("description-too-long")]
    [InlineData("unrelated-error")]
    void should_remove_a_stale_target_and_emit_no_stdout_on_refusal(string failure)
    {
        Expose();
        Export().ShouldEqual(0);
        var source = File.ReadAllText(SourcePath);
        source = failure switch
        {
            "no-agents" => source[..source.IndexOf("\nagents", StringComparison.Ordinal)],
            "syntax-error" => source + "\n  tokens unknown",
            "missing-reference" => source.Replace("command RegisterProject\n    scope", "command Missing\n    scope", StringComparison.Ordinal),
            "description-too-long" => source.Replace("Register a project", new string('x', 1025), StringComparison.Ordinal),
            _ => source + "\nunknown root"
        };
        File.WriteAllText(SourcePath, source);
        var output = new StringWriter();
        var error = new StringWriter();
        ExportCommand.Run(["--format", "capability-manifest", "--output", _target, SourcePath], output, error).ShouldEqual(1);
        File.Exists(_target).ShouldBeFalse();
        output.ToString().ShouldEqual(string.Empty);
        error.ToString().ShouldContain("Removed stale capability manifest");
        if (failure == "description-too-long") error.ToString().ShouldContain("Projects.Registration.RegisterProject.RegisterProject");
    }

    [Fact]
    void should_emit_an_explicit_empty_block()
    {
        File.AppendAllText(SourcePath, "\nagents\n  tokens personal\n");
        Export().ShouldEqual(0);
        using var document = JsonDocument.Parse(File.ReadAllBytes(_target));
        document.RootElement.GetProperty("operations").GetArrayLength().ShouldEqual(0);
    }

    [Theory]
    [InlineData("--scope", "Projects")]
    [InlineData("--screens", "")]
    void should_reject_partial_or_screen_exports(string option, string value)
    {
        Expose();
        var arguments = new List<string> { "--format", "capability-manifest", option };
        if (value.Length > 0) arguments.Add(value);
        arguments.Add(SourcePath);
        ExportCommand.Run(arguments.ToArray(), new StringWriter(), new StringWriter()).ShouldEqual(2);
    }

    int Export() => ExportCommand.Run(["--format", "capability-manifest", "--output", _target, SourcePath], new StringWriter(), new StringWriter());

    void Expose()
    {
        var source = File.ReadAllText(SourcePath).Replace("module Projects", "policy Staff\n  require role \"Staff\"\nmodule Projects\n  authorize Staff", StringComparison.Ordinal);
        File.WriteAllText(SourcePath, source + "\nagents\n  tokens personal\n  command RegisterProject\n    scope write\n    description \"Register a project\"\n", new UTF8Encoding(false));
    }
}
