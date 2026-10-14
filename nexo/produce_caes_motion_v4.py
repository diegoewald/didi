import bpy, os, math, json, random
from pathlib import Path
from mathutils import Vector

# Execute V3 with Blender 5.x Action API guard.
src=Path('nexo/produce_caes_motion_v3.py').read_text(encoding='utf-8')
src=src.replace("    if action:\n        for fc in action.fcurves:","    if action and hasattr(action, 'fcurves'):\n        for fc in action.fcurves:")
os.environ['CAES_PREVIEW']='0'
os.environ.pop('START_FRAME',None); os.environ.pop('END_FRAME',None)
exec(compile(src,'nexo/produce_caes_motion_v3.py','exec'),globals(),globals())

scene=bpy.context.scene
scene.frame_start=1; scene.frame_end=END_FRAME; scene.render.fps=FPS
scene.render.resolution_x=720; scene.render.resolution_y=1280; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='JPEG'; scene.render.image_settings.quality=92
scene.render.engine='BLENDER_EEVEE'
try:
    scene.view_settings.look='AgX - Medium High Contrast'
except: pass

# -------------------------
# Cinematic Eevee lighting
# -------------------------
def collection(name):
    c=bpy.data.collections.get(name) or bpy.data.collections.new(name)
    if c.name not in scene.collection.children: scene.collection.children.link(c)
    return c
LIGHTS=collection('PRO_LIGHTING_V4')
for o in list(LIGHTS.objects): bpy.data.objects.remove(o,do_unlink=True)

def light(name,typ,loc,energy,color,size=5.0,target=(0,1,0.7)):
    d=bpy.data.lights.new(name,typ); d.energy=energy; d.color=color
    if typ=='AREA': d.shape='DISK'; d.size=size
    if typ=='POINT': d.shadow_soft_size=size
    o=bpy.data.objects.new(name,d); LIGHTS.objects.link(o); o.location=loc
    if typ in ('AREA','SPOT'): o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    return o
light('Key_Global','AREA',(0,-5.5,7.0),1600,(0.78,0.88,1.0),8.0,(0,1,0.4))
light('Rim_Left','AREA',(-8,3.5,5.0),1300,(0.10,0.55,1.0),6.0,(-4,1,0.7))
light('Rim_Right','AREA',(8,3.5,5.0),1200,(1.0,0.35,0.08),6.0,(4,1,0.7))
light('Front_Fill','AREA',(0,-6,2.0),900,(0.55,0.72,1.0),7.0,(0,0,1.0))
light('Compressor_Cyan','POINT',(-3.6,0.4,1.5),420,(0.0,0.55,1.0),1.2)
light('Heat_Amber','POINT',(-0.4,0.9,1.2),520,(1.0,0.22,0.04),1.0)
light('Cavern_Cyan','POINT',(1.0,0.8,-1.8),580,(0.0,0.48,1.0),1.3)
light('Turbine_Amber','POINT',(4.4,0.5,1.3),520,(1.0,0.22,0.03),1.1)
light('Grid_Cyan','POINT',(8.4,0.4,1.5),420,(0.0,0.55,1.0),1.4)
scene.world.use_nodes=True
bg=scene.world.node_tree.nodes.get('Background')
if bg: bg.inputs['Color'].default_value=(0.004,0.009,0.018,1); bg.inputs['Strength'].default_value=0.20

# -------------------------
# Hold the presenter in each stage zone instead of interpolating across the set.
# -------------------------
STOPS=[
 (0,(-0.8,-1.20,0)),
 (4300,(-6.0,-0.95,0)),
 (8900,(-2.0,-0.95,0)),
 (23900,(2.5,-0.95,0)),
 (37100,(3.0,-0.95,0)),
 (46000,(6.9,-0.95,0)),
 (52380,(0.0,-1.20,0)),
 (59080,(0.0,-1.20,0)),
]
for i,(ms,pos) in enumerate(STOPS):
    f=fr(ms); arm.location=pos; arm.keyframe_insert('location',frame=f)
    if i+1<len(STOPS):
        nf=fr(STOPS[i+1][0]); arm.location=pos; arm.keyframe_insert('location',frame=max(f,nf-2))

# Subtle body idle, independent from gestures.
for i,ms in enumerate(range(0,59000,1100)):
    b=arm.pose.bones.get('chest')
    if b:
        b.rotation_mode='XYZ'; b.rotation_euler=(0.006*math.sin(i*1.1),0.008*math.sin(i*0.73),0.006*math.cos(i*0.87)); b.keyframe_insert('rotation_euler',frame=fr(ms))

# -------------------------
# Better vertical shot design: 17 shots aligned to semantic phrases.
# -------------------------
for m in list(scene.timeline_markers): scene.timeline_markers.remove(m)
CAMCOL=collection('PRO_CAMERAS_V4')
for o in list(CAMCOL.objects): bpy.data.objects.remove(o,do_unlink=True)

def camshot(name,s,e,loc0,target0,lens0,loc1=None,target1=None,lens1=None):
    d=bpy.data.cameras.new(name+'_DATA'); d.lens=lens0; d.sensor_width=36; d.sensor_fit='VERTICAL'
    c=bpy.data.objects.new(name,d); CAMCOL.objects.link(c)
    t=bpy.data.objects.new(name+'_TARGET',None); CAMCOL.objects.link(t)
    con=c.constraints.new('TRACK_TO'); con.target=t; con.track_axis='TRACK_NEGATIVE_Z'; con.up_axis='UP_Y'
    f0=fr(s); f1=fr(e)
    c.location=loc0; c.keyframe_insert('location',frame=f0); t.location=target0; t.keyframe_insert('location',frame=f0); d.lens=lens0; d.keyframe_insert('lens',frame=f0)
    c.location=loc1 or loc0; c.keyframe_insert('location',frame=f1); t.location=target1 or target0; t.keyframe_insert('location',frame=f1); d.lens=lens1 or lens0; d.keyframe_insert('lens',frame=f1)
    mk=scene.timeline_markers.new(name,frame=f0); mk.camera=c
    return {'name':name,'s':s,'e':e,'cam':c}

S=[]
# Presenter-centric opening; camera stays in front of NEXO, not the thermal tank.
S.append(camshot('V4_01_NEXO_INTRO',0,2300,(-0.8,-7.0,2.05),(-0.8,-1.20,1.18),64,(-0.8,-6.1,1.85),(-0.8,-1.20,1.30),70))
S.append(camshot('V4_02_AIR_AROUND_NEXO',2300,4300,(-0.8,-5.6,1.75),(-0.8,-1.20,1.18),72,(-0.35,-5.3,1.65),(-0.65,-1.10,1.15),68))
# Show NEXO + solar array + turbine context.
S.append(camshot('V4_03_RENEWABLE_WIDE',4300,8900,(-6.6,-8.8,2.85),(-7.55,1.15,0.95),46,(-7.25,-7.3,2.55),(-7.65,1.30,1.10),52))
S.append(camshot('V4_04_ENERGY_TO_COMP',8900,12400,(-4.5,-7.0,1.85),(-4.3,1.05,0.65),54,(-3.75,-5.0,1.55),(-3.65,1.38,0.80),62))
# Real macro travelling through compressor stages.
S.append(camshot('V4_05_COMPRESSOR_INTAKE',12400,14600,(-5.15,-3.15,1.02),(-4.55,1.45,0.82),76,(-4.35,-2.75,0.98),(-4.10,1.45,0.82),80))
S.append(camshot('V4_06_COMPRESSOR_STAGES',14600,20400,(-4.1,-2.55,0.96),(-3.70,1.45,0.82),82,(-2.75,-2.45,0.98),(-3.05,1.45,0.82),78))
# Explicit thermal shot rather than looking at an empty tube.
S.append(camshot('V4_07_HEAT_RECOVERY',20400,23900,(-0.45,-4.35,1.75),(-0.40,1.65,0.88),62,(-0.9,-3.65,1.45),(-0.40,1.65,0.92),70))
S.append(camshot('V4_08_PIPE_DESCENT',23900,28700,(0.25,-4.8,1.05),(0.80,1.72,0.10),58,(1.0,-3.95,-0.95),(1.0,1.72,-1.55),66))
S.append(camshot('V4_09_CAVERN_REVEAL',28700,32500,(0.25,-4.2,-1.55),(1.0,1.72,-2.0),56,(1.45,-3.45,-1.90),(1.0,1.72,-2.0),64))
S.append(camshot('V4_10_STORAGE_CLOSE',32500,37100,(1.45,-3.30,-1.92),(1.0,1.72,-2.0),68,(0.55,-3.05,-1.78),(1.0,1.72,-2.0),62))
S.append(camshot('V4_11_REVERSE_PIPE',37100,40700,(1.0,-4.0,-1.08),(1.60,1.72,-0.75),58,(3.25,-4.2,1.05),(3.50,1.55,0.80),62))
S.append(camshot('V4_12_TURBINE_MACRO',40700,46000,(3.45,-3.1,1.08),(4.10,1.55,0.82),76,(5.00,-2.75,1.12),(4.75,1.55,0.82),70))
S.append(camshot('V4_13_GENERATOR',46000,49500,(5.55,-4.15,1.45),(6.15,1.55,0.82),66,(6.35,-3.55,1.35),(6.20,1.55,0.82),72))
S.append(camshot('V4_14_GRID_CITY',49500,52380,(7.45,-6.1,2.10),(8.65,1.60,1.00),54,(8.85,-5.3,2.25),(8.90,1.70,1.00),52))
# Summary is two vertical-friendly hero cuts instead of one unusable super-wide.
S.append(camshot('V4_15_SUMMARY_PRESSURE',52380,56280,(-2.2,-7.2,2.6),(-1.4,0.6,0.65),54,(-1.0,-7.4,2.55),(-0.5,0.7,0.55),52))
S.append(camshot('V4_16_SUMMARY_ELECTRIC',56280,59080,(3.5,-7.4,2.55),(4.6,0.9,0.70),52,(5.4,-7.0,2.45),(6.0,1.05,0.70),54))
scene.camera=S[0]['cam']

# -------------------------
# Opening air particles: visible response to phrase "usando apenas ar".
# -------------------------
FX=bpy.data.collections.get('MOTION_FX')
random.seed(104)
for i in range(28):
    a=2*math.pi*i/28; rad=0.65+0.22*math.sin(i*2.7); z=1.15+0.55*math.sin(a*1.6)
    p=sphere(f'FX_intro_air_v4_{i:02d}',(-0.8+math.cos(a)*rad,-1.38+0.22*math.sin(a),z),(0.025,0.025,0.025),M_CYAN,FX,12)
    p.scale=(0.001,)*3; p.keyframe_insert('scale',frame=fr(2150)); p.scale=(1,)*3; p.keyframe_insert('scale',frame=fr(2550+i*12));
    # slow orbit/drift
    p.location=(-0.8+math.cos(a+0.55)*rad,-1.36+0.25*math.sin(a+0.55),z+0.08*math.sin(i)); p.keyframe_insert('location',frame=fr(3700)); p.scale=(0.001,)*3; p.keyframe_insert('scale',frame=fr(4200))

# Pressure counter-like visual rings around compressor exit.
for i in range(5):
    o=torus(f'FX_pressure_v4_{i}',(-2.55+i*0.04,1.45,0.82),0.16+0.055*i,0.013,M_CYAN,FX,rot=(0,math.pi/2,0))
    o.scale=(0.01,)*3; o.keyframe_insert('scale',frame=fr(16600+i*140)); o.scale=(1.0,)*3; o.keyframe_insert('scale',frame=fr(17900+i*160)); o.scale=(1.35,)*3; o.keyframe_insert('scale',frame=fr(19900)); o.scale=(0.01,)*3; o.keyframe_insert('scale',frame=fr(20400))

# Stronger generator-to-grid pulse on the final energy delivery phrase.
for i in range(9):
    x=7.0+i*0.38
    p=sphere(f'FX_grid_flash_v4_{i}',(x,1.12+0.05*math.sin(i),0.95+0.06*i),(0.045,)*3,M_CYAN,FX,12)
    p.scale=(0.01,)*3; p.keyframe_insert('scale',frame=fr(47800+i*250)); p.scale=(1.25,)*3; p.keyframe_insert('scale',frame=fr(48300+i*250)); p.scale=(0.01,)*3; p.keyframe_insert('scale',frame=fr(49100+i*250))

# More natural antenna secondary motion.
for side,mult in [('L',1),('R',-1)]:
    o=bpy.data.objects.get('NEXO_antenna_'+side)
    if o:
        o.rotation_mode='XYZ'
        for k,ms in enumerate(range(0,59000,1500)):
            o.rotation_euler.y=0.025*mult*math.sin(k*1.7); o.rotation_euler.z=0.018*math.sin(k*1.1); o.keyframe_insert('rotation_euler',frame=fr(ms))

OUTV4='output_caes_motion_v4'; os.makedirs(OUTV4,exist_ok=True)
scene.frame_set(1); scene.camera=S[0]['cam']
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUTV4,'CAES_MOTION_V4_MASTER.blend'))
if os.environ.get('CAES_V4_PREVIEW','0')=='1':
    for s in S:
        scene.frame_set(fr((s['s']+s['e'])//2)); scene.render.filepath=os.path.join(OUTV4,'preview_'+s['name']+'.jpg'); bpy.ops.render.render(write_still=True)
start=int(os.environ.get('START_FRAME','0') or '0'); end=int(os.environ.get('END_FRAME','0') or '0')
if start>0 and end>=start:
    scene.frame_start=start; scene.frame_end=end; os.makedirs(os.path.join(OUTV4,'frames'),exist_ok=True); scene.render.filepath=os.path.join(OUTV4,'frames','frame_'); bpy.ops.render.render(animation=True)
print('CAES MOTION V4 READY',END_FRAME)
