"""Import an FBX into Blender headlessly and print a JSON summary.

Usage:
    blender.exe -b --python tests/interop/validate_blender.py -- <file.fbx>

The last line of stdout is "RESULT {json}" with mesh count, triangle count and
the world-space bounding box (Blender space, Z-up).
"""

import json
import sys

import bpy


def main() -> None:
    argv = sys.argv
    path = argv[argv.index("--") + 1] if "--" in argv else argv[-1]

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)

    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]

    triangles = 0
    mins = [1e30, 1e30, 1e30]
    maxs = [-1e30, -1e30, -1e30]

    for obj in meshes:
        mesh = obj.data
        mesh.calc_loop_triangles()
        triangles += len(mesh.loop_triangles)
        for vertex in mesh.vertices:
            world = obj.matrix_world @ vertex.co
            for axis in range(3):
                mins[axis] = min(mins[axis], world[axis])
                maxs[axis] = max(maxs[axis], world[axis])

    if not meshes:
        mins = [0.0, 0.0, 0.0]
        maxs = [0.0, 0.0, 0.0]

    print("RESULT " + json.dumps({
        "meshes": len(meshes),
        "triangles": triangles,
        "bboxMin": [round(v, 4) for v in mins],
        "bboxMax": [round(v, 4) for v in maxs],
    }))


main()
