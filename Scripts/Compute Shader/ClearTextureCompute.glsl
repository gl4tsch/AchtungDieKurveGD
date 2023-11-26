#[compute]
#version 460

// Instruct the GPU to use 8x8x1 = 64 local invocations per workgroup.
layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

// Prepare memory for the image, which will be both read and written to
// `restrict` is used to tell the compiler that the memory will only be accessed
// by this variable.
layout(r8, binding = 0) uniform image2D arena;

int borderWidth = 1;
vec4 borderColor = vec4(1,1,1,1);
vec4 backgroundColor = vec4(0,0,0,0);

void main()
{
	// Grab the current pixel's position from the ID of this specific invocation ("thread").
	ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    ivec2 dimensions = imageSize(arena);
    if (coords.x < borderWidth || coords.x > dimensions.x - 1 - borderWidth || coords.y < borderWidth || coords.y > dimensions.y - 1 - borderWidth)
    {
        imageStore(arena, coords, borderColor);
    }
    else
    {
        imageStore(arena, coords, backgroundColor);
    }
}