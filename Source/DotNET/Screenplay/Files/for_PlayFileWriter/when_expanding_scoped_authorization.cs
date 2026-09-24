// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileWriter;

public class when_expanding_scoped_authorization : Specification
{
    const string Source =
        """
        policy Access
          require authenticated

        policy Staff
          require authenticated

        policy Extra
          require authenticated

        module Portal
          authorize Access
          feature Orders
            authorize Staff
            feature Returns
              authorize Extra
              slice StateChange ReturnOrder
                command RequestReturn
        """;

    ApplicationCompilation<ApplicationSyntax> _recompiled;
    IEnumerable<PlayFileContent> _files;
    DirectoryInfo _root;

    void Establish() => _root = Directory.CreateTempSubdirectory("playauth");

    void Because()
    {
        var application = new ScreenplayCompiler().Compile(Source).Value!;
        var writer = new PlayFileWriter();
        _files = writer.Expand(application);
        writer.WriteTo(application, _root.FullName);
        _recompiled = new PlayFileCompiler().CompileFolder(_root.FullName);
    }

    [Fact] void should_emit_module_gate_once() => _files.Count(_ => _.Content.Contains("authorize Access", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_emit_feature_gate_once() => _files.Count(_ => _.Content.Contains("authorize Staff", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_emit_nested_gate_once() => _files.Count(_ => _.Content.Contains("authorize Extra", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_keep_all_gates_after_merging() => _recompiled.Result.Value!.Modules.Single().Features.Single().Features.Single().Authorize!.References().Single().Name.ShouldEqual("Extra");
    [Fact] void should_not_report_duplicate_gates() => _recompiled.Result.Diagnostics.ShouldBeEmpty();

    void Destroy() => _root.Delete(true);
}
