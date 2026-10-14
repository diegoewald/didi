import os
from pathlib import Path

# Preserve chunk bounds because the production script deliberately shields V3 from rendering.
req_start=os.environ.get('START_FRAME','0')
req_end=os.environ.get('END_FRAME','0')
src=Path('nexo/produce_caes_motion_v4.py').read_text(encoding='utf-8')
src=src.replace("start=int(os.environ.get('START_FRAME','0') or '0'); end=int(os.environ.get('END_FRAME','0') or '0')", f"start=int({req_start!r}); end=int({req_end!r})")
exec(compile(src,'nexo/produce_caes_motion_v4.py','exec'),globals(),globals())
