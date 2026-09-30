import re

with open('Assets/Asset/Animation/RawPlayerAnimation/WirabayaP_Idle.fbx', 'rb') as f:
    content = f.read()

models = re.findall(b'Model: \\d+, "Model::([^"]+)"', content)
print('Models found:', set(m.decode('utf-8', 'ignore') for m in models))

materials = re.findall(b'Material: \\d+, "Material::([^"]+)"', content)
print('Materials found:', set(m.decode('utf-8', 'ignore') for m in materials))
