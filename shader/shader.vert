#version 410 core

layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aTexCoord;

uniform mat4 projection;
uniform vec2 uPosition;
uniform vec2 uSize;

out vec2 vTexCoord;

void main()
{
    vec2 scaled = aPosition * uSize;
    vec2 worldPos = scaled + uPosition;
    gl_Position = projection * vec4(worldPos, 0.0, 1.0);
    vTexCoord = aTexCoord;
}