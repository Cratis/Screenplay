// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

/// <summary>
/// Printed output of the hint-free replacement matrix, captured from the merge-base implementation (8875798).
/// </summary>
static class PrinterMatrixExpected
{
    public static readonly Dictionary<string, string> Outputs = new()
    {
        ["rules/CanonicalizeTouchedDocuments/unchanged"] = "module M\n\n  feature F\n\n    slice StateChange S\n\n      command C // command\n        label String\n        validate // block A\n          label rule Check // first\n            file Same.cs // file first\n          label rule Check // second\n            file Same.cs // file second\n",
        ["rules/CanonicalizeTouchedDocuments/rename-first"] = "module M\n\n  feature F\n\n    slice StateChange S\n\n      command C // command\n        label String\n        validate // block A\n          label rule Renamed // first\n            file Same.cs // file first\n          label rule Check // second\n            file Same.cs // file second\n",
        ["rules/CanonicalizeTouchedDocuments/rename-second"] = "module M\n\n  feature F\n\n    slice StateChange S\n\n      command C // command\n        label String\n        validate // block A\n          label rule Check // first\n            file Same.cs // file first\n          label rule Renamed // second\n            file Same.cs // file second\n",
        ["rules/CanonicalizeTouchedDocuments/insert-third"] = "module M\n\n  feature F\n\n    slice StateChange S\n\n      command C // command\n        label String\n        validate // block A\n          label rule Check // first\n            file Same.cs // file first\n          label rule Check // second\n            file Same.cs // file second\n          label rule Renamed\n            file Same.cs\n",
        ["rules/CanonicalizeTouchedDocuments/delete-first"] = "module M\n\n  feature F\n\n    slice StateChange S\n\n      command C // command\n        label String\n        validate // block A\n          label rule Check // first\n            file Same.cs // file first\n",
        ["rules/CanonicalizeTouchedDocuments/delete-second"] = "module M\n\n  feature F\n\n    slice StateChange S\n\n      command C // command\n        label String\n        validate // block A\n          label rule Check // first\n            file Same.cs // file first\n",
        ["blocks/CanonicalizeTouchedDocuments/unchanged"] = "module M\n\n  feature F\n\n    slice StateChange S\n\n      command C // command\n        label String\n        validate // block one\n          label rule Check // one rule\n            file Same.cs // one file\n        validate // block two\n          label rule Check // two rule\n            file Same.cs // two file\n",
        ["blocks/CanonicalizeTouchedDocuments/append-third"] = "module M\n\n  feature F\n\n    slice StateChange S\n\n      command C // command\n        label String\n        validate // block one\n          label rule Check // one rule\n            file Same.cs // one file\n        validate // block two\n          label rule Check // two rule\n            file Same.cs // two file\n        validate\n          label rule Renamed\n            file Same.cs\n",
        ["blocks/CanonicalizeTouchedDocuments/delete-first"] = "module M\n\n  feature F\n\n    slice StateChange S\n\n      command C // command\n        label String\n        validate // block one\n          label rule Check // one rule\n            file Same.cs // one file\n",
        ["blocks/CanonicalizeTouchedDocuments/delete-second"] = "module M\n\n  feature F\n\n    slice StateChange S\n\n      command C // command\n        label String\n        validate // block one\n          label rule Check // one rule\n            file Same.cs // one file\n",
        ["blocks/CanonicalizeTouchedDocuments/rename-first-rule"] = "module M\n\n  feature F\n\n    slice StateChange S\n\n      command C // command\n        label String\n        validate // block one\n          label rule Renamed // one rule\n            file Same.cs // one file\n        validate // block two\n          label rule Check // two rule\n            file Same.cs // two file\n",
        ["rules/PreserveTrivia/unchanged"] = "NOOP",
        ["rules/PreserveTrivia/rename-first"] = "REFUSED",
        ["rules/PreserveTrivia/rename-second"] = "REFUSED",
        ["rules/PreserveTrivia/insert-third"] = "REFUSED",
        ["rules/PreserveTrivia/delete-first"] = "REFUSED",
        ["rules/PreserveTrivia/delete-second"] = "REFUSED",
        ["blocks/PreserveTrivia/unchanged"] = "NOOP",
        ["blocks/PreserveTrivia/append-third"] = "REFUSED",
        ["blocks/PreserveTrivia/delete-first"] = "REFUSED",
        ["blocks/PreserveTrivia/delete-second"] = "REFUSED",
        ["blocks/PreserveTrivia/rename-first-rule"] = "REFUSED",
    };
}
