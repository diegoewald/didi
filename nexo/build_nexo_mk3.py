import bpy, math, os
from mathutils import Vector

OUT = os.path.join(os.path.dirname(bpy.data.filepath) if bpy.data.filepath else os.getcwd(), 'output')
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def mat_principled(name, color, metallic=0.0, roughness=0.45, emission=None, emission_strength=0.0, transmission=0.0, alpha=1.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = roughness
    if 'Transmission Weight' in bsdf.inputs:
        bsdf.inputs['Transmission Weight'].default_value = transmission
    elif 'Transmission' in bsdf.inputs:
        bsdf.inputs['Transmission'].default_value = transmission
    if emission is not None:
        if 'Emission Color' in bsdf.inputs:
            bsdf.inputs['Emission Color'].default_value = (*emission, 1)
            bsdf.inputs['Emission Strength'].default_value = emission_strength
        elif 'Emission' in bsdf.inputs:
            bsdf.inputs['Emission'].default_value = (*emission, 1)
            if 'Emission Strength' in bsdf.inputs:
                bsdf.inputs['Emission Strength'].default_value = emission_strength
    if alpha < 1.0:
        bsdf.inputs['Alpha'].default_value = alpha
        m.surface_render_method = 'DITHERED'
    return m

WHITE = mat_principled('Shell_Pearl', (0.82,0.86,0.89), metallic=0.22, roughness=0.25)
WHITE2 = mat_principled('Shell_Secondary', (0.55,0.61,0.66), metallic=0.38, roughness=0.28)
DARK = mat_principled('Graphite', (0.025,0.035,0.045), metallic=0.72, roughness=0.20)
RUBBER = mat_principled('Rubber', (0.015,0.018,0.022), metallic=0.0, roughness=0.74)
GLASS = mat_principled('Face_Glass', (0.015,0.035,0.045), metallic=0.18, roughness=0.08, transmission=0.16)
CYAN = mat_principled('Cyan_Emission', (0.00,0.35,0.48), metallic=0.1, roughness=0.18, emission=(0.0,0.85,1.0), emission_strength=6.0)
BLUE = mat_principled('Blue_Accent', (0.03,0.14,0.30), metallic=0.55, roughness=0.22, emission=(0.05,0.28,0.8), emission_strength=1.3)
BLACK = mat_principled('Deep_Black', (0.002,0.003,0.004), metallic=0.08, roughness=0.12)
EYEWHITE = mat_principled('Eye_White', (0.86,0.92,0.95), metallic=0.05, roughness=0.18)
FLOOR = mat_principled('Studio_Floor', (0.018,0.023,0.03), metallic=0.15, roughness=0.42)

def smooth(obj):
    if hasattr(obj.data, 'polygons'):
        for p in obj.data.polygons: p.use_smooth = True

def add_bevel(obj, width=0.03, segments=4):
    mod = obj.modifiers.new('Bevel', 'BEVEL')
    mod.width = width
    mod.segments = segments
    mod.limit_method = 'ANGLE'
    bpy.context.view_layer.objects.active = obj
    try: bpy.ops.object.modifier_apply(modifier=mod.name)
    except: pass

def rounded_cube(name, loc, size, radius=0.05, mat=WHITE, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(location=loc, rotation=rot)
    o = bpy.context.object
    o.name = name
    o.scale = (size[0]/2, size[1]/2, size[2]/2)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    add_bevel(o, min(radius, min(size)*0.22), 5)
    smooth(o)
    o.data.materials.append(mat)
    return o

def sphere(name, loc, scale, mat=WHITE, segments=48, rings=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    o = bpy.context.object
    o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    smooth(o)
    o.data.materials.append(mat)
    return o

def cylinder(name, loc, radius, depth, mat=DARK, rot=(0,0,0), vertices=48, bevel=0.015):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    o = bpy.context.object
    o.name = name
    if bevel: add_bevel(o, bevel, 3)
    smooth(o)
    o.data.materials.append(mat)
    return o

def torus(name, loc, major, minor, mat=CYAN, rot=(0,0,0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=64, minor_segments=16, location=loc, rotation=rot)
    o=bpy.context.object; o.name=name; smooth(o); o.data.materials.append(mat); return o

def curve_tube(name, pts, bevel=0.012, mat=DARK):
    cu=bpy.data.curves.new(name,'CURVE'); cu.dimensions='3D'; cu.bevel_depth=bevel; cu.bevel_resolution=5; cu.resolution_u=16
    sp=cu.splines.new('BEZIER'); sp.bezier_points.add(len(pts)-1)
    for bp,co in zip(sp.bezier_points,pts):
        bp.co=co; bp.handle_left_type='AUTO'; bp.handle_right_type='AUTO'
    o=bpy.data.objects.new(name,cu); bpy.context.collection.objects.link(o); cu.materials.append(mat); return o

def add_rivet_ring(prefix, center, radii=(0.22,0.19), count=8, y=-0.198):
    for i in range(count):
        a=2*math.pi*i/count
        x=center[0]+radii[0]*math.cos(a); z=center[2]+radii[1]*math.sin(a)
        sphere(f'{prefix}_rivet_{i:02d}', (x,y,z), (0.012,0.008,0.012), DARK, 20, 10)

parts=[]
def keep(o): parts.append(o); return o

keep(rounded_cube('pelvis_shell',(0,0,0.93),(0.38,0.25,0.22),0.07,WHITE2))
keep(rounded_cube('abdomen_core',(0,0,1.10),(0.30,0.20,0.22),0.08,DARK))
for z in (1.02,1.09,1.16): keep(torus(f'waist_ring_{z:.2f}',(0,0,z),0.145,0.014,CYAN,rot=(math.pi/2,0,0)))
keep(rounded_cube('torso_main',(0,0,1.34),(0.56,0.30,0.48),0.10,WHITE))
keep(rounded_cube('sternum_panel',(0,-0.168,1.36),(0.25,0.035,0.27),0.055,GLASS))
keep(torus('chest_core_ring',(0,-0.19,1.39),0.080,0.012,CYAN,rot=(math.pi/2,0,0)))
keep(sphere('chest_core',(0,-0.196,1.39),(0.047,0.018,0.047),CYAN,32,16))
keep(curve_tube('chest_line_L',[(-0.03,-0.176,1.48),(-0.14,-0.183,1.50),(-0.24,-0.171,1.47)],0.008,CYAN))
keep(curve_tube('chest_line_R',[(0.03,-0.176,1.48),(0.14,-0.183,1.50),(0.24,-0.171,1.47)],0.008,CYAN))
keep(cylinder('neck_core',(0,0,1.64),0.075,0.14,DARK,bevel=0.012))
for z in (1.59,1.64,1.69): keep(torus(f'neck_ring_{z:.2f}',(0,0,z),0.084,0.009,CYAN))
keep(rounded_cube('head_shell',(0,0,1.90),(0.66,0.38,0.46),0.11,WHITE))
keep(rounded_cube('face_glass',(0,-0.205,1.90),(0.54,0.035,0.34),0.075,GLASS))
keep(rounded_cube('head_brow_armor',(0,-0.185,2.095),(0.48,0.055,0.045),0.018,WHITE2))
keep(rounded_cube('head_chin_armor',(0,-0.185,1.705),(0.40,0.055,0.045),0.018,WHITE2))
for side,x in [('L',-0.145),('R',0.145)]:
    keep(sphere(f'eye_white_{side}',(x,-0.227,1.955),(0.113,0.025,0.113),EYEWHITE,64,32))
    keep(sphere(f'iris_{side}',(x,-0.247,1.955),(0.060,0.014,0.060),CYAN,48,24))
    keep(sphere(f'pupil_{side}',(x,-0.259,1.955),(0.030,0.010,0.030),BLACK,40,20))
    keep(sphere(f'eye_highlight_{side}',(x-0.020,-0.270,1.988),(0.013,0.006,0.013),EYEWHITE,20,10))
    keep(rounded_cube(f'brow_{side}',(x,-0.252,2.070),(0.155,0.018,0.025),0.010,DARK,rot=(0,0,(-0.08 if side=='L' else 0.08))))
keep(rounded_cube('mouth_screen',(0,-0.232,1.790),(0.19,0.018,0.060),0.025,BLACK))
keep(curve_tube('mouth_light',[(-0.060,-0.246,1.790),(0,-0.252,1.778),(0.060,-0.246,1.790)],0.007,CYAN))
add_rivet_ring('face',(0,0,1.90),radii=(0.292,0.182),count=10,y=-0.226)
for side,x in [('L',-0.353),('R',0.353)]:
    keep(cylinder(f'ear_outer_{side}',(x,0,1.90),0.078,0.036,DARK,rot=(0,math.pi/2,0),bevel=0.009))
    keep(cylinder(f'ear_inner_{side}',(x + (-0.020 if side=='L' else 0.020),0,1.90),0.046,0.040,CYAN,rot=(0,math.pi/2,0),bevel=0.007))
keep(curve_tube('antenna_L',[(-0.36,0,1.96),(-0.45,-0.02,2.04),(-0.53,-0.02,2.12)],0.017,DARK))
keep(sphere('antenna_tip_L',(-0.545,-0.02,2.135),(0.050,0.050,0.050),CYAN,32,16))
keep(curve_tube('antenna_R',[(0.36,0,1.96),(0.44,-0.01,2.02),(0.49,-0.01,2.09)],0.014,DARK))
keep(sphere('antenna_tip_R',(0.505,-0.01,2.105),(0.038,0.038,0.038),CYAN,32,16))
for side,sgn in [('L',-1),('R',1)]:
    shoulder=(0.34*sgn,0,1.49); elbow=(0.59*sgn,0,1.30); wrist=(0.78*sgn,0,1.16)
    keep(sphere(f'shoulder_joint_{side}',shoulder,(0.100,0.100,0.100),DARK,40,20))
    keep(sphere(f'shoulder_cap_{side}',(shoulder[0],-0.012,shoulder[2]+0.018),(0.135,0.115,0.105),WHITE,48,24))
    mid=((shoulder[0]+elbow[0])/2,0,(shoulder[2]+elbow[2])/2)
    length=math.dist(shoulder,elbow); ang=math.atan2(abs(elbow[0]-shoulder[0]), shoulder[2]-elbow[2]); rot_y=sgn*ang
    keep(cylinder(f'upperarm_{side}',mid,0.080,length*0.88,WHITE2,rot=(0,rot_y,0),bevel=0.025))
    keep(torus(f'upperarm_ring_{side}',mid,0.083,0.009,CYAN,rot=(0,rot_y,0)))
    keep(sphere(f'elbow_joint_{side}',elbow,(0.087,0.087,0.087),DARK,40,20))
    keep(torus(f'elbow_ring_{side}',elbow,0.093,0.010,CYAN))
    mid2=((elbow[0]+wrist[0])/2,0,(elbow[2]+wrist[2])/2); length2=math.dist(elbow,wrist); ang2=math.atan2(abs(wrist[0]-elbow[0]), elbow[2]-wrist[2])
    keep(cylinder(f'forearm_{side}',mid2,0.090,length2*0.90,WHITE,rot=(0,sgn*ang2,0),bevel=0.025))
    keep(rounded_cube(f'forearm_panel_{side}',(mid2[0],-0.074,mid2[2]),(0.070,0.024,0.125),0.018,GLASS,rot=(0,sgn*ang2,0)))
    keep(sphere(f'wrist_joint_{side}',wrist,(0.068,0.068,0.068),DARK,32,16))
    hand=(0.855*sgn,-0.005,1.09)
    keep(rounded_cube(f'palm_{side}',hand,(0.13,0.085,0.16),0.035,WHITE2,rot=(0,sgn*0.12,sgn*0.08)))
    for fi in range(4):
        fx=hand[0]+sgn*(0.042+fi*0.022); fy=-0.012; fz=hand[2]-0.080+fi*0.008
        keep(cylinder(f'finger_{side}_{fi}_A',(fx,fy,fz),0.013,0.065,WHITE,rot=(0,sgn*0.18,0),vertices=24,bevel=0.008))
        keep(cylinder(f'finger_{side}_{fi}_B',(fx+sgn*0.010,fy-0.002,fz-0.050),0.012,0.052,WHITE2,rot=(0,sgn*0.18,0),vertices=24,bevel=0.007))
    keep(cylinder(f'thumb_{side}_A',(hand[0]+sgn*0.060,-0.018,hand[2]+0.015),0.016,0.075,WHITE,rot=(0,sgn*0.85,sgn*0.45),vertices=24,bevel=0.008))
    keep(cylinder(f'thumb_{side}_B',(hand[0]+sgn*0.092,-0.018,hand[2]-0.010),0.015,0.060,WHITE2,rot=(0,sgn*0.75,sgn*0.45),vertices=24,bevel=0.008))
for side,sgn in [('L',-1),('R',1)]:
    hip=(0.15*sgn,0,0.91); knee=(0.16*sgn,0,0.53); ankle=(0.16*sgn,0,0.18)
    keep(sphere(f'hip_joint_{side}',hip,(0.095,0.095,0.095),DARK,40,20))
    keep(sphere(f'hip_cap_{side}',(hip[0],-0.005,hip[2]),(0.125,0.105,0.115),WHITE2,40,20))
    keep(cylinder(f'thigh_{side}',((hip[0]+knee[0])/2,0,(hip[2]+knee[2])/2),0.105,0.30,WHITE,bevel=0.030))
    keep(rounded_cube(f'thigh_panel_{side}',(0.15*sgn,-0.091,0.72),(0.09,0.025,0.16),0.022,GLASS))
    keep(sphere(f'knee_joint_{side}',knee,(0.100,0.100,0.100),DARK,40,20))
    keep(rounded_cube(f'kneecap_{side}',(knee[0],-0.095,knee[2]),(0.13,0.045,0.12),0.034,WHITE2))
    keep(torus(f'knee_ring_{side}',knee,0.105,0.010,CYAN))
    keep(cylinder(f'calf_{side}',((knee[0]+ankle[0])/2,0,(knee[2]+ankle[2])/2),0.092,0.27,WHITE2,bevel=0.027))
    keep(rounded_cube(f'calf_guard_{side}',(0.16*sgn,-0.075,0.345),(0.13,0.045,0.19),0.030,WHITE))
    keep(sphere(f'ankle_joint_{side}',ankle,(0.065,0.065,0.065),DARK,32,16))
    keep(rounded_cube(f'foot_{side}',(0.16*sgn,-0.055,0.095),(0.20,0.34,0.13),0.050,WHITE))
    keep(rounded_cube(f'sole_{side}',(0.16*sgn,-0.065,0.025),(0.21,0.35,0.045),0.018,RUBBER))
    keep(curve_tube(f'foot_light_{side}',[(0.10*sgn,-0.225,0.105),(0.16*sgn,-0.235,0.11),(0.22*sgn,-0.225,0.105)],0.006,CYAN))
for z in (1.20,1.31,1.42): keep(rounded_cube(f'back_vent_{z:.2f}',(0,0.17,z),(0.22,0.025,0.050),0.012,DARK))

bpy.ops.object.armature_add(enter_editmode=True, location=(0,0,0))
arm=bpy.context.object; arm.name='NEXO_MK3_RIG'; arm.show_in_front=True; arm.data.name='NEXO_MK3_Armature'
for b in arm.data.edit_bones: arm.data.edit_bones.remove(b)
def ebone(name, head, tail, parent=None):
    b=arm.data.edit_bones.new(name); b.head=head; b.tail=tail
    if parent: b.parent=arm.data.edit_bones.get(parent)
    return b
ebone('root',(0,0,0.02),(0,0,0.40)); ebone('pelvis',(0,0,0.82),(0,0,1.02),'root'); ebone('spine',(0,0,1.02),(0,0,1.42),'pelvis'); ebone('chest',(0,0,1.30),(0,0,1.60),'spine'); ebone('neck',(0,0,1.58),(0,0,1.72),'chest'); ebone('head',(0,0,1.70),(0,0,2.10),'neck')
for side,sgn in [('L',-1),('R',1)]:
    ebone(f'upper_arm.{side}',(0.31*sgn,0,1.50),(0.59*sgn,0,1.30),'chest'); ebone(f'forearm.{side}',(0.59*sgn,0,1.30),(0.78*sgn,0,1.16),f'upper_arm.{side}'); ebone(f'hand.{side}',(0.78*sgn,0,1.16),(0.91*sgn,0,1.06),f'forearm.{side}'); ebone(f'thigh.{side}',(0.15*sgn,0,0.92),(0.16*sgn,0,0.53),'pelvis'); ebone(f'shin.{side}',(0.16*sgn,0,0.53),(0.16*sgn,0,0.18),f'thigh.{side}'); ebone(f'foot.{side}',(0.16*sgn,0,0.18),(0.16*sgn,-0.20,0.08),f'shin.{side}'); ebone(f'eye.{side}',(0.145*sgn,-0.10,1.955),(0.145*sgn,-0.28,1.955),'head')
ebone('mouth',(0,-0.08,1.79),(0,-0.25,1.79),'head')
bpy.ops.object.mode_set(mode='OBJECT'); arm.data.display_type='BBONE'
def bone_parent(obj,bone):
    wm=obj.matrix_world.copy(); obj.parent=arm; obj.parent_type='BONE'; obj.parent_bone=bone; obj.matrix_world=wm
for o in parts:
    n=o.name; bone='chest'
    if n.startswith(('head','face_','eye_','iris_','pupil_','brow_','mouth_','ear_','antenna_','neck_')): bone='head' if not n.startswith('neck_') else 'neck'
    elif n.startswith(('pelvis_','abdomen_','waist_')): bone='pelvis'
    elif n.startswith(('upperarm_','shoulder_')): bone='upper_arm.L' if n.endswith('_L') or '_L_' in n else 'upper_arm.R'
    elif n.startswith(('forearm_','elbow_')): bone='forearm.L' if n.endswith('_L') or '_L_' in n else 'forearm.R'
    elif n.startswith(('palm_','finger_','thumb_','wrist_')): bone='hand.L' if '_L' in n else 'hand.R'
    elif n.startswith(('thigh_','hip_')): bone='thigh.L' if '_L' in n else 'thigh.R'
    elif n.startswith(('calf_','shin_','knee_','kneecap_')): bone='shin.L' if '_L' in n else 'shin.R'
    elif n.startswith(('foot_','sole_','ankle_')): bone='foot.L' if '_L' in n else 'foot.R'
    elif n.startswith(('torso_','sternum_','chest_','back_')): bone='chest'
    try: bone_parent(o,bone)
    except: pass
for side in ('L','R'):
    for prefix in ('eye_white_','iris_','pupil_','eye_highlight_'):
        o=bpy.data.objects.get(prefix+side)
        if o: bone_parent(o,f'eye.{side}')
for bn in ('head','neck'):
    pb=arm.pose.bones[bn]; c=pb.constraints.new('LIMIT_ROTATION'); c.use_limit_x=True; c.min_x=-0.45; c.max_x=0.45; c.use_limit_y=True; c.min_y=-0.55; c.max_y=0.55; c.use_limit_z=True; c.min_z=-0.55; c.max_z=0.55; c.owner_space='LOCAL'
scene=bpy.context.scene; scene.frame_start=1; scene.frame_end=120; scene.render.fps=24
pb=arm.pose.bones
for b in pb: b.rotation_mode='XYZ'
for f in (1,120):
    pb['head'].rotation_euler=(0,0,0); pb['head'].keyframe_insert('rotation_euler',frame=f); pb['upper_arm.R'].rotation_euler=(0,0,0); pb['upper_arm.R'].keyframe_insert('rotation_euler',frame=f); pb['forearm.R'].rotation_euler=(0,0,0); pb['forearm.R'].keyframe_insert('rotation_euler',frame=f)
pb['head'].rotation_euler=(0.05,0,-0.12); pb['head'].keyframe_insert('rotation_euler',frame=45); pb['head'].rotation_euler=(-0.03,0,0.10); pb['head'].keyframe_insert('rotation_euler',frame=80)
pb['upper_arm.R'].rotation_euler=(0.05,-0.25,-0.90); pb['upper_arm.R'].keyframe_insert('rotation_euler',frame=28); pb['forearm.R'].rotation_euler=(0.10,0.0,-0.75); pb['forearm.R'].keyframe_insert('rotation_euler',frame=28)
for f,ang in [(40,-0.45),(52,-1.05),(64,-0.45),(76,-1.05),(88,-0.55)]: pb['forearm.R'].rotation_euler=(0.10,0.0,ang); pb['forearm.R'].keyframe_insert('rotation_euler',frame=f)
pb['upper_arm.R'].rotation_euler=(0,0,0); pb['upper_arm.R'].keyframe_insert('rotation_euler',frame=105); pb['forearm.R'].rotation_euler=(0,0,0); pb['forearm.R'].keyframe_insert('rotation_euler',frame=105)
for side in ('L','R'):
    for prefix in ('eye_white_','iris_','pupil_','eye_highlight_'):
        o=bpy.data.objects.get(prefix+side)
        if not o: continue
        for f,s in [(1,1),(55,1),(58,0.10),(61,1),(120,1)]: o.scale.z=s; o.keyframe_insert('scale',frame=f)

bpy.ops.mesh.primitive_plane_add(size=20, location=(0,0,-0.005)); floor=bpy.context.object; floor.name='Studio_Floor'; floor.data.materials.append(FLOOR)
rounded_cube('Backdrop',(0,2.4,2.0),(7,0.18,4.8),0.12,FLOOR)
def area(name,loc,energy,color,size,rot):
    data=bpy.data.lights.new(name,'AREA'); data.energy=energy; data.color=color; data.shape='DISK'; data.size=size; o=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(o); o.location=loc; o.rotation_euler=rot; return o
area('Key',(-2.3,-3.5,3.4),1400,(0.82,0.92,1.0),3.0,(math.radians(55),0,math.radians(-28))); area('Fill',(2.5,-2.4,2.4),700,(0.42,0.62,1.0),2.5,(math.radians(65),0,math.radians(38))); area('Rim',(0,2.0,3.6),1800,(0.0,0.65,1.0),2.0,(math.radians(-55),0,math.pi))
ld=bpy.data.lights.new('EyeCatch','POINT'); ld.energy=120; ld.color=(0.8,0.95,1.0); ld.shadow_soft_size=1.2; lo=bpy.data.objects.new('EyeCatch',ld); bpy.context.collection.objects.link(lo); lo.location=(0,-2.0,2.0)
camd=bpy.data.cameras.new('Camera'); cam=bpy.data.objects.new('Camera',camd); bpy.context.collection.objects.link(cam); scene.camera=cam; camd.lens=62; camd.sensor_width=36
def look_at(obj,pt): obj.rotation_euler=(Vector(pt)-obj.location).to_track_quat('-Z','Y').to_euler()
scene.world.color=(0.006,0.009,0.014); world=scene.world; world.use_nodes=True; bg=world.node_tree.nodes.get('Background'); bg.inputs['Color'].default_value=(0.004,0.009,0.018,1); bg.inputs['Strength'].default_value=0.32
scene.render.engine='BLENDER_EEVEE_NEXT'; scene.render.resolution_x=1080; scene.render.resolution_y=1350; scene.render.resolution_percentage=100; scene.render.image_settings.file_format='PNG'; scene.render.film_transparent=False; scene.render.image_settings.color_mode='RGBA'; scene.view_settings.look='AgX - Medium High Contrast'
views=[('preview_3q',(3.15,-5.4,2.45),(0,0,1.20),62),('preview_front',(0,-5.8,1.85),(0,0,1.18),66),('preview_side',(5.6,-0.25,2.0),(0,0,1.16),66)]
scene.frame_set(1)
for fname,loc,target,lens in views:
    cam.location=loc; camd.lens=lens; look_at(cam,target); scene.render.filepath=os.path.join(OUT,f'{fname}.png'); bpy.ops.render.render(write_still=True)
blend_path=os.path.join(OUT,'NEXO_MK3_RIGGED.blend'); bpy.ops.wm.save_as_mainfile(filepath=blend_path)
scene.frame_set(1)
try: bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'NEXO_MK3_RIGGED.glb'), export_format='GLB', export_animations=True)
except Exception as e: print('GLB export failed:',e)
with open(os.path.join(OUT,'NEXO_MK3_INFO.txt'),'w',encoding='utf-8') as f:
    f.write('NEXO MK3 - Blender-native humanoid robot presenter\n'); f.write('Design: rounded rectangular head, large expressive circular eyes, asymmetrical antennas, humanoid proportions.\n'); f.write(f'Objects: {len(bpy.data.objects)}\n'); f.write(f'Meshes: {len(bpy.data.meshes)}\n'); f.write(f'Bones: {len(arm.data.bones)}\n'); f.write('Includes a short head/wave/blink animation on frames 1-120.\n')
print('DONE', blend_path)
