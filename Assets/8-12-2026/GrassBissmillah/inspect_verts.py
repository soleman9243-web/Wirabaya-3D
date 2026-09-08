import struct
import zlib

with open(r'Assets/IL3DN/Graphics/Meshes/IL3DN_Plant_Grass_02.fbx', 'rb') as f:
    data = f.read()

idx = data.find(b'Vertices')
while idx != -1:
    prop_type = data[idx+8:idx+9]
    print(f"Prop type at {idx}: {prop_type}")
    if prop_type == b'd':
        arr_len, enc, comp_len = struct.unpack('<III', data[idx+9:idx+21])
        raw = data[idx+21:idx+21+comp_len] if enc == 1 else data[idx+21:idx+21+arr_len*8]
        if enc == 1: raw = zlib.decompress(raw)
        coords = struct.unpack(f'<{arr_len}d', raw)
        xs = coords[0::3]
        ys = coords[1::3]
        zs = coords[2::3]
        print(f"Total vertices: {len(xs)}")
        print(f"X range: [{min(xs):.3f}, {max(xs):.3f}]")
        print(f"Y range: [{min(ys):.3f}, {max(ys):.3f}]")
        print(f"Z range: [{min(zs):.3f}, {max(zs):.3f}]")
        break
    idx = data.find(b'Vertices', idx+1)
