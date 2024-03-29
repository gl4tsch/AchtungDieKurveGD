#[compute]
#version 460

// Instruct the GPU to use 8x8x1 = 64 local invocations per workgroup.
layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

// Prepare memory for the image, which will be both read and written to
// `restrict` is used to tell the compiler that the memory will only be accessed
// by this variable.
layout(r8, binding = 0) uniform image2D arena;

// select pixels in in a line segment
layout(set = 0, binding = 1, std430) restrict buffer PixelFilter
{
	float startPosX, startPosY, endPosX, endPosY;
	float halfThickness;
	int clipMode;
	// all colors with high enough alpha for now
} pixelFilter;

struct Pixel
{
	uint posX, posY;
	float r, g, b;
};

// this gets filled with coordinates
layout(set = 0, binding = 2, std430) restrict buffer PixelBuffer
{
	uint insertIdx;
    Pixel[] pixels;
} pixelBuffer;

float sdSegment( vec2 p, vec2 a, vec2 b )
{
    vec2 pa = p-a, ba = b-a;
    float h = clamp( dot(pa,ba) / dot(ba,ba), 0.0, 1.0 );
    return length( pa - ba*h );
}

void main()
{
	// Grab the current pixel's position from the ID of this specific invocation ("thread").
	ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
	vec4 pixel = imageLoad(arena, coords);
	vec2 posA = vec2(pixelFilter.startPosX, pixelFilter.startPosY);
	vec2 posB = vec2(pixelFilter.endPosX, pixelFilter.endPosY);
	if (pixel.a > 0.98 && sdSegment(coords, posA, posB) <= pixelFilter.halfThickness)
	{
		bool clipA = (pixelFilter.clipMode == 1 || pixelFilter.clipMode == 3) && length(posA - coords) <= pixelFilter.halfThickness;
		bool clipB = (pixelFilter.clipMode == 2 || pixelFilter.clipMode == 3) && length(posB - coords) <= pixelFilter.halfThickness;
		if (!clipA && !clipB)
		{
			uint idx = atomicAdd(pixelBuffer.insertIdx, 1);
			pixelBuffer.pixels[idx] = Pixel(uint(coords.x), uint(coords.y), float(pixel.r), float(pixel.g), float(pixel.b));
			// disable collision for selected pixels here for now
			imageStore(arena, coords, vec4(pixel.xyz, 0.8));
		}
	}
}
