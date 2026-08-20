using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace ProjectOdyssey
{
    public class Shader
    {
        public int handle;

        public Shader(string vertexShaderPath, string fragmentShaderPath) 
        {
            int vertexShader = CompileShader(ShaderType.VertexShader, vertexShaderPath);
            int fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentShaderPath);

            handle = LinkProgram(vertexShader, fragmentShader);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        private static int CompileShader(ShaderType shaderType, string shaderPath)
        {
            int shader = GL.CreateShader(shaderType);
            GL.ShaderSource(shader, File.ReadAllText(shaderPath));
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);

            if (success == 0)
            {
                Console.WriteLine(GL.GetShaderInfoLog(shader));
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
                Console.WriteLine(GL.GetProgramInfoLog(program));
            }

            GL.DetachShader(program, vertexShader);
            GL.DetachShader(program, fragmentShader);

            return program;
        }

        public void SetInt(string name, int value)
        {
            int location = GL.GetUniformLocation(handle, name);
            GL.Uniform1(location, value);
        }

        public void SetMatrix4(string name, Matrix4 matrix)
        {
            int location = GL.GetUniformLocation(handle, name);
            GL.UniformMatrix4(location, false, ref matrix);
        }

        public void SetVector2(string name, float x, float y)
        {
            int location = GL.GetUniformLocation(handle, name);
            GL.Uniform2(location, x, y);
        }

        public void SetVector4(string name, Vector4 value)
        {
            int location = GL.GetUniformLocation(handle, name);
            GL.Uniform4(location, value);
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
