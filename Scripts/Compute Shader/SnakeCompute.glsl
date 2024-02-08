#[compute]
#version 460

// Instruct the GPU to use 8x8x1 = 64 local invocations per workgroup.
layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

// Prepare memory for the image, which will be both read and written to
// `restrict` is used to tell the compiler that the memory will only be accessed
// by this variable.
layout(r8, binding = 0) uniform image2D arena;

struct LineData
{
    float prevPosX, prevPosY, newPosX, newPosY;
	float arcAngle;
	float segmentLength;
	float headingAngle;
    float halfThickness;
    float colorR, colorG, colorB, colorA;
	int clipMode;
};

// snake draw data inpt
layout(set = 0, binding = 1, std430) restrict readonly buffer SnakeBuffer
{
	uint snakeCount;
    LineData[] snakes;
} snakeBuffer;

// collision output
layout(set = 0, binding = 2, std430) restrict writeonly buffer CollisionBuffer
{
	int[] collisions;
} collisionBuffer;

// additional line draw data input
layout(set = 0, binding = 3, std430) restrict readonly buffer LineBuffer
{
	uint lineCount;
	LineData[] lines;
} lineBuffer;

const float PI = 3.14;
const float deg2Rad = PI / 180.0;
const float rad2Deg = 180.0 / PI;

mat2 rotateAroundOrigin(float angleRad)
{
	return mat2(cos(angleRad), -sin(angleRad), sin(angleRad), cos(angleRad));
}

// https://iquilezles.org/articles/distfunctions2d/
float sdSegment( vec2 p, vec2 a, vec2 b )
{
    vec2 pa = p-a, ba = b-a;
    float h = clamp( dot(pa,ba) / dot(ba,ba), 0.0, 1.0 );
    return length( pa - ba*h );
}

// https://www.shadertoy.com/view/wl23RK
// sc is the sin/cos of the aperture
float sdArc( in vec2 p, in vec2 sc, in float ra, float rb )
{
    p.x = abs(p.x);
    return ((sc.y*p.x>sc.x*p.y) ? length(p-sc*ra) : abs(length(p)-ra)) - rb;
}

// alternative: https://www.shadertoy.com/view/WldGWM
float sdJoint( in vec2 p, in float l, in float a, float w)
{
    // if perfectly straight
    if( abs(a)<0.001 )
    {
        float v = p.y;
        p.y -= clamp(p.y,0.0,l);
		return length(p);
    }
    
    // parameters
    vec2  sc = vec2(sin(a),cos(a));
    float ra = 0.5*l/a;
    
    // recenter
    p.x -= ra;
    
    // reflect
    vec2 q = p - 2.0*sc*max(0.0,dot(sc,p));

	// distance
    float u = abs(ra)-length(q);
    float d = (q.y<0.0) ? length( q+vec2(ra,0.0) ) : abs(u);

    return d-w;
}

// arcAngle > 0 for right turns, < 0 for left turns
float sdArcWrapper( in vec2 point, vec2 arcStart, float arcAngleRad, float arcRadius, float headingAngleRad, float thickness)
{
	float angleSign = -sign(arcAngleRad);
	arcAngleRad = abs(arcAngleRad);
    float halfArcAngleRad = arcAngleRad / 2.0;
    vec2 sc = vec2(sin(halfArcAngleRad),cos(halfArcAngleRad));
    
    point -= arcStart; // offset
    point *= rotateAroundOrigin(headingAngleRad); // rotate such that headingAngle is up
    point.x += angleSign * arcRadius; // offset such that position is on arc
    point *= rotateAroundOrigin(angleSign * (deg2Rad * 90.0 - halfArcAngleRad)); // rotate arc such that one end is at position
    
	return sdArc(point, sc, arcRadius, thickness);
}

float sdJointWrapper( in vec2 point, vec2 arcStart, float arcAngle, float segmentLength, float headingAngle, float width )
{
	point -= arcStart; // offset
	point *= rotateAroundOrigin(headingAngle);
	return sdJoint( point, segmentLength, arcAngle, width );
}

float distToLineSegment(vec2 pos, LineData line)
{
	if (line.arcAngle == 0)
	{
		return sdSegment(pos, vec2(line.prevPosX, line.prevPosY), vec2(line.newPosX, line.newPosY));
	}
	return sdJointWrapper(pos, vec2(line.prevPosX, line.prevPosY), line.arcAngle, line.segmentLength, line.headingAngle, 0.0);
}

void main()
{
	// Grab the current pixel's position from the ID of this specific invocation ("thread").
	ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
	ivec2 dimensions = imageSize(arena);
	// as margin to avoid accidental self collisions due to float imprecision errors
	float epsilon = 1.0 / dimensions.x / 4.0;
	vec4 pixel = imageLoad(arena, coords);

	// SNAKES
	for (int i = 0; i < snakeBuffer.snakeCount; i++)
	{
		LineData snake = snakeBuffer.snakes[i];
		vec2 prevPos = vec2(snake.prevPosX, snake.prevPosY);
		vec2 newPos = vec2(snake.newPosX, snake.newPosY);
		vec4 color = vec4(snake.colorR, snake.colorG, snake.colorB, snake.colorA);
		float distToSegment = distToLineSegment(coords, snake);

		if (distToSegment <= snake.halfThickness)
		{
			// if the pixel is not the end of same snake from previous frame && the pixel has a value already
			float distToStart = length(prevPos - coords);
            if (distToStart - epsilon > snake.halfThickness && pixel.a == 1)
            {
                // COLLISION
                collisionBuffer.collisions[i] = 1;
				imageStore(arena, coords, vec4(1,0,0,1));
            }
			else
			{
				// draw pixel
				imageStore(arena, coords, color);
			}
		}
	}

	// LINES
	for (int i = 0; i < lineBuffer.lineCount; i++)
	{
		LineData line = lineBuffer.lines[i];
		vec2 posA = vec2(line.prevPosX, line.prevPosY);
		vec2 posB = vec2(line.newPosX, line.newPosY);
		vec4 color = vec4(line.colorR, line.colorG, line.colorB, line.colorA);

		float distToSegment = distToLineSegment(coords, line);

		if (distToSegment <= line.halfThickness)
		{
			// clip ends
			// clip a
			if ((line.clipMode == 1 || line.clipMode == 3) && length(posA - coords) <= line.halfThickness)
			{
				continue;
			}
			if ((line.clipMode == 2 || line.clipMode == 3) && length(posB - coords) <= line.halfThickness)
			{
				continue;
			}

			// fill pixel
			imageStore(arena, coords, color);
		}
	}
}
