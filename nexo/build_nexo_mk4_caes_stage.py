import bpy, math, os, json, random
from mathutils import Vector

# ------------------------------------------------------------
# NEXO MK4 + CAES MASTER STAGE
# Blender 5.2+ / generated for a 55 s technical motion video.
# ------------------------------------------------------------
OUT = os.path.join(os.getcwd(), 'output_mk4_caes')
os.makedirs(OUT, exist_ok=True)
random.seed(42)

# Reset
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for c in list(bpy.data.collections):
    if c.name != 'Collection':
        bpy.data.collections.remove(c)
root_col = bpy.context.scene.collection.children.get('Collection')
if root_col:
    root_col.name = 'MASTER'

# Collections
def col(name):
    c=bpy.data.collections.get(name) or bpy.data.collections.new(name)
    if c.name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(c)
    return c
COL_NEXO=col('NEXO_MK4')
COL_RENEW=col('01_RENEWABLES')
COL_COMP=col('02_COMPRESSOR')
COL_HEAT=col('03_THERMAL')
COL_CAVERN=col('04_CAVERN')
COL_TURB=col('05_TURBINE_GENERATOR')
COL_GRID=col('06_GRID_CITY')
COL_STAGE=col('STAGE_TERRAIN')
COL_CAM=col('CAMERAS')
COL_CTRL=col('CONTROLS_BLOCKING')

# Helpers
def move_to_collection(o, collection):
    for cc in list(o.users_collection): cc.objects.unlink(o)
    collection.objects.link(o)
    return o

def mat(name, rgb, metallic=0.0, rough=0.45, emission=None, estr=0.0):
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color=(*rgb,1)
    m.use_nodes=True
    bsdf=m.node_tree.nodes.get('Principled BSDF')
    if bsdf:
        if 'Base Color' in bsdf.inputs: bsdf.inputs['Base Color'].default_value=(*rgb,1)
        if 'Metallic' in bsdf.inputs: bsdf.inputs['Metallic'].default_value=metallic
        if 'Roughness' in bsdf.inputs: bsdf.inputs['Roughness'].default_value=rough
        if emission:
            if 'Emission Color' in bsdf.inputs:
                bsdf.inputs['Emission Color'].default_value=(*emission,1)
                bsdf.inputs['Emission Strength'].default_value=estr
            elif 'Emission' in bsdf.inputs:
                bsdf.inputs['Emission'].default_value=(*emission,1)
    return m

M_WHITE=mat('NEXO pearl shell',(0.78,0.84,0.88),0.22,0.23)
M_SILVER=mat('NEXO secondary alloy',(0.34,0.40,0.45),0.72,0.22)
M_DARK=mat('Graphite mechanisms',(0.025,0.035,0.045),0.78,0.20)
M_RUBBER=mat('Joint rubber',(0.015,0.018,0.023),0.0,0.78)
M_CYAN=mat('NEXO cyan',(0.0,0.34,0.50),0.18,0.18,(0.0,0.8,1.0),5.5)
M_EYE=mat('Eye white',(0.88,0.94,0.96),0.05,0.20)
M_BLACK=mat('Pupil',(0.002,0.003,0.004),0.05,0.12)
M_BLUE=mat('Energy blue',(0.02,0.22,0.55),0.35,0.25,(0.02,0.34,1.0),3.5)
M_AMBER=mat('Hot air amber',(0.55,0.16,0.02),0.25,0.30,(1.0,0.25,0.03),2.4)
M_GREEN=mat('Renewable green',(0.06,0.30,0.16),0.15,0.40)
M_SOLAR=mat('Solar cells',(0.015,0.08,0.22),0.40,0.18)
M_STEEL=mat('Industrial steel',(0.24,0.28,0.31),0.82,0.23)
M_STEEL2=mat('Industrial dark steel',(0.08,0.10,0.12),0.75,0.28)
M_ROCK=mat('Rock',(0.14,0.10,0.075),0.05,0.90)
M_SOIL=mat('Soil',(0.20,0.10,0.05),0.0,0.95)
M_EARTH=mat('Deep earth',(0.10,0.055,0.035),0.0,1.0)
M_GRASS=mat('Surface',(0.09,0.22,0.10),0.0,0.85)
M_CITY=mat('City',(0.20,0.23,0.26),0.35,0.40)
M_GLASS=mat('City windows',(0.02,0.10,0.18),0.25,0.20,(0.05,0.25,0.8),0.8)


def bevel(o,w=0.03,seg=3):
    if o.type!='MESH': return o
    md=o.modifiers.new('Soft bevel','BEVEL'); md.width=w; md.segments=seg; md.limit_method='ANGLE'
    bpy.context.view_layer.objects.active=o
    try: bpy.ops.object.modifier_apply(modifier=md.name)
    except: pass
    return o

def smooth(o):
    if o.type=='MESH':
        for p in o.data.polygons: p.use_smooth=True
    return o

def cube(name,loc,size,ma,collection,rounding=0.03,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(location=loc,rotation=rot)
    o=bpy.context.object; o.name=name; o.scale=(size[0]/2,size[1]/2,size[2]/2)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    bevel(o,min(rounding,min(size)*0.20),4); smooth(o); o.data.materials.append(ma); move_to_collection(o,collection); return o

def sphere(name,loc,scale,ma,collection,segments=32):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=max(12,segments//2), location=loc)
    o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); smooth(o); o.data.materials.append(ma); move_to_collection(o,collection); return o

def cyl(name,loc,r,depth,ma,collection,rot=(0,0,0),vertices=40):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=loc,rotation=rot)
    o=bpy.context.object; o.name=name; bevel(o,min(r*0.12,0.035),3); smooth(o); o.data.materials.append(ma); move_to_collection(o,collection); return o

def torus(name,loc,major,minor,ma,collection,rot=(0,0,0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=48,minor_segments=12,location=loc,rotation=rot)
    o=bpy.context.object; o.name=name; smooth(o); o.data.materials.append(ma); move_to_collection(o,collection); return o

def segment(name,a,b,r,ma,collection):
    a,b=Vector(a),Vector(b); mid=(a+b)/2; d=b-a
    o=cyl(name,mid,r,d.length,ma,collection)
    o.rotation_euler=d.to_track_quat('Z','Y').to_euler(); return o

def curve(name,pts,ma,collection,bevel_depth=0.04,cyclic=False):
    cu=bpy.data.curves.new(name,'CURVE'); cu.dimensions='3D'; cu.bevel_depth=bevel_depth; cu.bevel_resolution=4; cu.resolution_u=10
    sp=cu.splines.new('BEZIER'); sp.bezier_points.add(len(pts)-1)
    for bp,p in zip(sp.bezier_points,pts):
        bp.co=p; bp.handle_left_type='AUTO'; bp.handle_right_type='AUTO'
    sp.use_cyclic_u=cyclic
    o=bpy.data.objects.new(name,cu); collection.objects.link(o); cu.materials.append(ma); return o

def text_obj(name,text,loc,size,collection,extrude=0.01,ma=M_WHITE,rot=(math.pi/2,0,0)):
    bpy.ops.object.text_add(location=loc,rotation=rot)
    o=bpy.context.object; o.name=name; o.data.body=text; o.data.align_x='CENTER'; o.data.align_y='CENTER'; o.data.size=size; o.data.extrude=extrude; o.data.bevel_depth=0.003; o.data.materials.append(ma); move_to_collection(o,collection); return o

def empty(name,loc,collection,display='PLAIN_AXES',size=0.4):
    o=bpy.data.objects.new(name,None); o.location=loc; o.empty_display_type=display; o.empty_display_size=size; collection.objects.link(o); return o

def look_at(o,pt): o.rotation_euler=(Vector(pt)-o.location).to_track_quat('-Z','Y').to_euler()

def camera(name,loc,target,lens=48):
    d=bpy.data.cameras.new(name); d.lens=lens; d.sensor_width=36
    o=bpy.data.objects.new(name,d); COL_CAM.objects.link(o); o.location=loc; look_at(o,target); return o

# ------------------------------------------------------------
# NEXO MK4 - corrected shoulders / arms / hands
# ------------------------------------------------------------
nexo=[]
def nk(o): nexo.append(o); return o

# torso / pelvis
nk(cube('NEXO_pelvis',(0,0,0.91),(0.38,0.26,0.20),M_SILVER,COL_NEXO,0.07))
nk(cube('NEXO_waist',(0,0,1.08),(0.25,0.20,0.18),M_DARK,COL_NEXO,0.06))
for z in (1.02,1.10,1.18): nk(torus(f'NEXO_waist_ring_{z}',(0,0,z),0.13,0.012,M_CYAN,COL_NEXO))
nk(cube('NEXO_torso',(0,0,1.36),(0.54,0.30,0.46),M_WHITE,COL_NEXO,0.10))
nk(cube('NEXO_chest_panel',(0,-0.165,1.38),(0.26,0.035,0.25),M_DARK,COL_NEXO,0.05))
nk(torus('NEXO_chest_ring',(0,-0.19,1.40),0.074,0.012,M_CYAN,COL_NEXO,rot=(math.pi/2,0,0)))
nk(sphere('NEXO_chest_core',(0,-0.205,1.40),(0.045,0.018,0.045),M_CYAN,COL_NEXO,24))
# clavicle housings integrate shoulder into torso
for side,sgn in [('L',-1),('R',1)]:
    nk(cube(f'NEXO_clavicle_shell_{side}',(0.19*sgn,0,1.50),(0.26,0.22,0.12),M_WHITE,COL_NEXO,0.055,rot=(0,0,sgn*0.10)))

# neck / head
nk(cyl('NEXO_neck',(0,0,1.65),0.070,0.15,M_DARK,COL_NEXO))
for z in (1.60,1.65,1.70): nk(torus(f'NEXO_neck_ring_{z}',(0,0,z),0.078,0.009,M_CYAN,COL_NEXO))
nk(cube('NEXO_head',(0,0,1.91),(0.66,0.38,0.44),M_WHITE,COL_NEXO,0.11))
nk(cube('NEXO_face_panel',(0,-0.205,1.91),(0.54,0.035,0.33),M_DARK,COL_NEXO,0.070))
for side,x in [('L',-0.145),('R',0.145)]:
    nk(sphere(f'NEXO_eye_{side}',(x,-0.229,1.96),(0.105,0.025,0.105),M_EYE,COL_NEXO,40))
    nk(sphere(f'NEXO_iris_{side}',(x,-0.248,1.96),(0.052,0.014,0.052),M_CYAN,COL_NEXO,32))
    nk(sphere(f'NEXO_pupil_{side}',(x,-0.260,1.96),(0.027,0.010,0.027),M_BLACK,COL_NEXO,24))
    nk(sphere(f'NEXO_eye_glint_{side}',(x-0.018,-0.269,1.989),(0.011,0.005,0.011),M_EYE,COL_NEXO,16))
    nk(cube(f'NEXO_brow_{side}',(x,-0.252,2.065),(0.15,0.018,0.022),M_SILVER,COL_NEXO,0.008,rot=(0,0,sgn*0.06 if (sgn:=-1 if side=='L' else 1) else 0)))
nk(cube('NEXO_mouth_screen',(0,-0.232,1.80),(0.18,0.018,0.055),M_BLACK,COL_NEXO,0.025))
nk(curve('NEXO_mouth_smile',[(-0.058,-0.247,1.805),(0,-0.252,1.79),(0.058,-0.247,1.805)],M_CYAN,COL_NEXO,0.006))
# antennae
for side,sgn,h in [('L',-1,2.14),('R',1,2.10)]:
    nk(curve(f'NEXO_antenna_{side}',[(0.34*sgn,0,1.98),(0.43*sgn,-0.01,2.05),(0.50*sgn,-0.01,h)],M_DARK,COL_NEXO,0.014))
    nk(sphere(f'NEXO_antenna_tip_{side}',(0.515*sgn,-0.01,h+0.015),(0.042,0.042,0.042),M_CYAN,COL_NEXO,24))

# corrected arms: shoulder socket closer to torso, longer upper arm, natural elbow/wrist
ARM_POINTS={}
for side,sgn in [('L',-1),('R',1)]:
    shoulder=(0.305*sgn,0,1.48); upper=(0.39*sgn,0,1.44); elbow=(0.58*sgn,0,1.31); wrist=(0.76*sgn,0,1.20); hand=(0.83*sgn,-0.01,1.13)
    ARM_POINTS[side]=(shoulder,upper,elbow,wrist,hand)
    nk(sphere(f'NEXO_shoulder_socket_{side}',shoulder,(0.085,0.085,0.085),M_DARK,COL_NEXO,32))
    nk(sphere(f'NEXO_shoulder_cap_{side}',upper,(0.105,0.095,0.095),M_WHITE,COL_NEXO,32))
    nk(segment(f'NEXO_upperarm_{side}',upper,elbow,0.072,M_SILVER,COL_NEXO))
    nk(torus(f'NEXO_elbow_ring_{side}',elbow,0.080,0.010,M_CYAN,COL_NEXO))
    nk(sphere(f'NEXO_elbow_joint_{side}',elbow,(0.075,0.075,0.075),M_DARK,COL_NEXO,28))
    # tapered visual: two forearm shells around a thinner core
    nk(segment(f'NEXO_forearm_core_{side}',elbow,wrist,0.060,M_DARK,COL_NEXO))
    mid=(Vector(elbow)*0.45+Vector(wrist)*0.55)
    nk(sphere(f'NEXO_forearm_shell_{side}',mid,(0.085,0.070,0.115),M_WHITE,COL_NEXO,28))
    nk(sphere(f'NEXO_wrist_joint_{side}',wrist,(0.055,0.055,0.055),M_DARK,COL_NEXO,24))
    nk(cube(f'NEXO_palm_{side}',hand,(0.115,0.080,0.145),M_SILVER,COL_NEXO,0.030,rot=(0,sgn*0.08,sgn*0.05)))
    # fingers: compact, aligned, larger than MK3
    for fi in range(4):
        base=(hand[0]+sgn*(0.040+fi*0.018),-0.012,hand[2]-0.070+fi*0.006)
        tip=(base[0]+sgn*0.012,-0.013,base[2]-0.082)
        nk(segment(f'NEXO_finger_{side}_{fi}',base,tip,0.012,M_WHITE,COL_NEXO))
    tb=(hand[0]+sgn*0.052,-0.020,hand[2]+0.008); tt=(tb[0]+sgn*0.070,-0.025,tb[2]-0.045)
    nk(segment(f'NEXO_thumb_{side}',tb,tt,0.014,M_WHITE,COL_NEXO))

# legs
for side,sgn in [('L',-1),('R',1)]:
    hip=(0.14*sgn,0,0.89); knee=(0.14*sgn,0,0.52); ankle=(0.14*sgn,0,0.18)
    nk(sphere(f'NEXO_hip_{side}',hip,(0.090,0.090,0.090),M_DARK,COL_NEXO,30))
    nk(segment(f'NEXO_thigh_{side}',(0.14*sgn,0,0.84),knee,0.090,M_WHITE,COL_NEXO))
    nk(torus(f'NEXO_knee_ring_{side}',knee,0.090,0.010,M_CYAN,COL_NEXO))
    nk(sphere(f'NEXO_knee_{side}',knee,(0.083,0.083,0.083),M_DARK,COL_NEXO,28))
    nk(segment(f'NEXO_shin_{side}',knee,ankle,0.078,M_SILVER,COL_NEXO))
    nk(sphere(f'NEXO_ankle_{side}',ankle,(0.050,0.050,0.050),M_DARK,COL_NEXO,20))
    nk(cube(f'NEXO_foot_{side}',(0.14*sgn,-0.060,0.085),(0.18,0.30,0.12),M_WHITE,COL_NEXO,0.045))
    nk(cube(f'NEXO_sole_{side}',(0.14*sgn,-0.062,0.020),(0.19,0.31,0.035),M_RUBBER,COL_NEXO,0.012))

# Armature with clavicles and correctly placed arm chains
bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0))
arm=bpy.context.object; arm.name='NEXO_MK4_RIG'; arm.data.name='NEXO_MK4_Armature'; arm.show_in_front=True; move_to_collection(arm,COL_NEXO)
for b in list(arm.data.edit_bones): arm.data.edit_bones.remove(b)
def eb(name,h,t,parent=None):
    b=arm.data.edit_bones.new(name); b.head=h; b.tail=t
    if parent: b.parent=arm.data.edit_bones.get(parent)
    return b
eb('root',(0,0,0.02),(0,0,0.40)); eb('pelvis',(0,0,0.82),(0,0,1.02),'root'); eb('spine',(0,0,1.02),(0,0,1.39),'pelvis'); eb('chest',(0,0,1.30),(0,0,1.57),'spine'); eb('neck',(0,0,1.57),(0,0,1.72),'chest'); eb('head',(0,0,1.70),(0,0,2.10),'neck')
for side,sgn in [('L',-1),('R',1)]:
    shoulder,upper,elbow,wrist,hand=ARM_POINTS[side]
    eb(f'clavicle.{side}',(0.08*sgn,0,1.51),shoulder,'chest')
    eb(f'upper_arm.{side}',shoulder,elbow,f'clavicle.{side}')
    eb(f'forearm.{side}',elbow,wrist,f'upper_arm.{side}')
    eb(f'hand.{side}',wrist,(0.86*sgn,-0.01,1.10),f'forearm.{side}')
    eb(f'thigh.{side}',(0.14*sgn,0,0.89),(0.14*sgn,0,0.52),'pelvis')
    eb(f'shin.{side}',(0.14*sgn,0,0.52),(0.14*sgn,0,0.18),f'thigh.{side}')
    eb(f'foot.{side}',(0.14*sgn,0,0.18),(0.14*sgn,-0.19,0.08),f'shin.{side}')
    eb(f'eye.{side}',(0.145*sgn,-0.10,1.96),(0.145*sgn,-0.28,1.96),'head')
bpy.ops.object.mode_set(mode='OBJECT'); arm.data.display_type='BBONE'

# Rigid parent groups to bones
def bone_parent(o,b):
    wm=o.matrix_world.copy(); o.parent=arm; o.parent_type='BONE'; o.parent_bone=b; o.matrix_world=wm
for o in nexo:
    n=o.name; b='chest'
    if any(k in n for k in ['head','face','eye_','iris','pupil','brow','mouth','antenna']): b='head'
    elif 'neck' in n: b='neck'
    elif any(k in n for k in ['pelvis','waist']): b='pelvis'
    elif any(k in n for k in ['clavicle_shell','shoulder_socket']): b='clavicle.L' if n.endswith('_L') else 'clavicle.R'
    elif any(k in n for k in ['shoulder_cap','upperarm']): b='upper_arm.L' if n.endswith('_L') else 'upper_arm.R'
    elif any(k in n for k in ['elbow','forearm']): b='forearm.L' if n.endswith('_L') else 'forearm.R'
    elif any(k in n for k in ['wrist','palm','finger','thumb']): b='hand.L' if '_L' in n else 'hand.R'
    elif any(k in n for k in ['hip','thigh']): b='thigh.L' if n.endswith('_L') else 'thigh.R'
    elif any(k in n for k in ['knee','shin']): b='shin.L' if n.endswith('_L') else 'shin.R'
    elif any(k in n for k in ['ankle','foot','sole']): b='foot.L' if n.endswith('_L') else 'foot.R'
    bone_parent(o,b)
# eyes to eye bones
for side in ('L','R'):
    for prefix in ('NEXO_eye_','NEXO_iris_','NEXO_pupil_','NEXO_eye_glint_'):
        o=bpy.data.objects.get(prefix+side)
        if o: bone_parent(o,f'eye.{side}')

# ------------------------------------------------------------
# STAGE / CAES SYSTEM
# ------------------------------------------------------------
# terrain cutaway layers
cube('Surface_layer',(0,2.3,-0.10),(22,7.8,0.20),M_GRASS,COL_STAGE,0.02)
cube('Soil_layer',(0,2.3,-0.65),(22,7.8,0.90),M_SOIL,COL_STAGE,0.02)
cube('Deep_earth',(0,2.3,-2.15),(22,7.8,2.10),M_EARTH,COL_STAGE,0.02)
# front cutaway lip / presentation platform
cube('Presentation_floor',(0,-2.0,0.00),(22,1.8,0.08),M_STEEL2,COL_STAGE,0.02)

# Renewables zone x=-8
for i,x in enumerate((-9.0,-7.8,-6.6)):
    cyl(f'Wind_tower_{i}',(x,2.2,1.15),0.07,2.3,M_WHITE,COL_RENEW)
    hub=sphere(f'Wind_hub_{i}',(x,2.2,2.32),(0.11,0.08,0.11),M_STEEL,COL_RENEW,24)
    rr=empty(f'Wind_rotor_{i}',(x,2.12,2.32),COL_RENEW,'PLAIN_AXES',0.2)
    for a in (0,2*math.pi/3,4*math.pi/3):
        bx=x+math.cos(a)*0.55; bz=2.32+math.sin(a)*0.55
        blade=segment(f'Wind_blade_{i}_{a:.2f}',(x,2.12,2.32),(bx,2.12,bz),0.025,M_WHITE,COL_RENEW); blade.parent=rr
    rr.rotation_mode='XYZ'; rr.rotation_euler.y=0; rr.keyframe_insert('rotation_euler',frame=1); rr.rotation_euler.y=math.radians(360*14); rr.keyframe_insert('rotation_euler',frame=1320)
# solar arrays
for row in range(2):
    for j in range(5):
        x=-9.1+j*0.65; y=0.8+row*0.65
        cube(f'Solar_{row}_{j}',(x,y,0.28),(0.52,0.34,0.035),M_SOLAR,COL_RENEW,0.015,rot=(math.radians(24),0,0))
        cube(f'Solar_frame_{row}_{j}',(x,y,0.23),(0.56,0.38,0.025),M_STEEL,COL_RENEW,0.01,rot=(math.radians(24),0,0))
text_obj('Label_renew','ENERGIA EXCEDENTE',(-7.8,-0.62,0.30),0.22,COL_RENEW,ma=M_CYAN,rot=(math.pi/2,0,0))

# Compressor zone x=-3.6
compressor_root=empty('Compressor_root',(-3.6,1.45,0.78),COL_COMP,'CUBE',0.3)
# main casing horizontal X
cyl('Compressor_casing',(-3.6,1.45,0.82),0.58,2.35,M_STEEL2,COL_COMP,rot=(0,math.pi/2,0))
cyl('Compressor_inner',(-3.6,1.45,0.82),0.46,2.15,M_STEEL,COL_COMP,rot=(0,math.pi/2,0))
cyl('Compressor_inlet',(-4.95,1.45,0.82),0.38,0.55,M_STEEL2,COL_COMP,rot=(0,math.pi/2,0))
cyl('Compressor_outlet',(-2.25,1.45,0.82),0.27,0.55,M_STEEL2,COL_COMP,rot=(0,math.pi/2,0))
shaft=cyl('Compressor_shaft',(-3.6,1.45,0.82),0.055,2.0,M_DARK,COL_COMP,rot=(0,math.pi/2,0))
rotor_roots=[]
for s in range(5):
    x=-4.35+s*0.37
    rr=empty(f'Comp_rotor_{s}',(x,1.45,0.82),COL_COMP,'PLAIN_AXES',0.18); rotor_roots.append(rr)
    torus(f'Comp_ring_{s}',(x,1.45,0.82),0.33,0.028,M_CYAN,COL_COMP,rot=(0,math.pi/2,0))
    for bi in range(8):
        a=2*math.pi*bi/8
        y=1.45+math.cos(a)*0.24; z=0.82+math.sin(a)*0.24
        bl=cube(f'Comp_blade_{s}_{bi}',(x,y,z),(0.045,0.09,0.28),M_SILVER,COL_COMP,0.012,rot=(a,0,0)); bl.parent=rr
    rr.rotation_mode='XYZ'; rr.rotation_euler.x=0; rr.keyframe_insert('rotation_euler',frame=320); rr.rotation_euler.x=math.radians(360*55); rr.keyframe_insert('rotation_euler',frame=650)
# platform/base
cube('Compressor_base',(-3.6,1.45,0.19),(3.0,1.65,0.25),M_STEEL2,COL_COMP,0.04)
for x in (-4.5,-3.6,-2.7): cube(f'Comp_support_{x}',(x,1.45,0.39),(0.18,0.85,0.42),M_STEEL,COL_COMP,0.03)
text_obj('Label_comp','COMPRESSOR MULTIESTAGIO',(-3.6,-0.63,0.30),0.20,COL_COMP,ma=M_CYAN,rot=(math.pi/2,0,0))

# Thermal storage / heat exchanger x=-0.5
cyl('Heat_tank',(-0.4,1.65,0.85),0.48,1.65,M_STEEL,COL_HEAT)
torus('Heat_tank_top',(-0.4,1.65,1.55),0.40,0.035,M_AMBER,COL_HEAT)
# helix coil
pts=[]
for i in range(80):
    a=i*0.26; z=0.30+i*(1.05/79); pts.append((-0.4+0.34*math.cos(a),1.65+0.34*math.sin(a),z))
curve('Heat_coil',pts,M_AMBER,COL_HEAT,0.025)
text_obj('Label_heat','CALOR RECUPERADO',(-0.4,-0.63,0.30),0.18,COL_HEAT,ma=M_AMBER,rot=(math.pi/2,0,0))

# Pipe network from compressor to heat and cavern
curve('Air_pipe_charge',[(-2.2,1.45,0.82),(-1.3,1.45,0.82),(-0.9,1.55,0.82),(-0.4,1.65,0.82),(0.4,1.75,0.75),(1.0,1.75,0.10),(1.0,1.75,-1.55)],M_BLUE,COL_STAGE,0.10)
# discharge pipe to turbine
curve('Air_pipe_discharge',[(1.0,1.75,-1.55),(2.2,1.75,-0.7),(3.0,1.65,0.35),(3.5,1.55,0.80)],M_AMBER,COL_STAGE,0.10)

# Cavern x=1, below ground: irregular rock mesh from ico sphere
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=4,radius=1.0,location=(1.0,1.8,-2.0))
cav=bpy.context.object; cav.name='CAES_Cavern_Rock'; cav.scale=(2.15,1.05,0.82); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
for v in cav.data.vertices:
    n=v.co.normalized(); f=1+0.09*math.sin(v.co.x*4.3)+0.06*math.cos(v.co.y*6.1)+0.05*math.sin(v.co.z*7.2)
    v.co*=f
smooth(cav); cav.data.materials.append(M_ROCK); move_to_collection(cav,COL_CAVERN)
# inner air volume
sphere('Cavern_air_volume',(1.0,1.8,-2.0),(1.78,0.80,0.58),M_BLUE,COL_CAVERN,32)
# particles
for i in range(70):
    a=random.random()*2*math.pi; rr=(random.random()**0.6); x=1.0+math.cos(a)*1.55*rr; y=1.8+(random.random()-0.5)*1.15*rr; z=-2.0+(random.random()-0.5)*0.90*rr
    sphere(f'Air_particle_{i:03d}',(x,y,z),(0.025,0.025,0.025),M_CYAN,COL_CAVERN,12)
# pressure rings / gauge style elements
for r in (0.45,0.65,0.85): torus(f'Cavern_pressure_ring_{r}',(1.0,0.74,-2.0),r,0.012,M_CYAN,COL_CAVERN,rot=(math.pi/2,0,0))
text_obj('Label_cavern','AR COMPRIMIDO - RESERVATORIO', (1.0,-0.62,0.30),0.17,COL_CAVERN,ma=M_CYAN,rot=(math.pi/2,0,0))

# Turbine + generator x=4.5..6.3
cyl('Turbine_casing',(4.35,1.55,0.82),0.55,1.85,M_STEEL2,COL_TURB,rot=(0,math.pi/2,0))
cyl('Turbine_shaft',(4.35,1.55,0.82),0.05,1.65,M_DARK,COL_TURB,rot=(0,math.pi/2,0))
for s in range(4):
    x=3.78+s*0.36; rr=empty(f'Turb_rotor_{s}',(x,1.55,0.82),COL_TURB,'PLAIN_AXES',0.16)
    torus(f'Turb_ring_{s}',(x,1.55,0.82),0.31,0.026,M_AMBER,COL_TURB,rot=(0,math.pi/2,0))
    for bi in range(8):
        a=2*math.pi*bi/8; y=1.55+math.cos(a)*0.23; z=0.82+math.sin(a)*0.23
        bl=cube(f'Turb_blade_{s}_{bi}',(x,y,z),(0.045,0.085,0.26),M_SILVER,COL_TURB,0.012,rot=(a,0,0)); bl.parent=rr
    rr.rotation_mode='XYZ'; rr.rotation_euler.x=0; rr.keyframe_insert('rotation_euler',frame=870); rr.rotation_euler.x=math.radians(360*70); rr.keyframe_insert('rotation_euler',frame=1160)
# generator
cyl('Generator_body',(6.15,1.55,0.82),0.63,1.45,M_STEEL,COL_TURB,rot=(0,math.pi/2,0))
for x in (5.65,6.15,6.65): torus(f'Generator_ring_{x}',(x,1.55,0.82),0.52,0.035,M_CYAN,COL_TURB,rot=(0,math.pi/2,0))
cyl('Generator_shaft',(5.25,1.55,0.82),0.07,1.25,M_DARK,COL_TURB,rot=(0,math.pi/2,0))
cube('Turbine_base',(5.0,1.55,0.19),(4.2,1.65,0.25),M_STEEL2,COL_TURB,0.04)
text_obj('Label_turb','TURBINA + GERADOR',(5.1,-0.63,0.30),0.20,COL_TURB,ma=M_AMBER,rot=(math.pi/2,0,0))

# Grid / city
# transformer
for x in (7.2,7.7): cyl(f'Transformer_{x}',(x,1.2,0.55),0.28,0.9,M_STEEL,COL_GRID)
curve('Grid_energy',[(6.8,1.55,0.82),(7.5,1.3,0.9),(8.5,1.1,1.2),(10.2,1.0,1.5)],M_CYAN,COL_GRID,0.045)
# pylons
for i,x in enumerate((8.3,9.6)):
    segment(f'Pylon_L_{i}',(x-0.30,1.0,0.0),(x,1.0,1.85),0.025,M_STEEL,COL_GRID); segment(f'Pylon_R_{i}',(x+0.30,1.0,0.0),(x,1.0,1.85),0.025,M_STEEL,COL_GRID)
    segment(f'Pylon_cross_{i}',(x-0.55,1.0,1.35),(x+0.55,1.0,1.35),0.025,M_STEEL,COL_GRID)
# city blocks
for i in range(10):
    x=8.0+(i%5)*0.65; y=2.2+(i//5)*0.8; h=0.55+0.22*(i%4)
    cube(f'Building_{i}',(x,y,h/2),(0.45,0.45,h),M_CITY,COL_GRID,0.025)
    for z in (0.20,0.40,0.60):
        if z<h: cube(f'Window_{i}_{z}',(x,y-0.235,z),(0.20,0.015,0.10),M_GLASS,COL_GRID,0.004)
text_obj('Label_grid','ELETRICIDADE PARA A REDE',(8.9,-0.62,0.30),0.18,COL_GRID,ma=M_CYAN,rot=(math.pi/2,0,0))

# motion / energy guide curves
curve('Flow_renew_to_comp',[(-7.6,0.15,0.35),(-5.8,0.1,0.45),(-4.2,0.1,0.45),(-3.6,0.75,0.65)],M_CYAN,COL_STAGE,0.030)
curve('Flow_generator_to_grid',[(6.6,1.55,0.82),(7.4,1.2,0.95),(8.6,1.05,1.20)],M_CYAN,COL_STAGE,0.030)

# ------------------------------------------------------------
# BLOCKING POSITIONS + CAMERAS + TIMELINE 55s @24fps
# ------------------------------------------------------------
scene=bpy.context.scene; scene.frame_start=1; scene.frame_end=1320; scene.render.fps=24
scene.render.resolution_x=960; scene.render.resolution_y=540; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'; scene.render.engine='BLENDER_WORKBENCH'
try:
    scene.display.shading.color_type='MATERIAL'; scene.display.shading.light='STUDIO'; scene.display.shading.show_shadows=True; scene.display.shading.show_cavity=True; scene.display.shading.cavity_type='WORLD'
except: pass
scene.world.color=(0.025,0.035,0.055)

shots=[
    {'name':'SHOT_01_INTRO','start':1,'end':144,'cam':camera('CAM_01_INTRO',(-1.5,-11.5,4.0),(0.0,1.0,0.65),54),'nexo':(-0.8,-1.20,0)},
    {'name':'SHOT_02_RENEWABLES','start':145,'end':336,'cam':camera('CAM_02_RENEW',(-8.0,-7.0,3.2),(-7.8,1.2,1.0),58),'nexo':(-6.0,-0.95,0)},
    {'name':'SHOT_03_COMPRESSOR','start':337,'end':600,'cam':camera('CAM_03_COMP',(-3.6,-5.2,2.25),(-3.6,1.35,0.85),62),'nexo':(-1.9,-0.95,0)},
    {'name':'SHOT_04_CAVERN','start':601,'end':864,'cam':camera('CAM_04_CAVERN',(1.0,-6.2,-0.45),(1.0,1.65,-1.45),52),'nexo':(2.5,-0.95,0)},
    {'name':'SHOT_05_TURBINE','start':865,'end':1128,'cam':camera('CAM_05_TURB',(5.0,-5.2,2.20),(5.0,1.45,0.85),60),'nexo':(3.1,-0.95,0)},
    {'name':'SHOT_06_SUMMARY','start':1129,'end':1320,'cam':camera('CAM_06_SUMMARY',(-0.5,-12.5,4.4),(0.5,1.2,0.55),52),'nexo':(0.0,-1.20,0)},
]
scene.camera=shots[0]['cam']
for s in shots:
    m=scene.timeline_markers.new(s['name'],frame=s['start']); m.camera=s['cam']
    p=empty('BLOCK_'+s['name'],s['nexo'],COL_CTRL,'CIRCLE',0.45)

# NEXO movement and simple presentational gestures
arm.rotation_mode='XYZ'
for s in shots:
    arm.location=s['nexo']; arm.keyframe_insert('location',frame=s['start'])
    arm.keyframe_insert('location',frame=min(s['end'],s['start']+18))
# basic gesture keys
pb=arm.pose.bones
for b in pb: b.rotation_mode='XYZ'
def pose(frame, head=(0,0,0), clavR=(0,0,0), armR=(0,0,0), foreR=(0,0,0), clavL=(0,0,0), armL=(0,0,0), foreL=(0,0,0)):
    vals={'head':head,'clavicle.R':clavR,'upper_arm.R':armR,'forearm.R':foreR,'clavicle.L':clavL,'upper_arm.L':armL,'forearm.L':foreL}
    for n,v in vals.items(): pb[n].rotation_euler=v; pb[n].keyframe_insert('rotation_euler',frame=frame)
pose(1); pose(70,head=(0.03,0,-0.08),clavR=(0,0,-0.18),armR=(0.05,-0.10,-0.55),foreR=(0.05,0,-0.50)); pose(140)
pose(145); pose(220,head=(0.02,0,0.18),armL=(0.0,0.12,0.65),foreL=(0,0,0.35)); pose(330)
pose(337); pose(430,head=(0.01,0,-0.15),armR=(0.0,-0.10,-0.75),foreR=(0,0,-0.35)); pose(560,armR=(0,0,-0.35),foreR=(0,0,-0.60)); pose(600)
pose(601); pose(700,head=(0.08,0,0.15),armL=(0,0.05,0.50),foreL=(0,0,0.30)); pose(820); pose(864)
pose(865); pose(960,head=(0.0,0,-0.15),armR=(0,-0.10,-0.65),foreR=(0,0,-0.45)); pose(1080); pose(1128)
pose(1129); pose(1200,armL=(0,0,0.38),foreL=(0,0,0.25),armR=(0,0,-0.38),foreR=(0,0,-0.25)); pose(1300); pose(1320)
# eyes/blink via scale
for side in ('L','R'):
    for pref in ('NEXO_eye_','NEXO_iris_','NEXO_pupil_','NEXO_eye_glint_'):
        o=bpy.data.objects.get(pref+side)
        if o:
            for f,scl in [(1,1.0),(280,1.0),(284,0.12),(288,1.0),(760,1.0),(764,0.12),(768,1.0),(1320,1.0)]: o.scale.z=scl; o.keyframe_insert('scale',frame=f)

# timeline script metadata
plan={
  'duration_seconds':55,'fps':24,'frames':1320,
  'shots':[{k:(v.name if k=='cam' else v) for k,v in s.items()} for s in shots],
  'narrative':['intro','renewable surplus','multi-stage compression + heat','underground storage','expansion/turbine/generator','summary'],
  'collections':[c.name for c in [COL_NEXO,COL_RENEW,COL_COMP,COL_HEAT,COL_CAVERN,COL_TURB,COL_GRID,COL_STAGE,COL_CAM,COL_CTRL]],
  'character_notes':'MK4 has clavicle bones, corrected shoulder integration, longer upper arms, cleaner elbow/wrist alignment and larger hands.'
}
with open(os.path.join(OUT,'CAES_MOTION_PLAN.json'),'w',encoding='utf-8') as f: json.dump(plan,f,indent=2,ensure_ascii=False)

# preview renders of key shots
for s in shots:
    scene.camera=s['cam']; scene.frame_set(s['start']); scene.render.filepath=os.path.join(OUT,s['name']+'.png'); bpy.ops.render.render(write_still=True)
# save master blend
scene.camera=shots[0]['cam']; scene.frame_set(1)
blend=os.path.join(OUT,'NEXO_MK4_CAES_MASTER.blend'); bpy.ops.wm.save_as_mainfile(filepath=blend)
# export NEXO GLB separately
try:
    bpy.ops.object.select_all(action='DESELECT')
    arm.select_set(True)
    for o in nexo: o.select_set(True)
    bpy.context.view_layer.objects.active=arm
    bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'NEXO_MK4_RIGGED.glb'),export_format='GLB',use_selection=True,export_animations=True)
except Exception as e:
    print('GLB export warning:',e)
# info file
with open(os.path.join(OUT,'README_GENERATED.txt'),'w',encoding='utf-8') as f:
    f.write('NEXO MK4 + CAES MASTER SCENE\n')
    f.write('55 s / 24 fps / 1320 frames\n')
    f.write('Includes NEXO MK4 corrected arms, full CAES stage, renewable sources, compressor, thermal recovery, underground cavern, turbine-generator, city/grid, six cameras, shot markers and animation blocking.\n')
    f.write(f'Objects: {len(bpy.data.objects)}\nMeshes: {len(bpy.data.meshes)}\nBones: {len(arm.data.bones)}\n')
print('DONE:',blend)
