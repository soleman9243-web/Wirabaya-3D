import re

with open('Assets/_Recovery/0 (29).unity', 'r') as f:
    text = f.read()

# Find all GameObjects with bandage
go_matches = list(re.finditer(r'--- !u!1 &(\d+)\nGameObject:\n(?:[^\n]+\n){1,10}\s+m_Name: (bandage\.\d+)', text))
print(f'Found {len(go_matches)} bandage GameObjects:')

for m in go_matches:
    go_id = m.group(1)
    name = m.group(2)
    # Find SkinnedMeshRenderer or MeshRenderer with m_GameObject: {fileID: go_id}
    renderer_match = re.search(r'--- !u!\d+ &(\d+)\n(?:SkinnedMeshRenderer|MeshRenderer):\n(?:[^\n]+\n)+?\s+m_GameObject: \{fileID: ' + go_id + r'\}(?:[^\n]+\n)+?\s+m_Materials:\n((?:\s+- [^\n]+\n)+)', text)
    if renderer_match:
        mats = renderer_match.group(2).strip().split('\n')
        print(f'  GameObject {go_id} ({name}):')
        for mat in mats:
            print(f'    {mat.strip()}')
    else:
        print(f'  GameObject {go_id} ({name}): No renderer found')
