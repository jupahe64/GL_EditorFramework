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
            Matrix3.CreateRotationX(PI * eulerAngles.X / 180f) *
            Matrix3.CreateRotationY(PI * eulerAngles.Y / 180f) *
            Matrix3.CreateRotationZ(PI * eulerAngles.Z / 180f);

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
            if (mtx.M13 - 1.0 < -0.0001)
            {
                if (mtx.M13 + 1.0 > 0.0001)
                {
                    return new Vector3(
                         180f * (float)Math.Atan2(mtx.M23, mtx.M33) / MathHelper.Pi,
                        -180f * (float)Math.Asin(mtx.M13)           / MathHelper.Pi,
                         180f * (float)Math.Atan2(mtx.M12, mtx.M11) / MathHelper.Pi);
                }
                else
                {
                    return new Vector3(
                        180f * (float)Math.Atan2(mtx.M21, mtx.M31),
                        90,
                        0f);
                }
            }
            else
            {
                return new Vector3(
                        180f * (float)Math.Atan2(mtx.M21, mtx.M31),
                        90,
                        0f);
            }
        }
    }
}
