#[compute]
#version 460

// Instruct the GPU to use 8x8x1 = 64 local invocations per workgroup.
layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

// Prepare memory for the image, which will be both read and written to
// `restrict` is used to tell the compiler that the memory will only be accessed
// by this variable.
layout(r8, binding = 0) restrict uniform image2D arena;

// select pixels in circle at center with radius
layout(set = 0, binding = 1, std430) restrict buffer PixelFilter {
	ivec2 center;
	float radius;
	// all colors for now
} pixelFilter;

// this gets filled with coordinates
layout(set = 0, binding = 2, std430) restrict buffer PixelBuffer {
	uint insertIdx;
    ivec2[] pixels;
} pixelBuffer;

float sdSegment( vec2 p, vec2 a, vec2 b )
{
    vec2 pa = p-a, ba = b-a;
    float h = clamp( dot(pa,ba) / dot(ba,ba), 0.0, 1.0 );
    return length( pa - ba*h );
}

void main() {
	// Grab the current pixel's position from the ID of this specific invocation ("thread").
	ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
	vec4 pixel = imageLoad(arena, coords);
	if (pixel.w == 1 && distance(coords, pixelFilter.center) <= pixelFilter.radius)
	{
		uint idx = atomicAdd(pixelBuffer.insertIdx, 1);
		pixelBuffer.pixels[idx] = coords;
		imageStore(arena, coords, vec4(1,0,0,0.8));
	}
}
