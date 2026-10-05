# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
import json
import tempfile
import unittest
from pathlib import Path
import jsonschema
from build import HERE, RIDS, prepare


class Packaging(unittest.TestCase):
    def test_each_platform_uses_the_same_version_and_bundled_binary(self):
        for rid, platform in RIDS.items():
            with self.subTest(rid=rid), tempfile.TemporaryDirectory() as folder:
                root = Path(folder)
                publish = root / "publish"
                publish.mkdir()
                binary = "Cratis.Screenplay.Tool" + (".exe" if rid == "win-x64" else "")
                (publish / binary).write_bytes(b"self-contained fixture")
                executable = prepare(publish, root / "packages", "4.55.0", rid)
                bundle = json.loads((root / "packages/mcpb/manifest.json").read_text())
                plugin = json.loads((root / "packages/plugin/plugin.json").read_text())
                mcp = json.loads((root / "packages/plugin/mcp.json").read_text())
                for name, document in [("plugin", plugin), ("mcp", mcp)]:
                    jsonschema.Draft202012Validator(json.loads((HERE / "Schemas" / f"{name}.schema.json").read_text())).validate(document)
                self.assertEqual(bundle["version"], plugin["version"])
                self.assertEqual(bundle["compatibility"]["platforms"], [platform])
                self.assertEqual(bundle["server"]["entry_point"], f"server/{executable}")
                self.assertEqual(mcp["mcpServers"]["screenplay"]["command"], f"./server/{executable}")
                self.assertNotIn("user_config", bundle)
                self.assertEqual(bundle["server"]["mcp_config"]["args"], ["mcp"])
                self.assertTrue((root / "packages/plugin/skills/screenplay/SKILL.md").is_file())
                self.assertEqual((root / "packages/mcpb/server" / executable).read_bytes(), (publish / binary).read_bytes())

    def test_rejects_missing_binary(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaises(ValueError):
                prepare(Path(folder), Path(folder) / "packages", "4.55.0", "osx-arm64")

    def test_rejects_unsupported_platform_and_invalid_version(self):
        for version, rid in [("4.55.0", "win-arm64"), ("../evil", "osx-arm64")]:
            with self.assertRaises(ValueError):
                prepare(Path("."), Path("."), version, rid)


if __name__ == "__main__":
    unittest.main()
