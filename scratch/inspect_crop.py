from PIL import Image
import numpy as np

img = Image.open(r'C:\Users\USER\.gemini\antigravity-ide\brain\8ec212b0-47a6-4540-85c9-3abb785e94d8\.user_uploaded\media_1790649989434.png')
print('Image size (W, H):', img.size)
arr = np.array(img)
print('Array shape (H, W, C):', arr.shape)

# Let's inspect the strip on the right side: x ~ 430 to 520, y ~ 200 to 300
crop = arr[200:300, 440:500]
print('R min, max, mean:', crop[:,:,0].min(), crop[:,:,0].max(), crop[:,:,0].mean())
print('G min, max, mean:', crop[:,:,1].min(), crop[:,:,1].max(), crop[:,:,1].mean())
print('B min, max, mean:', crop[:,:,2].min(), crop[:,:,2].max(), crop[:,:,2].mean())

print('\n10x10 patch of Red channel in this strip:')
for row in crop[40:50, 20:30, 0]:
    print(' '.join(f'{val:3d}' for val in row))

print('\n10x10 patch of Green channel in this strip:')
for row in crop[40:50, 20:30, 1]:
    print(' '.join(f'{val:3d}' for val in row))
