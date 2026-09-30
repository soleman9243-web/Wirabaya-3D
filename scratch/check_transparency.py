from PIL import Image
import numpy as np

img = Image.open(r'C:\Users\USER\.gemini\antigravity-ide\brain\8ec212b0-47a6-4540-85c9-3abb785e94d8\.user_uploaded\media_1790652444461.png')
arr = np.array(img)

# Crop the brown strip (left side: x ~ 100 to 200, y ~ 150 to 300)
crop_brown = arr[150:300, 100:200]
Image.fromarray(crop_brown).save('scratch/crop_brown_strip.png')

# Print min, max, mean of R, G, B in the brown strip
print('Brown strip RGB stats:')
print('R:', crop_brown[:,:,0].min(), crop_brown[:,:,0].max(), crop_brown[:,:,0].mean())
print('G:', crop_brown[:,:,1].min(), crop_brown[:,:,1].max(), crop_brown[:,:,1].mean())
print('B:', crop_brown[:,:,2].min(), crop_brown[:,:,2].max(), crop_brown[:,:,2].mean())

# Check the background tree at the same Y level to the left/right of the strip
# Background forest is at y ~ 50 to 150
crop_bg = arr[50:120, 100:200]
print('\nBackground forest RGB stats at same X:')
print('R:', crop_bg[:,:,0].min(), crop_bg[:,:,0].max(), crop_bg[:,:,0].mean())
print('G:', crop_bg[:,:,1].min(), crop_bg[:,:,1].max(), crop_bg[:,:,1].mean())
print('B:', crop_bg[:,:,2].min(), crop_bg[:,:,2].max(), crop_bg[:,:,2].mean())
