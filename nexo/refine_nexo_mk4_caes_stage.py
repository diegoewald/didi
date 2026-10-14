import bpy, os, math, json
from mathutils import Vector

# Build the base scene first, then perform the visual corrections found in review.
BASE='nexo/build_nexo_mk4_caes_stage.py'
exec(compile(open(BASE,'rb').read(), BASE, 'exec'), globals(), globals())

# ------------------------------------------------------------
# REVIEW PASS 2
# - underground shot was blocked by terrain volumes
# - compressor/turbine casings hid the explanatory internals
# - 3D labels cluttered the master wide shots
# - cavern needed an explicit cutaway presentation
# ------------------------------------------------------------

# 1) Turn the geological mass into a BACK cross-section wall instead of a block
#    between the camera and cavern.
soil=bpy.data.objects.get('Soil_layer')
deep=bpy.data.objects.get('Deep_earth')
if soil:
    soil.location.y=4.75
    soil.dimensions.y=0.32
if deep:
    deep.location.y=4.75
    deep.dimensions.y=0.32

# Add visible stratigraphy bands to make the cross-section read instantly.
for idx,(z,h,ma) in enumerate([
    (-0.18,0.12,M_GRASS),(-0.50,0.42,M_SOIL),(-0.96,0.16,M_ROCK),(-1.45,0.72,M_EARTH),(-2.18,0.12,M_ROCK),(-2.65,0.85,M_EARTH)
]):
    cube(f'CrossSection_band_{idx}',(0,4.53,z),(22,0.10,h),ma,COL_STAGE,0.01)

# 2) Compressor cutaway: remove the opaque barrel but preserve an industrial shell frame.
for n in ('Compressor_casing','Compressor_inner'):
    o=bpy.data.objects.get(n)
    if o: o.hide_render=True; o.hide_viewport=True
for i,x in enumerate((-4.62,-4.15,-3.68,-3.21,-2.74,-2.52)):
    torus(f'Comp_cutaway_ring_{i}',(x,1.45,0.82),0.51,0.035,M_STEEL2,COL_COMP,rot=(0,math.pi/2,0))
cube('Comp_cutaway_top',(-3.58,1.45,1.31),(2.20,0.16,0.13),M_STEEL2,COL_COMP,0.025)
cube('Comp_cutaway_bottom',(-3.58,1.45,0.33),(2.20,0.16,0.13),M_STEEL2,COL_COMP,0.025)
# stator rings between rotor stages
for i,x in enumerate((-4.00,-3.63,-3.26,-2.89)):
    torus(f'Comp_stator_{i}',(x,1.45,0.82),0.39,0.020,M_SILVER,COL_COMP,rot=(0,math.pi/2,0))

# 3) Turbine cutaway, same explanatory language.
o=bpy.data.objects.get('Turbine_casing')
if o: o.hide_render=True; o.hide_viewport=True
for i,x in enumerate((3.52,3.88,4.24,4.60,4.96)):
    torus(f'Turb_cutaway_ring_{i}',(x,1.55,0.82),0.49,0.035,M_STEEL2,COL_TURB,rot=(0,math.pi/2,0))
cube('Turb_cutaway_top',(4.25,1.55,1.29),(1.70,0.15,0.12),M_STEEL2,COL_TURB,0.025)
cube('Turb_cutaway_bottom',(4.25,1.55,0.35),(1.70,0.15,0.12),M_STEEL2,COL_TURB,0.025)

# 4) Cavern cutaway. Hide opaque volumes in preview and create a front-facing rock rim/back wall.
for n in ('CAES_Cavern_Rock','Cavern_air_volume'):
    o=bpy.data.objects.get(n)
    if o: o.hide_render=True; o.hide_viewport=True
back=sphere('Cavern_cutaway_back',(1.0,2.45,-2.02),(1.88,0.12,0.72),M_ROCK,COL_CAVERN,32)
rim1=torus('Cavern_cutaway_rim',(1.0,2.12,-2.0),1.46,0.105,M_ROCK,COL_CAVERN,rot=(math.pi/2,0,0)); rim1.scale.x=1.20; rim1.scale.z=0.52
rim2=torus('Cavern_cutaway_inner_rim',(1.0,2.10,-2.0),1.20,0.035,M_CYAN,COL_CAVERN,rot=(math.pi/2,0,0)); rim2.scale.x=1.22; rim2.scale.z=0.54
# pressure guide arcs / floor markers
for z in (-2.36,-2.00,-1.64):
    curve(f'Cavern_pressure_line_{z}',[(0.0,1.32,z),(1.0,1.23,z-0.04),(2.0,1.32,z)],M_CYAN,COL_CAVERN,0.014)

# 5) 3D labels are useful for layout but clutter final camera composition.
#    Keep them in Blender for reference while hiding them from preview renders.
for o in bpy.data.objects:
    if o.name.startswith('Label_'):
        o.hide_render=True

# 6) Improve shot framing after seeing the generated previews.
def setcam(name,loc,target,lens=None):
    c=bpy.data.objects.get(name)
    if not c: return
    c.location=loc
    if lens: c.data.lens=lens
    look_at(c,target)
setcam('CAM_01_INTRO',(-1.2,-11.8,3.3),(0.0,1.2,0.65),56)
setcam('CAM_02_RENEW',(-7.9,-7.4,2.8),(-7.8,1.2,1.05),60)
setcam('CAM_03_COMP',(-3.7,-5.5,2.0),(-3.60,1.42,0.82),64)
setcam('CAM_04_CAVERN',(1.0,-5.9,-1.10),(1.0,1.85,-2.00),54)
setcam('CAM_05_TURB',(4.9,-5.5,2.0),(4.9,1.50,0.82),63)
setcam('CAM_06_SUMMARY',(-0.6,-13.0,3.2),(0.5,1.2,0.62),54)

# 7) A clearer NEXO presentation pose: shoulders sit lower, elbows out slightly,
#    hands open away from torso instead of collapsing toward it.
pb=arm.pose.bones
for f in (1,1200,1320):
    pb['clavicle.L'].rotation_euler=(0,0,-0.05)
    pb['clavicle.R'].rotation_euler=(0,0,0.05)
    pb['clavicle.L'].keyframe_insert('rotation_euler',frame=f)
    pb['clavicle.R'].keyframe_insert('rotation_euler',frame=f)

# 8) Add explicit shot-note empties for later audio sync / semantic cues.
cue_data=[
 ('CUE_SURPLUS',145,(-7.6,-0.35,0.55)),
 ('CUE_COMPRESS',337,(-3.6,-0.35,0.55)),
 ('CUE_HEAT',505,(-0.4,-0.35,0.55)),
 ('CUE_STORAGE',601,(1.0,-0.35,-0.20)),
 ('CUE_RELEASE',865,(3.4,-0.35,0.55)),
 ('CUE_GENERATE',1010,(6.1,-0.35,0.55)),
 ('CUE_SUMMARY',1129,(0.0,-0.35,0.55)),
]
for name,frame,loc in cue_data:
    e=empty(name,loc,COL_CTRL,'SPHERE',0.18)
    e['cue_frame']=frame

# Re-render all six reviewed shots.
review_shots=[
 ('SHOT_01_INTRO','CAM_01_INTRO',1),
 ('SHOT_02_RENEWABLES','CAM_02_RENEW',145),
 ('SHOT_03_COMPRESSOR','CAM_03_COMP',337),
 ('SHOT_04_CAVERN','CAM_04_CAVERN',601),
 ('SHOT_05_TURBINE','CAM_05_TURB',865),
 ('SHOT_06_SUMMARY','CAM_06_SUMMARY',1129),
]
for base_name,cam_name,frame in review_shots:
    scene.camera=bpy.data.objects.get(cam_name)
    scene.frame_set(frame)
    scene.render.filepath=os.path.join(OUT,base_name+'_V2.png')
    bpy.ops.render.render(write_still=True)

# Add review metadata.
review={
 'version':'MK4_CAEs_STAGE_V2',
 'fixes':['arms/clavicles presentation pose','compressor cutaway','turbine cutaway','underground cross-section visibility','cavern cutaway','cleaner camera framing','hidden 3D labels in renders','audio-sync cue empties'],
 'cue_frames':{n:f for n,f,_ in cue_data},
 'status':'motion-ready blocking base; final materials/audio sync still pending'
}
with open(os.path.join(OUT,'REVIEW_V2.json'),'w',encoding='utf-8') as f: json.dump(review,f,indent=2,ensure_ascii=False)

# Save a distinct reviewed master file.
scene.frame_set(1); scene.camera=bpy.data.objects.get('CAM_01_INTRO')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'NEXO_MK4_CAES_MASTER_V2.blend'))
print('REFINE PASS COMPLETE')
