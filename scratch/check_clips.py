import re

with open('Assets/Asset/NekoLegends/Shaders/ShaderAnimeCel/CelShaderV2.shader', 'r') as f:
    text = f.read()

clips = re.findall(r'.{0,60}clip\(.{0,60}', text)
print('clip() calls in CelShaderV2.shader:', len(clips))
for c in clips:
    print('  ', c.strip())
