from PIL import Image
import numpy as np

img = Image.open(r'C:\Users\USER\.gemini\antigravity-ide\brain\8ec212b0-47a6-4540-85c9-3abb785e94d8\.user_uploaded\media_1790648611004.png')
print('Image size:', img.size)
arr = np.array(img)

# Save a crop of the left leg shaded area
# Left leg is around x ~ 100 to 220, y ~ 150 to 350
# The outer edge / shaded area of left leg: x ~ 80 to 140, y ~ 200 to 280
crop_shaded = arr[200:260, 90:140]
Image.fromarray(crop_shaded).save('scratch/pants_shaded_crop.png')

# Print statistics of this shaded crop
print('R min, max, mean:', crop_shaded[:,:,0].min(), crop_shaded[:,:,0].max(), crop_shaded[:,:,0].mean())
print('G min, max, mean:', crop_shaded[:,:,1].min(), crop_shaded[:,:,1].max(), crop_shaded[:,:,1].mean())
print('B min, max, mean:', crop_shaded[:,:,2].min(), crop_shaded[:,:,2].max(), crop_shaded[:,:,2].mean())

# Look at 15x15 pixel values
print('\n15x15 R channel values in shaded pants:')
for row in crop_shaded[20:35, 15:30, 0]:
    print(' '.join(f'{v:3d}' for v in row))
