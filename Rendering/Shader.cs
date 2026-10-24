using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace ProjectOdyssey.Render
{
    public class Shader
    {
        private int handle;
        private readonly Dictionary<string, int> uniformLocations = new();

        public Shader(string vertexShaderPath, string fragmentShaderPath)
        {
            int vertexShader = CompileShader(ShaderType.VertexShader, vertexShaderPath);
            int fragmentShader = 0;

            try
            {
                fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentShaderPath);
                handle = LinkProgram(vertexShader, fragmentShader);
            }
            finally
            {
                // Runs on success and failure, so a bad fragment shader can't leak the vertex shader
                GL.DeleteShader(vertexShader);
                if (fragmentShader != 0) GL.DeleteShader(fragmentShader);
            }
        }

        private static int CompileShader(ShaderType shaderType, string shaderPath)
        {
            int shader = GL.CreateShader(shaderType);
            GL.ShaderSource(shader, File.ReadAllText(shaderPath));
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);

            if (success == 0)
            {
                string log = GL.GetShaderInfoLog(shader);
                GL.DeleteShader(shader);
                throw new InvalidOperationException($"{shaderType} failed to compile ({shaderPath}):\n{log}");
            }

            return shader;
        }

        private static int LinkProgram(int vertexShader, int fragmentShader)
        {
            int program = GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);

            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);

            if (success == 0)
            {
                string log = GL.GetProgramInfoLog(program);
                GL.DeleteProgram(program);
                throw new InvalidOperationException($"Shader program failed to link:\n{log}");
            }

            GL.DetachShader(program, vertexShader);
            GL.DetachShader(program, fragmentShader);

            return program;
        }

        private int GetLocation(string name)
        {
            if (!uniformLocations.TryGetValue(name, out int location))
            {
                location = GL.GetUniformLocation(handle, name);
                uniformLocations[name] = location;
            }

            return location;
        }

        public void SetInt(string name, int value)
        {
            GL.Uniform1(GetLocation(name), value);
        }

        public void SetMatrix4(string name, Matrix4 matrix)
        {
            GL.UniformMatrix4(GetLocation(name), false, ref matrix);
        }

        public void SetVector2(string name, float x, float y)
        {
            GL.Uniform2(GetLocation(name), x, y);
        }

        public void SetVector4(string name, Vector4 value)
        {
            GL.Uniform4(GetLocation(name), value);
        }

        public void Use()
        {
            GL.UseProgram(handle);
        }

        public void Dispose()
        {
            GL.DeleteProgram(handle);
        }
    }
}
