import os
from pathlib import Path
req_start=os.environ.get('START_FRAME','0'); req_end=os.environ.get('END_FRAME','0')
src=Path('nexo/produce_caes_motion_v4.py').read_text(encoding='utf-8')
src=src.replace("scene.render.engine='BLENDER_EEVEE'","scene.render.engine='BLENDER_WORKBENCH'")
src=src.replace("scene.render.resolution_x=720; scene.render.resolution_y=1280; scene.render.resolution_percentage=100","scene.render.resolution_x=540; scene.render.resolution_y=960; scene.render.resolution_percentage=100")
src=src.replace("start=int(os.environ.get('START_FRAME','0') or '0'); end=int(os.environ.get('END_FRAME','0') or '0')", f"start=int({req_start!r}); end=int({req_end!r})")
exec(compile(src,'nexo/produce_caes_motion_v4.py','exec'),globals(),globals())
