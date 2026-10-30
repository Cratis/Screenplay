// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_read_model_member_types.given;

public class a_member : Specification
{
    protected const string Message = "Read model 'Parent' member 'child' references unsupported type 'Child'. Use a primitive, concept or top-level 'type'; read-model shapes and slice-local 'type' declarations are not member types.";
    protected ScreenplayCompiler Compiler;
    protected CompilationResult<Syntax.ApplicationSyntax> Result;
    protected virtual string Declarations => string.Empty;
    protected virtual string MemberType => "Child[]";
    protected virtual string SliceDeclarations => "      readmodel Child\n        value String\n";
    protected virtual string Siblings => string.Empty;
    protected string Source => "module M\n  feature F\n    slice StateView View\n      readmodel Parent\n        child " + MemberType + "\n" + SliceDeclarations + Siblings + Declarations;

    void Establish() => Compiler = new();
}
