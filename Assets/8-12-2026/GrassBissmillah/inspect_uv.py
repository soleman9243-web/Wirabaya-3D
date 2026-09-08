with open(r'Assets/IL3DN/Graphics/Meshes/IL3DN_Plant_Grass_02.fbx', 'rb') as f:
    data = f.read()

# Look for UV or LayerElementUV in FBX
idx = 0
while True:
    idx = data.find(b'UV', idx)
    if idx == -1: break
    print(f"Found UV at {idx}:", data[idx-20:idx+60])
    idx += 2
