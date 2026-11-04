# ============================================================
#  Solar Explorer VR - génère les modèles 3D dans Blender
#  Comment l'utiliser :
#   1. Ouvre Blender (une scène vide ou la scène par défaut)
#   2. Onglet "Scripting" > "Open" > choisis ce fichier
#   3. (Optionnel) change OUT_DIR ci-dessous
#   4. Clique sur le bouton "Run Script" (triangle)
#  Résultat : Planete.fbx et Saturne.fbx dans OUT_DIR.
#  Copie-les ensuite dans Assets/Models du projet Unity.
# ============================================================
import bpy, bmesh, math, os

# Dossier de sortie (par défaut : un dossier sur le Bureau)
OUT_DIR = os.path.join(os.path.expanduser("~"), "Desktop", "SolarExplorer_Models")
os.makedirs(OUT_DIR, exist_ok=True)


def clear_scene():
    """Supprime tous les objets de la scène."""
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)


def make_sphere(name):
    """Sphère de rayon 1 m, lisse, avec un matériau (la UV est automatique)."""
    bpy.ops.mesh.primitive_uv_sphere_add(segments=64, ring_count=32, radius=1, location=(0, 0, 0))
    obj = bpy.context.active_object
    obj.name = name
    bpy.ops.object.shade_smooth()
    mat = bpy.data.materials.new(name + "_mat")
    obj.data.materials.append(mat)
    return obj


def make_ring(name="SaturnRing", inner=1.3, outer=2.3, seg=128):
    """Anneau plat avec une UV radiale : u = distance au centre, v = angle."""
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new()
    vin, vout = [], []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        vin.append(bm.verts.new((math.cos(a) * inner, math.sin(a) * inner, 0)))
        vout.append(bm.verts.new((math.cos(a) * outer, math.sin(a) * outer, 0)))
    for i in range(seg):
        j = (i + 1) % seg
        f = bm.faces.new((vin[i], vout[i], vout[j], vin[j]))
        coords = [(0, i / seg), (1, i / seg), (1, (i + 1) / seg), (0, (i + 1) / seg)]
        for loop, c in zip(f.loops, coords):
            loop[uv].uv = c
    bm.to_mesh(mesh)
    bm.free()
    obj.data.materials.append(bpy.data.materials.new(name + "_mat"))
    return obj


def export(objs, filename):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    path = os.path.join(OUT_DIR, filename)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        bake_space_transform=True,   # = "Apply Transform"
        object_types={'MESH'},
    )
    print("Exporté :", path)


# --- Planete.fbx : une sphère réutilisée pour le Soleil et les 8 planètes ---
clear_scene()
planet = make_sphere("Planete")
export([planet], "Planete.fbx")

# --- Saturne.fbx : sphère + anneau (le nom "SaturnRing" est reconnu par Unity) ---
clear_scene()
saturne = make_sphere("Saturne")
ring = make_ring("SaturnRing")
export([saturne, ring], "Saturne.fbx")

print("Terminé ! Fichiers dans :", OUT_DIR)
