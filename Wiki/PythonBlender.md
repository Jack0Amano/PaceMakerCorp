# Python on Blender
メッシュの名前をObject名に統一する  
```
// 選択中のObjectを取得
selected_objects = bpy.context.selected_objects
// メッシュの名前をObjectNameに変える
for o in selected_objects:
    o.data.name = o.name
```
  
選択したObjectをFBXで個々に出力  
```
import bpy
import os

root = os.getcwd() + "\\low"
try:
    os.makedirs(root)
except:
    print("RootDirectory is still exit")
    
selected_objects = bpy.context.selected_objects

for ob in bpy.context.scene.objects:
    # 非選択状態に設定する
    ob.select_set(False)
    
for ob in selected_objects:
    ob.select_set(True)
    path = root + "\\" + (ob.name.replace("_high", ""))
    try:
        os.makedirs(path)
    except:
        print(ob.name)
    path += "\\" + ob.name + ".fbx"
    if os.path.isfile(path):
        os.remove(path)
    bpy.ops.export_scene.fbx(
        filepath=path,
        check_existing=True,
        filter_glob="*.fbx",
        use_selection=True,
        use_active_collection=False,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_NONE',
        bake_space_transform=False,
        object_types={'MESH'},
        use_mesh_modifiers=True,
        use_mesh_modifiers_render=True,
        mesh_smooth_type='OFF',
        use_subsurf=False,
        use_mesh_edges=False,
        use_tspace=False,
        use_custom_props=False,
        add_leaf_bones=True,
        primary_bone_axis='Y',
        secondary_bone_axis='X',
        use_armature_deform_only=False,
        armature_nodetype='NULL',
        bake_anim=False,
        bake_anim_use_all_bones=False,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=False,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=1.0,
        path_mode='AUTO',
        embed_textures=False,
        batch_mode='OFF',
        use_batch_own_dir=True,
        use_metadata=True,
        axis_forward='-Z',
        axis_up='Y'
    )
    ob.select_set(False)
```
  
すべてのMaterialを削除する  
```
import bpy
import os

for ob in bpy.context.selected_editable_objects:
    ob.active_material_index = 0
    for i in range(len(ob.material_slots)):
        bpy.ops.object.material_slot_remove({'object': ob})
```