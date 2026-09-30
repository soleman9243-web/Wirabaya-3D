import re

with open('Assets/_Recovery/0 (29).unity', 'r') as f:
    text = f.read()

# Build mapping of fileID -> block
blocks = {}
current_id = None
current_lines = []

for line in text.split('\n'):
    if line.startswith('--- !u!'):
        if current_id:
            blocks[current_id] = '\n'.join(current_lines)
        m = re.match(r'--- !u!\d+ &(-?\d+)', line)
        current_id = m.group(1) if m else None
        current_lines = [line]
    else:
        current_lines.append(line)
if current_id:
    blocks[current_id] = '\n'.join(current_lines)

print('Total blocks in scene:', len(blocks))

# Find bandage GameObjects
for id, block in blocks.items():
    if 'GameObject:' in block and 'm_Name: bandage.' in block:
        name = re.search(r'm_Name: (bandage\.\d+)', block).group(1)
        comps = re.findall(r'- component: \{fileID: (-?\d+)\}', block)
        print(f'{name} (GO ID {id}):')
        for comp_id in comps:
            comp_block = blocks.get(comp_id, '')
            if 'm_Materials:' in comp_block:
                mats = re.findall(r'guid: ([a-f0-9]+)', comp_block)
                print(f'   Renderer {comp_id}: mats={mats}')
