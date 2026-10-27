"""Import an FBX into a headless Unreal Engine project and print a JSON summary.

Run with:
    UnrealEditor-Cmd.exe <project.uproject> -run=pythonscript \
        -script="<this file>" -unattended -nop4 -nosplash -nullrhi

The FBX path comes from the STARRYSKY_FBX environment variable.
"""

import json
import os

import unreal


def main() -> None:
    fbx = os.environ.get("STARRYSKY_FBX", "")
    if not fbx or not os.path.exists(fbx):
        print("RESULT " + json.dumps({"error": "STARRYSKY_FBX missing"}))
        return

    destination = "/Game/Validation"
    task = unreal.AssetImportTask()
    task.set_editor_property("filename", fbx)
    task.set_editor_property("destination_path", destination)
    task.set_editor_property("destination_name", "")
    task.set_editor_property("automated", True)
    task.set_editor_property("replace_existing", True)
    task.set_editor_property("save", False)

    unreal.AssetToolsHelpers.get_asset_tools().import_asset_tasks([task])

    paths = [str(p) for p in task.get_editor_property("imported_object_paths")]

    meshes = 0
    triangles = 0
    for path in paths:
        asset = unreal.load_asset(path)
        if isinstance(asset, unreal.StaticMesh):
            meshes += 1
            triangles += asset.get_num_triangles(0)

    print("RESULT " + json.dumps({
        "meshes": meshes,
        "triangles": triangles,
        "assets": paths,
    }))


main()
