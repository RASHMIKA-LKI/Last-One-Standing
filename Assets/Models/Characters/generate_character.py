import bpy
import math

def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def create_material(name, color, roughness=0.5):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs['Base Color'].default_value = color
        bsdf.inputs['Roughness'].default_value = roughness
    return mat

def build_character():
    clear_scene()

    # Materials
    mat_skin = create_material("Mat_Skin", (0.87, 0.65, 0.52, 1.0), roughness=0.6)
    mat_hoodie = create_material("Mat_Hoodie", (0.15, 0.35, 0.65, 1.0), roughness=0.4)
    mat_hoodie_accent = create_material("Mat_Accent", (0.9, 0.45, 0.1, 1.0), roughness=0.4)
    mat_pants = create_material("Mat_Pants", (0.1, 0.12, 0.18, 1.0), roughness=0.7)
    mat_shoes = create_material("Mat_Shoes", (0.85, 0.85, 0.85, 1.0), roughness=0.3)
    mat_hair = create_material("Mat_Hair", (0.1, 0.07, 0.05, 1.0), roughness=0.8)
    mat_backpack = create_material("Mat_Backpack", (0.2, 0.22, 0.2, 1.0), roughness=0.6)

    parts = []

    # 1. Torso / Hoodie (Center ~1.2m, height ~0.55m, width ~0.42m, depth ~0.26m)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 1.22))
    torso = bpy.context.active_object
    torso.name = "Body_Torso"
    torso.scale = (0.42, 0.26, 0.52)
    bpy.ops.object.transform_apply(scale=True)
    torso.data.materials.append(mat_hoodie)
    # Add bevel for smooth stylized game look
    bev = torso.modifiers.new("Bevel", "BEVEL")
    bev.width = 0.03
    bev.segments = 2
    parts.append(torso)

    # 2. Hoodie Pocket (Front)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -0.14, 1.1))
    pocket = bpy.context.active_object
    pocket.name = "Body_Pocket"
    pocket.scale = (0.28, 0.04, 0.16)
    bpy.ops.object.transform_apply(scale=True)
    pocket.data.materials.append(mat_hoodie_accent)
    parts.append(pocket)

    # 3. Backpack (Survival gear)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.18, 1.25))
    bp = bpy.context.active_object
    bp.name = "Body_Backpack"
    bp.scale = (0.34, 0.16, 0.42)
    bpy.ops.object.transform_apply(scale=True)
    bp.data.materials.append(mat_backpack)
    bev_bp = bp.modifiers.new("Bevel", "BEVEL")
    bev_bp.width = 0.03
    bev_bp.segments = 2
    parts.append(bp)

    # 4. Neck
    bpy.ops.mesh.primitive_cylinder_add(radius=0.08, depth=0.12, location=(0, 0, 1.52))
    neck = bpy.context.active_object
    neck.name = "Body_Neck"
    neck.data.materials.append(mat_skin)
    parts.append(neck)

    # 5. Head
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.13, location=(0, 0, 1.68))
    head = bpy.context.active_object
    head.name = "Body_Head"
    head.scale = (1.0, 1.05, 1.15)
    bpy.ops.object.transform_apply(scale=True)
    head.data.materials.append(mat_skin)
    parts.append(head)

    # 6. Hair / Cap
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.14, location=(0, 0.02, 1.73))
    hair = bpy.context.active_object
    hair.name = "Body_Hair"
    hair.scale = (1.02, 1.02, 0.8)
    bpy.ops.object.transform_apply(scale=True)
    hair.data.materials.append(mat_hair)
    parts.append(hair)

    # 7. Arms (A-Pose / T-Pose transition: standard 45-degree A-pose is ideal for animation & rigging)
    for side, sign in [("L", 1), ("R", -1)]:
        # Shoulder / Upper Arm
        bpy.ops.mesh.primitive_cylinder_add(radius=0.07, depth=0.28, location=(sign * 0.32, 0, 1.35))
        upper_arm = bpy.context.active_object
        upper_arm.name = f"UpperArm_{side}"
        upper_arm.rotation_euler = (0, sign * math.radians(45), 0)
        bpy.ops.object.transform_apply(rotation=True)
        upper_arm.data.materials.append(mat_hoodie)
        parts.append(upper_arm)

        # Forearm
        bpy.ops.mesh.primitive_cylinder_add(radius=0.06, depth=0.28, location=(sign * 0.52, 0, 1.15))
        forearm = bpy.context.active_object
        forearm.name = f"Forearm_{side}"
        forearm.rotation_euler = (0, sign * math.radians(45), 0)
        bpy.ops.object.transform_apply(rotation=True)
        forearm.data.materials.append(mat_skin)
        parts.append(forearm)

        # Hand / Glove
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.065, location=(sign * 0.65, 0, 1.02))
        hand = bpy.context.active_object
        hand.name = f"Hand_{side}"
        hand.scale = (1.0, 0.8, 1.2)
        bpy.ops.object.transform_apply(scale=True)
        hand.data.materials.append(mat_skin)
        parts.append(hand)

    # 8. Pelvis / Hips
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0.92))
    hips = bpy.context.active_object
    hips.name = "Body_Hips"
    hips.scale = (0.38, 0.24, 0.16)
    bpy.ops.object.transform_apply(scale=True)
    hips.data.materials.append(mat_pants)
    parts.append(hips)

    # 9. Legs & Shoes
    for side, sign in [("L", 1), ("R", -1)]:
        # Thigh
        bpy.ops.mesh.primitive_cylinder_add(radius=0.085, depth=0.38, location=(sign * 0.14, 0, 0.72))
        thigh = bpy.context.active_object
        thigh.name = f"Thigh_{side}"
        thigh.data.materials.append(mat_pants)
        parts.append(thigh)

        # Shin / Calf
        bpy.ops.mesh.primitive_cylinder_add(radius=0.075, depth=0.38, location=(sign * 0.14, 0, 0.36))
        shin = bpy.context.active_object
        shin.name = f"Shin_{side}"
        shin.data.materials.append(mat_pants)
        parts.append(shin)

        # Shoe / Sneaker
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(sign * 0.14, -0.05, 0.08))
        shoe = bpy.context.active_object
        shoe.name = f"Shoe_{side}"
        shoe.scale = (0.14, 0.28, 0.14)
        bpy.ops.object.transform_apply(scale=True)
        shoe.data.materials.append(mat_shoes)
        bev_s = shoe.modifiers.new("Bevel", "BEVEL")
        bev_s.width = 0.02
        bev_s.segments = 2
        parts.append(shoe)

    # Apply all modifiers and join into a clean single mesh with vertex groups
    bpy.ops.object.select_all(action='DESELECT')
    for p in parts:
        p.select_set(True)
        bpy.context.view_layer.objects.active = p
        for m in list(p.modifiers):
            bpy.ops.object.modifier_apply(modifier=m.name)

    # Set active object to torso and join
    bpy.context.view_layer.objects.active = torso
    bpy.ops.object.join()
    char_mesh = bpy.context.active_object
    char_mesh.name = "HostelSurvivor_Mesh"

    # Smooth shading
    for poly in char_mesh.data.polygons:
        poly.use_smooth = True

    # 10. Create Standard Humanoid Armature (Rig)
    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    armature_obj = bpy.context.active_object
    armature_obj.name = "HostelSurvivor_Armature"
    arm_data = armature_obj.data
    arm_data.name = "ArmatureData"

    edit_bones = arm_data.edit_bones
    # Base default bone -> Hips
    hips_b = edit_bones[0]
    hips_b.name = "Hips"
    hips_b.head = (0, 0, 0.92)
    hips_b.tail = (0, 0, 1.1)

    # Spine
    spine_b = edit_bones.new("Spine")
    spine_b.head = (0, 0, 1.1)
    spine_b.tail = (0, 0, 1.3)
    spine_b.parent = hips_b

    # Chest
    chest_b = edit_bones.new("Chest")
    chest_b.head = (0, 0, 1.3)
    chest_b.tail = (0, 0, 1.5)
    chest_b.parent = spine_b

    # Neck
    neck_b = edit_bones.new("Neck")
    neck_b.head = (0, 0, 1.5)
    neck_b.tail = (0, 0, 1.6)
    neck_b.parent = chest_b

    # Head
    head_b = edit_bones.new("Head")
    head_b.head = (0, 0, 1.6)
    head_b.tail = (0, 0, 1.82)
    head_b.parent = neck_b

    for side, sign in [("L", 1), ("R", -1)]:
        # Upper Arm
        u_arm = edit_bones.new(f"UpperArm.{side}")
        u_arm.head = (sign * 0.22, 0, 1.42)
        u_arm.tail = (sign * 0.42, 0, 1.25)
        u_arm.parent = chest_b

        # Lower Arm
        l_arm = edit_bones.new(f"LowerArm.{side}")
        l_arm.head = (sign * 0.42, 0, 1.25)
        l_arm.tail = (sign * 0.62, 0, 1.05)
        l_arm.parent = u_arm

        # Hand
        hand_b = edit_bones.new(f"Hand.{side}")
        hand_b.head = (sign * 0.62, 0, 1.05)
        hand_b.tail = (sign * 0.72, 0, 0.95)
        hand_b.parent = l_arm

        # Upper Leg
        u_leg = edit_bones.new(f"UpperLeg.{side}")
        u_leg.head = (sign * 0.14, 0, 0.9)
        u_leg.tail = (sign * 0.14, 0, 0.54)
        u_leg.parent = hips_b

        # Lower Leg
        l_leg = edit_bones.new(f"LowerLeg.{side}")
        l_leg.head = (sign * 0.14, 0, 0.54)
        l_leg.tail = (sign * 0.14, 0, 0.16)
        l_leg.parent = u_leg

        # Foot
        foot_b = edit_bones.new(f"Foot.{side}")
        foot_b.head = (sign * 0.14, 0, 0.16)
        foot_b.tail = (sign * 0.14, -0.15, 0.0)
        foot_b.parent = l_leg

    bpy.ops.object.mode_set(mode='OBJECT')

    # Parent mesh to armature with automatic weights
    bpy.ops.object.select_all(action='DESELECT')
    char_mesh.select_set(True)
    armature_obj.select_set(True)
    bpy.context.view_layer.objects.active = armature_obj
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')

    # Save .blend file
    blend_path = r"d:\Unity\Hostal_Survive\Assets\Models\Characters\HostelSurvivor.blend"
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"Saved .blend to: {blend_path}")

    # Export .fbx for Unity
    fbx_path = r"d:\Unity\Hostal_Survive\Assets\Models\Characters\HostelSurvivor.fbx"
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=False,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=True,
        object_types={'ARMATURE', 'MESH'},
        mesh_smooth_type='FACE',
        add_leaf_bones=False,
        primary_bone_axis='Y',
        secondary_bone_axis='X'
    )
    print(f"Exported .fbx to: {fbx_path}")

build_character()
