import bpy, os, math, json, random
from mathutils import Vector

BASE='nexo/refine_nexo_mk4_caes_stage.py'
exec(compile(open(BASE,'rb').read(), BASE, 'exec'), globals(), globals())

CUES=json.load(open('nexo/caes_audio_cues_v3.json','r',encoding='utf-8'))
FPS=24
DURATION_MS=CUES['duration_ms']
END_FRAME=math.ceil(DURATION_MS/1000*FPS)

def fr(ms): return max(1, round(ms*FPS/1000)+1)

def interp(action, mode='BEZIER'):
    if action:
        for fc in action.fcurves:
            for kp in fc.keyframe_points:
                kp.interpolation=mode
                if mode=='BEZIER':
                    kp.handle_left_type='AUTO_CLAMPED'; kp.handle_right_type='AUTO_CLAMPED'

def key_obj(o, frame, loc=None, rot=None, scale=None):
    if loc is not None: o.location=loc; o.keyframe_insert('location',frame=frame)
    if rot is not None: o.rotation_euler=rot; o.keyframe_insert('rotation_euler',frame=frame)
    if scale is not None: o.scale=scale; o.keyframe_insert('scale',frame=frame)

def key_bone(name, frame, rot):
    b=arm.pose.bones.get(name)
    if not b: return
    b.rotation_mode='XYZ'; b.rotation_euler=rot; b.keyframe_insert('rotation_euler',frame=frame)

def reset_pose(frame):
    for n in ['head','neck','clavicle.L','clavicle.R','upper_arm.L','upper_arm.R','forearm.L','forearm.R','hand.L','hand.R']:
        key_bone(n,frame,(0,0,0))

def pose(frame, **kw):
    mapping={'head':'head','neck':'neck','clavL':'clavicle.L','clavR':'clavicle.R','armL':'upper_arm.L','armR':'upper_arm.R','foreL':'forearm.L','foreR':'forearm.R','handL':'hand.L','handR':'hand.R'}
    for k,v in kw.items(): key_bone(mapping[k], frame, v)

scene=bpy.context.scene
scene.frame_start=1; scene.frame_end=END_FRAME; scene.render.fps=FPS
scene.render.resolution_x=720; scene.render.resolution_y=1280; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='JPEG'; scene.render.image_settings.quality=92
scene.render.engine='BLENDER_WORKBENCH'
try:
    scene.display.shading.color_type='MATERIAL'; scene.display.shading.light='STUDIO'; scene.display.shading.show_shadows=True; scene.display.shading.show_cavity=True; scene.display.shading.cavity_type='WORLD'; scene.display.shading.curvature_ridge_factor=1.8; scene.display.shading.curvature_valley_factor=1.3
except: pass
try: scene.view_settings.look='AgX - Medium High Contrast'
except: pass
scene.world.color=(0.012,0.018,0.030)
for m in list(scene.timeline_markers): scene.timeline_markers.remove(m)

SHOTS=[
 {'name':'PRO_01_INTRO_CLOSE','s':0,'e':2300},{'name':'PRO_02_AIR_REVEAL','s':2300,'e':4300},{'name':'PRO_03_RENEWABLES','s':4300,'e':8900},{'name':'PRO_04_TO_COMPRESSOR','s':8900,'e':12400},{'name':'PRO_05_COMPRESSOR_INTERNAL','s':12400,'e':16700},{'name':'PRO_06_PRESSURE_HEAT','s':16700,'e':23900},{'name':'PRO_07_PIPE_DESCENT','s':23900,'e':28700},{'name':'PRO_08_CAVERN_REVEAL','s':28700,'e':32500},{'name':'PRO_09_STORAGE_HOLD','s':32500,'e':37100},{'name':'PRO_10_REVERSE_FLOW','s':37100,'e':40700},{'name':'PRO_11_TURBINE','s':40700,'e':46000},{'name':'PRO_12_GENERATOR_GRID','s':46000,'e':52380},{'name':'PRO_13_SUMMARY','s':52380,'e':59080},]

cams=[]
def C(name,loc0,t0,l0,loc1=None,t1=None,l1=None):
    s=next(x for x in SHOTS if x['name']==name)
    cd=bpy.data.cameras.new(name+'_DATA'); cd.lens=l0; cd.sensor_width=36; cd.sensor_fit='VERTICAL'
    coll=bpy.data.collections.get('PRO_CAMERAS') or bpy.data.collections.new('PRO_CAMERAS')
    if coll.name not in scene.collection.children: scene.collection.children.link(coll)
    cam=bpy.data.objects.new(name,cd); coll.objects.link(cam)
    target=bpy.data.objects.new(name+'_TARGET',None); coll.objects.link(target)
    con=cam.constraints.new('TRACK_TO'); con.target=target; con.track_axis='TRACK_NEGATIVE_Z'; con.up_axis='UP_Y'
    f0=fr(s['s']); f1=fr(s['e'])
    key_obj(cam,f0,loc=loc0); cd.lens=l0; cd.keyframe_insert('lens',frame=f0); key_obj(target,f0,loc=t0)
    key_obj(cam,f1,loc=loc1 or loc0); cd.lens=l1 or l0; cd.keyframe_insert('lens',frame=f1); key_obj(target,f1,loc=t1 or t0)
    interp(cam.animation_data.action); interp(target.animation_data.action)
    marker=scene.timeline_markers.new(name,frame=f0); marker.camera=cam
    cams.append(cam); return cam

C('PRO_01_INTRO_CLOSE',(-0.8,-5.0,1.88),(-0.8,-1.15,1.45),70,(-0.8,-7.1,2.20),(-0.8,-1.05,1.15),58)
C('PRO_02_AIR_REVEAL',(-0.35,-5.7,1.65),(-0.4,1.65,0.90),68,(0.35,-5.2,1.40),(0.5,1.72,0.35),58)
C('PRO_03_RENEWABLES',(-7.7,-6.6,2.10),(-7.8,1.25,1.25),60,(-7.4,-5.3,2.75),(-7.8,1.65,1.40),52)
C('PRO_04_TO_COMPRESSOR',(-6.1,-5.8,1.55),(-5.0,0.65,0.62),56,(-3.65,-4.35,1.55),(-3.65,1.38,0.82),66)
C('PRO_05_COMPRESSOR_INTERNAL',(-4.90,-2.65,0.90),(-3.95,1.45,0.82),72,(-2.95,-2.15,0.95),(-3.15,1.45,0.82),78)
C('PRO_06_PRESSURE_HEAT',(-3.0,-3.8,1.48),(-2.85,1.45,0.82),62,(-0.65,-4.2,1.60),(-0.40,1.65,0.88),60)
C('PRO_07_PIPE_DESCENT',(0.15,-4.8,0.95),(0.75,1.72,0.20),58,(1.0,-4.0,-1.10),(1.0,1.72,-1.65),64)
C('PRO_08_CAVERN_REVEAL',(0.1,-4.0,-1.65),(1.0,1.70,-2.0),58,(1.55,-3.4,-1.92),(1.0,1.75,-2.0),64)
C('PRO_09_STORAGE_HOLD',(1.60,-3.55,-1.80),(1.0,1.75,-2.0),62,(0.45,-3.25,-1.72),(1.0,1.75,-2.0),58)
C('PRO_10_REVERSE_FLOW',(1.1,-4.2,-1.10),(1.6,1.72,-0.85),58,(3.4,-4.0,1.05),(3.5,1.55,0.80),62)
C('PRO_11_TURBINE',(3.45,-3.35,1.10),(4.20,1.55,0.82),72,(5.15,-3.0,1.20),(4.85,1.55,0.82),66)
C('PRO_12_GENERATOR_GRID',(5.5,-4.0,1.30),(6.1,1.55,0.82),66,(8.8,-5.4,2.15),(8.7,1.55,0.95),54)
C('PRO_13_SUMMARY',(-0.8,-11.0,3.35),(0.4,1.2,0.52),52,(0.5,-12.4,3.75),(0.8,1.2,0.45),48)
scene.camera=cams[0]

if arm.animation_data: arm.animation_data_clear()
for b in arm.pose.bones: b.rotation_mode='XYZ'
positions=[(0,(-0.8,-1.2,0)),(4300,(-6.0,-0.95,0)),(8900,(-2.0,-0.95,0)),(23900,(2.5,-0.95,0)),(37100,(3.0,-0.95,0)),(46000,(6.9,-0.95,0)),(52380,(0,-1.20,0)),(59080,(0,-1.20,0))]
for ms,pos in positions: arm.location=pos; arm.keyframe_insert('location',frame=fr(ms))
interp(arm.animation_data.action,'CONSTANT')
for ms in [0,2300,4300,8900,12400,16700,23900,28700,32500,37100,40700,46000,52380,59080]: reset_pose(fr(ms))
pose(fr(500), head=(0.02,0,-0.06), clavL=(0,0,-0.08), clavR=(0,0,0.08), armL=(0.03,0.08,0.38), foreL=(0,0,0.35), armR=(0.03,-0.08,-0.38), foreR=(0,0,-0.35))
pose(fr(1800), head=(-0.02,0,0.04), armL=(0,0,0.18), foreL=(0,0,0.20), armR=(0,0,-0.18), foreR=(0,0,-0.20))
pose(fr(5000), head=(-0.04,0,0.28), clavL=(0,0,-0.08), armL=(-0.05,0.10,0.86), foreL=(0.0,0.0,0.32), handL=(0,0,0.16))
pose(fr(7200), head=(0.02,0,0.18), armL=(0.02,0.08,0.58), foreL=(0,0,0.44))
pose(fr(9800), head=(0.01,0,-0.18), armR=(0.02,-0.08,-0.68), foreR=(0,0,-0.40), handR=(0,0,-0.12))
pose(fr(11600), head=(0.02,0,-0.24), armR=(0.02,-0.10,-0.82), foreR=(0,0,-0.22))
pose(fr(13200), head=(0.01,0,-0.16), armR=(0.02,-0.08,-0.72), foreR=(0,0,-0.44))
pose(fr(15400), armR=(0.0,-0.06,-0.42), foreR=(0,0,-0.68))
pose(fr(17800), head=(0.0,0,-0.04), armL=(0,0,0.46), foreL=(0,0,0.60), armR=(0,0,-0.46), foreR=(0,0,-0.60))
pose(fr(19700), armL=(0,0,0.22), foreL=(0,0,0.35), armR=(0,0,-0.22), foreR=(0,0,-0.35))
pose(fr(21900), head=(0.02,0,0.22), armL=(0.02,0.08,0.62), foreL=(0,0,0.34))
pose(fr(24800), head=(0.12,0,0.10), armR=(0.05,-0.06,-0.38), foreR=(0,0,-0.85))
pose(fr(30000), head=(0.10,0,0.04), armR=(0.02,-0.05,-0.22), foreR=(0,0,-0.35))
pose(fr(34000), head=(-0.02,0,0.00), armL=(0,0,0.16), foreL=(0,0,0.18), armR=(0,0,-0.16), foreR=(0,0,-0.18))
pose(fr(38200), head=(0.02,0,-0.22), armR=(0,-0.08,-0.56), foreR=(0,0,-0.44))
pose(fr(42000), head=(0.01,0,-0.24), armR=(0,-0.10,-0.78), foreR=(0,0,-0.22))
pose(fr(44800), armR=(0,-0.05,-0.45), foreR=(0,0,-0.55))
pose(fr(47200), head=(0.0,0,-0.18), armR=(0,-0.08,-0.55), foreR=(0,0,-0.38))
pose(fr(50500), head=(0.0,0,-0.30), armR=(0,-0.12,-0.88), foreR=(0,0,-0.20))
pose(fr(53200), head=(0.0,0,0.0), armL=(0,0,0.40), foreL=(0,0,0.28), armR=(0,0,-0.40), foreR=(0,0,-0.28))
pose(fr(57000), head=(-0.02,0,0.03), armL=(0,0,0.18), foreL=(0,0,0.20), armR=(0,0,-0.18), foreR=(0,0,-0.20))
if arm.animation_data and arm.animation_data.action: interp(arm.animation_data.action,'BEZIER')
for ms,ang in [(3500,0.035),(8200,-0.025),(16400,0.030),(23600,-0.025),(32100,0.028),(36700,-0.022),(40400,0.030),(45700,-0.025),(52000,0.025),(58800,0)]: key_bone('head',fr(ms),(0.015,0,ang))
for side in ('L','R'):
    eye=arm.pose.bones.get(f'eye.{side}')
    if eye:
        for ms,rot in [(0,(0,0,0)),(4500,(0.00,0.10,0.18)),(9000,(0,-0.08,-0.20)),(23900,(0.10,0.04,0.06)),(37100,(0,-0.08,-0.20)),(46000,(0,-0.10,-0.26)),(52380,(0,0,0)),(59080,(0,0,0))]: eye.rotation_mode='XYZ'; eye.rotation_euler=rot; eye.keyframe_insert('rotation_euler',frame=fr(ms))
        interp(arm.animation_data.action,'BEZIER')
blink_times=[3700,8200,12100,16400,20400,23800,28600,32300,36800,40500,45600,52000,55800]
for side in ('L','R'):
    for pref in ('NEXO_eye_','NEXO_iris_','NEXO_pupil_','NEXO_eye_glint_'):
        o=bpy.data.objects.get(pref+side)
        if not o: continue
        if o.animation_data: o.animation_data_clear()
        for ms in blink_times:
            f=fr(ms); o.scale=(1,1,1); o.keyframe_insert('scale',frame=f-2); o.scale=(1,1,0.08); o.keyframe_insert('scale',frame=f); o.scale=(1,1,1); o.keyframe_insert('scale',frame=f+2)
        interp(o.animation_data.action,'BEZIER')
mouth=bpy.data.objects.get('NEXO_mouth_smile')
if mouth:
    if mouth.animation_data: mouth.animation_data_clear()
    mouth.scale=(1,1,0.55); mouth.keyframe_insert('scale',frame=1)
    for seg in CUES['segments']:
        words=max(1,len(seg['t'].split())); span=seg['e']-seg['s']
        for i in range(words):
            s=seg['s']+span*i/words; e=seg['s']+span*(i+0.72)/words; f0=fr(s); f1=fr((s+e)/2); f2=fr(e)
            mouth.scale=(0.92,1,0.45); mouth.keyframe_insert('scale',frame=f0); mouth.scale=(1.08,1,1.28 if i%3 else 1.55); mouth.keyframe_insert('scale',frame=f1); mouth.scale=(0.95,1,0.50); mouth.keyframe_insert('scale',frame=f2)
    interp(mouth.animation_data.action,'BEZIER')

FX=bpy.data.collections.get('MOTION_FX')
if not FX: FX=bpy.data.collections.new('MOTION_FX'); scene.collection.children.link(FX)
def fx_sphere(name, loc, radius, matl): return sphere(name,loc,(radius,radius,radius),matl,FX,16)
def animate_path(prefix, points, start_ms, end_ms, count, matl, radius=0.055):
    dur=(end_ms-start_ms)/count
    for j in range(count):
        o=fx_sphere(f'{prefix}_{j:02d}',points[0],radius,matl)
        if o.animation_data: o.animation_data_clear()
        st=start_ms+j*dur*0.55; en=min(end_ms,st+dur*1.7); key_obj(o,fr(max(0,st-120)),scale=(0.001,0.001,0.001)); key_obj(o,fr(st),scale=(1,1,1))
        for k,p in enumerate(points): key_obj(o,fr(st+(en-st)*k/(len(points)-1)),loc=p)
        key_obj(o,fr(en),scale=(1,1,1)); key_obj(o,fr(en+120),scale=(0.001,0.001,0.001)); interp(o.animation_data.action,'LINEAR')

animate_path('FX_energy_in',[(-8.4,1.0,0.6),(-7.0,0.8,0.52),(-5.6,0.6,0.50),(-4.6,0.8,0.62),(-3.9,1.25,0.76)],6500,12400,10,M_CYAN,0.055)
random.seed(7)
for j in range(20):
    a=2*math.pi*(j%8)/8; rr=0.30*(0.45+0.55*((j*37)%100)/100); o=fx_sphere(f'FX_comp_air_{j:02d}',(-5.25,1.45+math.cos(a)*rr,0.82+math.sin(a)*rr),0.035,M_CYAN)
    if o.animation_data: o.animation_data_clear()
    st=12400+j*110; en=20100+j*45; key_obj(o,fr(st),scale=(0.35,0.35,0.35)); stages=[-5.15,-4.65,-4.25,-3.85,-3.45,-3.05,-2.55,-2.20]
    for k,x in enumerate(stages):
        shrink=1-0.78*k/(len(stages)-1); p=(x,1.45+math.cos(a)*rr*shrink,0.82+math.sin(a)*rr*shrink); key_obj(o,fr(st+(en-st)*k/(len(stages)-1)),loc=p,scale=(0.55+0.45*k/(len(stages)-1),)*3)
    key_obj(o,fr(en+250),scale=(0.001,0.001,0.001)); interp(o.animation_data.action,'LINEAR')
for i in range(4):
    r=torus(f'FX_heat_ring_{i}',(-0.4,1.65,0.85),0.28+0.08*i,0.018,M_AMBER,FX)
    if r.animation_data: r.animation_data_clear()
    for ms,sc in [(20400+i*220,0.15),(21300+i*220,1.0),(22900+i*180,1.55),(23500,0.01)]: key_obj(r,fr(ms),scale=(sc,sc,sc))
    interp(r.animation_data.action,'BEZIER')
animate_path('FX_charge',[(-2.2,1.45,0.82),(-0.5,1.65,0.82),(0.4,1.75,0.65),(1.0,1.75,-0.2),(1.0,1.75,-1.55),(1.0,1.75,-2.0)],23900,32000,16,M_CYAN,0.050)
particles=[o for o in bpy.data.objects if o.name.startswith('Air_particle_')]
for idx,o in enumerate(particles):
    if o.animation_data: o.animation_data_clear()
    st=24500+(idx%35)*180; base=o.scale.copy(); key_obj(o,fr(st),scale=(0.001,0.001,0.001)); key_obj(o,fr(st+600),scale=base); key_obj(o,fr(33500+(idx%5)*80),scale=tuple(v*1.0 for v in base)); key_obj(o,fr(35500+(idx%7)*65),scale=tuple(v*1.25 for v in base)); key_obj(o,fr(37100),scale=base); key_obj(o,fr(40500+(idx%8)*50),scale=tuple(v*0.75 for v in base)); interp(o.animation_data.action,'BEZIER')
for o in bpy.data.objects:
    if o.name.startswith('Cavern_pressure_ring_') or o.name.startswith('Cavern_pressure_line_'):
        if o.animation_data: o.animation_data_clear()
        key_obj(o,fr(28700),scale=(0.7,0.7,0.7)); key_obj(o,fr(33000),scale=(1.08,1.08,1.08)); key_obj(o,fr(36500),scale=(1,1,1)); interp(o.animation_data.action,'BEZIER')
animate_path('FX_release',[(1.0,1.75,-2.0),(1.0,1.75,-1.3),(2.1,1.70,-0.60),(2.9,1.62,0.30),(3.55,1.55,0.80),(4.25,1.55,0.82)],37100,45600,18,M_AMBER,0.055)
for s in range(4):
    rr=bpy.data.objects.get(f'Turb_rotor_{s}')
    if rr:
        if rr.animation_data: rr.animation_data_clear()
        rr.rotation_mode='XYZ'; rr.rotation_euler.x=0; rr.keyframe_insert('rotation_euler',frame=fr(40700)); rr.rotation_euler.x=math.radians(360*85); rr.keyframe_insert('rotation_euler',frame=fr(51800)); interp(rr.animation_data.action,'LINEAR')
for o in bpy.data.objects:
    if o.name.startswith('Generator_ring_'):
        if o.animation_data: o.animation_data_clear()
        for ms,sc in [(46000,0.92),(47600,1.08),(49300,0.96),(51000,1.10),(52300,1.0)]: key_obj(o,fr(ms),scale=(sc,sc,sc))
        interp(o.animation_data.action,'BEZIER')
animate_path('FX_grid',[(6.55,1.55,0.82),(7.4,1.3,0.95),(8.2,1.1,1.15),(9.0,1.0,1.30),(10.0,1.0,1.45)],47000,52380,14,M_CYAN,0.052)
for s in range(5):
    rr=bpy.data.objects.get(f'Comp_rotor_{s}')
    if rr:
        if rr.animation_data: rr.animation_data_clear()
        rr.rotation_mode='XYZ'; rr.rotation_euler.x=0; rr.keyframe_insert('rotation_euler',frame=fr(12400)); rr.rotation_euler.x=math.radians(360*90); rr.keyframe_insert('rotation_euler',frame=END_FRAME); interp(rr.animation_data.action,'LINEAR')

OUT='output_caes_motion_v3'; os.makedirs(OUT,exist_ok=True)
scene.frame_set(1); scene.camera=cams[0]
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'CAES_MOTION_V3_MASTER.blend'))
preview=os.environ.get('CAES_PREVIEW','0')=='1'
if preview:
    for s in SHOTS:
        scene.frame_set(fr((s['s']+s['e'])//2)); scene.render.filepath=os.path.join(OUT,'preview_'+s['name']+'.jpg'); bpy.ops.render.render(write_still=True)
start=int(os.environ.get('START_FRAME','0') or '0'); end=int(os.environ.get('END_FRAME','0') or '0')
if start>0 and end>=start:
    scene.frame_start=start; scene.frame_end=end; scene.render.filepath=os.path.join(OUT,'frames','frame_'); os.makedirs(os.path.join(OUT,'frames'),exist_ok=True); bpy.ops.render.render(animation=True)
print('CAES MOTION V3 READY', END_FRAME)
