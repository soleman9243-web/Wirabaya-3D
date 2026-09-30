from PIL import Image
import numpy as np

img = Image.open(r'C:\Users\USER\.gemini\antigravity-ide\brain\8ec212b0-47a6-4540-85c9-3abb785e94d8\.user_uploaded\media_1790648611004.png')
arr = np.array(img, dtype=float)

# High-pass filter (Laplacian or local difference) on R channel
from scipy.ndimage import laplace
# If scipy not available, manual laplacian:
r = arr[:,:,0]
lap = np.abs(r[:-2, 1:-1] + r[2:, 1:-1] + r[1:-1, :-2] + r[1:-1, 2:] - 4*r[1:-1, 1:-1])

# Save visualization of high frequency noise
norm_lap = np.clip(lap * 5, 0, 255).astype(np.uint8)
Image.fromarray(norm_lap).save('scratch/pants_laplacian.png')

# Find coordinates of top noisy regions inside pants (y: 100 to 450, x: 80 to 380)
# exclude blue bone lines (B > 150)
not_bone = arr[1:-1, 1:-1, 2] < 100
pants_mask = (arr[1:-1, 1:-1, 0] > 60) & (arr[1:-1, 1:-1, 1] > 60) & not_bone
pants_lap = lap * pants_mask

print('Max laplacian inside pants:', pants_lap.max())
y_max, x_max = np.unravel_index(np.argmax(pants_lap), pants_lap.shape)
print(f'Noisiest point inside pants: y={y_max+1}, x={x_max+1}, lap={pants_lap[y_max, x_max]}')

# Print surrounding 11x11 R values
print('Surrounding R values:')
for row in arr[y_max-5:y_max+6, x_max-5:x_max+6, 0].astype(int):
    print(' '.join(f'{v:3d}' for v in row))
