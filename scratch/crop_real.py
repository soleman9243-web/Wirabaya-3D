from PIL import Image
import numpy as np

img = Image.open(r'C:\Users\USER\.gemini\antigravity-ide\brain\8ec212b0-47a6-4540-85c9-3abb785e94d8\.user_uploaded\media_1790652444461.png')
print('Image dimensions:', img.size)

# Let's crop right in the middle of the brown strip:
# In media_1790652444461.png, width is ? and height is ?
# Let's check image size
w, h = img.size
print(f'Width={w}, Height={h}')

# Let's save crops of various sizes from the brown strip
crop50 = img.crop((w*0.3, h*0.4, w*0.45, h*0.6))
crop50.save('scratch/brown_strip_real.png')

arr = np.array(crop50)
print('crop50 shape:', arr.shape)
print('crop50 R min, max, std:', arr[:,:,0].min(), arr[:,:,0].max(), arr[:,:,0].std())

# Let's look at 10x10 from crop50
print('10x10 slice:')
for row in arr[20:30, 20:30, 0]:
    print(' '.join(f'{v:3d}' for v in row))
