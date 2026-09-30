import re

with open('Assets/Asset/NekoLegends/Shaders/ShaderAnimeCel/CelShaderV2.shader', 'r') as f:
    text = f.read()

# Find void or float or half functions defined
funcs = re.findall(r'(?:void|half\d?|float\d?)\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*\{', text)
print('Functions defined in CelShaderV2.shader:')
for fn in sorted(set(funcs)):
    if not fn.startswith('Build') and not fn.startswith('Unity'):
        print(' ', fn)
