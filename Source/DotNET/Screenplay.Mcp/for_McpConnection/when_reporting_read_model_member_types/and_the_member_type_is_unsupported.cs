// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reporting_read_model_member_types;

public class and_the_member_type_is_unsupported : given.a_connection
{
    JsonElement _opened;
    JsonElement _diagnostics;
    JsonElement _workspaceDiagnostics;
    JsonElement Diagnostic => _diagnostics.GetProperty("page").GetProperty("items").EnumerateArray().Single();

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "module M\n  feature F\n    slice StateView View\n      readmodel Parent\n        child Child[]\n      readmodel Child\n        value String");
        Initialize();
    }

    void Because()
    {
        _opened = Call("open-workspace").GetProperty("result").GetProperty("structuredContent");
        _diagnostics = Call("diagnostics").GetProperty("result").GetProperty("structuredContent");
        _workspaceDiagnostics = Call("read-workspace", new { expectedRevision = _opened.GetProperty("revision").GetString(), view = "diagnostics" }).GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_refuse_authoring_readiness() => _opened.GetProperty("readiness").GetProperty("authoringAccepted").GetBoolean().ShouldBeFalse();
    [Fact] void should_refuse_executable_readiness() => _opened.GetProperty("readiness").GetProperty("executableReady").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_exactly_one_authoring_diagnostic() => _opened.GetProperty("readiness").GetProperty("authoringDiagnosticCount").GetInt32().ShouldEqual(1);
    [Fact] void should_preserve_the_native_code() => Diagnostic.GetProperty("code").GetString().ShouldEqual("PLAY0273");
    [Fact] void should_preserve_error_severity() => Diagnostic.GetProperty("severity").GetString().ShouldEqual("Error");
    [Fact] void should_preserve_the_member_path() => Diagnostic.GetProperty("location").GetProperty("path").GetString().ShouldEqual("application.play");
    [Fact] void should_preserve_the_member_line() => Diagnostic.GetProperty("location").GetProperty("line").GetInt32().ShouldEqual(5);
    [Fact] void should_preserve_the_member_column() => Diagnostic.GetProperty("location").GetProperty("column").GetInt32().ShouldEqual(15);
    [Fact] void should_explain_the_supported_value_namespace() => Diagnostic.GetProperty("message").GetString().ShouldEqual("Read model 'Parent' member 'child' references unsupported type 'Child'. Use a primitive, concept or top-level 'type'; read-model shapes and slice-local 'type' declarations are not member types.");
    [Fact] void should_report_failed_validation() => _diagnostics.GetProperty("success").GetBoolean().ShouldBeFalse();
    [Fact] void should_preserve_the_same_diagnostic_in_read_workspace() => _workspaceDiagnostics.GetProperty("page").GetProperty("items").EnumerateArray().Single().GetProperty("message").GetString().ShouldEqual(Diagnostic.GetProperty("message").GetString());
}
