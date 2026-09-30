import re

with open('Assets/Asset/NekoLegends/Shaders/ShaderAnimeCel/CelShaderV2.shader', 'r') as f:
    text = f.read()

includes = re.findall(r'#include\s+["<]([^">]+)[">]', text)
print('Includes:')
for inc in sorted(set(includes)):
    print(' ', inc)
