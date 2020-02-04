using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GL_EditorFramework.GL_Core;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GL_EditorFramework
{
    public sealed class Framework {
        public static float TWO_PI = (float)Math.PI*2;
        public static float PI = (float)Math.PI;
        public static float HALF_PI = (float)Math.PI/2;

        public static bool ShowShaderErrors = false;

        public delegate void ListEventHandler(object sender, ListEventArgs e);

        public class ListEventArgs : EventArgs
        {
            public IList List { get; set; }
            public ListEventArgs(IList list)
            {
                List = list;
            }
        }

        public static Matrix3 Mat3FromEulerAnglesDeg(Vector3 eulerAngles) =>
            Extensions.CreateRotationX(Math.PI * eulerAngles.X / 180.0) *
            Extensions.CreateRotationY(Math.PI * eulerAngles.Y / 180.0) *
            Extensions.CreateRotationZ(Math.PI * eulerAngles.Z / 180.0);

        public static void Initialize()
        {
            if (initialized)
                return;

            //texture sheet
            TextureSheet = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, TextureSheet);

            var bmp = Properties.Resources.TextureSheet;
            var bmpData = bmp.LockBits(
                new System.Drawing.Rectangle(0, 0, 128*4, 128*2),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, 128*4, 128*2, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            bmp.UnlockBits(bmpData);
            
            initialized = true;
        }
        private static bool initialized = false;
        public static int TextureSheet;
    }





    public static class Extensions
    {
        public static Vector3 ExtractDegreeEulerAngles(this Matrix3 mtx)
        {
            if (mtx.M13 - 1.0 < -1/256f)
            {
                if (mtx.M13 + 1.0 > 1/256f)
                {
                    return new Vector3(
                        (float)( 180 * Math.Atan2(mtx.M23, mtx.M33) / Math.PI),
                        (float)(-180 * Math.Asin(mtx.M13)           / Math.PI),
                        (float)( 180 * Math.Atan2(mtx.M12, mtx.M11) / Math.PI));
                }
                else
                {
                    return new Vector3(
                        (float)(180 * Math.Atan2(mtx.M21, mtx.M31) / Math.PI),
                        90,
                        0f);
                }
            }
            else
            {
                return new Vector3(
                        (float)(180 * Math.Atan2(mtx.M21, -mtx.M31) / Math.PI),
                        -90,
                        0f);
            }
        }

        public static Matrix3 CreateRotationX(double angle)
        {
            Matrix3 result;
            float cos = (float)(Math.Round(Math.Cos(angle)*256f)/256.0);
            float sin = (float)(Math.Round(Math.Sin(angle)*256f)/256.0);

            result.Row0 = Vector3.UnitX;
            result.Row1 = new Vector3(0.0f, cos, sin);
            result.Row2 = new Vector3(0.0f, -sin, cos);
            return result;
        }

        public static Matrix3 CreateRotationY(double angle)
        {
            Matrix3 result;
            float cos = (float)(Math.Round(Math.Cos(angle) * 256f) / 256.0);
            float sin = (float)(Math.Round(Math.Sin(angle) * 256f) / 256.0);

            result.Row0 = new Vector3(cos, 0.0f, -sin);
            result.Row1 = Vector3.UnitY;
            result.Row2 = new Vector3(sin, 0.0f, cos);
            return result;
        }

        public static Matrix3 CreateRotationZ(double angle)
        {
            Matrix3 result;
            float cos = (float)(Math.Round(Math.Cos(angle) * 256f) / 256.0);
            float sin = (float)(Math.Round(Math.Sin(angle) * 256f) / 256.0);

            result.Row0 = new Vector3(cos, sin, 0.0f);
            result.Row1 = new Vector3(-sin, cos, 0.0f);
            result.Row2 = Vector3.UnitZ;
            return result;
        }
    }
}
