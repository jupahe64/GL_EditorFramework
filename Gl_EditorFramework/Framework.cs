﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GL_EditorFramework.GL_Core;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GL_EditorFramework
{
    public sealed class Framework 
    {
        public static Color MixedColor(Color color1, Color color2)
        {
            byte a1 = color1.A;
            byte r1 = color1.R;
            byte g1 = color1.G;
            byte b1 = color1.B;

            byte a2 = color2.A;
            byte r2 = color2.R;
            byte g2 = color2.G;
            byte b2 = color2.B;

            int a3 = (a1 + a2) / 2;
            int r3 = (r1 + r2) / 2;
            int g3 = (g1 + g2) / 2;
            int b3 = (b1 + b2) / 2;

            return Color.FromArgb(a3, r3, g3, b3);
        }

        public static readonly Brush backBrush = new SolidBrush(MixedColor(SystemColors.ControlDark, SystemColors.Control));

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
            Extensions.CreateRotationZ(Math.PI * eulerAngles.Z / 180.0) *
                Matrix3.Identity;


        //from AbstractTransformAction.cs slightly modified
        public static Vector3 ApplyRotation(Vector3 rot, Matrix3 deltaRotation)
        {
            Matrix3 rotMtx = Mat3FromEulerAnglesDeg(rot);

            Matrix3 newRot = rotMtx * deltaRotation;
            if (newRot == rotMtx)
            {
                return rot;
            }
            else
            {
                return newRot.ExtractDegreeEulerAngles() + new Vector3(
                    (float)Math.Round(rot.X / 360f) * 360,
                    (float)Math.Round(rot.Y / 360f) * 360,
                    (float)Math.Round(rot.Z / 360f) * 360
                    );
            }
        }

        public static Keys KeyStroke(string keyStrokeString)
        {
            bool hasValidKeyCode = false;
            Keys result = Keys.None;
            foreach (string val in keyStrokeString
                .Replace(" ", "")
                .Split('+'))
            {
                string keyName = val;

                if (keyName == "Ctrl")
                    keyName = "Control";

                else if (keyName == "Del")
                    keyName = "Delete";


                if (Enum.TryParse(keyName, out Keys key))
                {
                    Keys keyCode = key & Keys.KeyCode;
                    if (keyCode == Keys.None)
                        result |= key;
                    else
                    {
                        if (hasValidKeyCode)
                            throw new Exception("A keystroke can't have more than one none modifier key");
                        else
                        {
                            result |= key;
                            hasValidKeyCode = true;
                        }
                    }
                }
            }

            if (!hasValidKeyCode)
                throw new Exception("A keystroke must have one none modifier key");

            return result;
        }

        public static void Initialize()
        {
            if (initialized)
                return;

            //string GetCondition(int row, Vector3 vec)
            //{
            //    if (vec.X != 0)
            //        return $"mtx.M{row}1=={vec.X}";
            //    else if (vec.Y != 0)
            //        return $"mtx.M{row}2=={vec.Y}";
            //    else
            //        return $"mtx.M{row}3=={vec.Z}";
            //}

            //string edgeCases = "";

            //for (float x = -90; x <= 180; x+=90)
            //{
            //    for (float y = -90; y <= 180; y += 90)
            //    {
            //        for (float z = -90; z <= 180; z += 90)
            //        {
            //            var mat = Mat3FromEulerAnglesDeg(new Vector3(x, y, z));

            //            var mat2 = Mat3FromEulerAnglesDeg(mat.ExtractDegreeEulerAngles());

            //            if (mat != mat2)
            //            {
            //                edgeCases += $"if({GetCondition(1,mat.Row0)} && {GetCondition(2, mat.Row1)} && {GetCondition(3, mat.Row2)})\n";
            //                edgeCases += $"    return new Vector3({x},{y},{z});\n";
            //            }
            //        }
            //    }
            //}

            //solid shader
            var solidColorFrag = new FragmentShader(
                @"#version 330
                        uniform vec4 color;
                        void main(){
                            gl_FragColor = color;
                        }");
            var solidColorVert = new VertexShader(
                @"#version 330
                        layout(location = 0) in vec4 position;
                        uniform mat4 mtxMdl;
                        uniform mat4 mtxCam;
                        void main(){
                            gl_Position = mtxCam*mtxMdl*position;
                        }");

            SolidColorShaderProgram = new ShaderProgram(solidColorFrag, solidColorVert);

            //texture sheet
            TextureSheet = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, TextureSheet);

            var bmp = Properties.Resources.TextureSheet;
            var bmpData = bmp.LockBits(
                new Rectangle(0, 0, 128*4, 128*2),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, 128*4, 128*2, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            bmp.UnlockBits(bmpData);
            
            initialized = true;
        }

        internal static List<VertexArrayObject> vaos = new List<VertexArrayObject>();

        internal static List<ShaderProgram> shaderPrograms = new List<ShaderProgram>();

        internal static List<GL_ControlModern> modernGlControls = new List<GL_ControlModern>();

        private static bool initialized = false;

        public static int TextureSheet { get; private set; }
        public static ShaderProgram SolidColorShaderProgram { get; private set; }
    }


    public static class Extensions
    {
        public static Vector3 ExtractDegreeEulerAngles(this Matrix3 mtx)
        {

            Vector3 aimVec = mtx.Row0.Normalized();
            Vector3 aimUpVector = mtx.Row2.Normalized();

            double x, y, z;

            if (aimVec == Vector3.UnitZ || aimVec == -Vector3.UnitZ)
            {
                z = Math.Atan2(aimUpVector.X, aimUpVector.Y);
                y = aimVec.Z * Math.PI / 2.0;
                x = 0.0;
            }
            else
            {
                z = Math.Atan2(aimVec.Y, aimVec.X);
                y = -Math.Asin(aimVec.Z);

                Vector3 axisA = Vector3.Cross(aimVec, Vector3.UnitZ);
                Vector3 axisB = Vector3.Cross(axisA, aimVec);

                

                x = Math.Atan2(Vector3.Dot(axisA, aimUpVector), Vector3.Dot(axisB, aimUpVector));
            }

            return new Vector3(
                (float)(Math.Round(180 * x / Math.PI * 1e5) / 1e5),
                (float)(Math.Round(180 * y / Math.PI * 1e5) / 1e5),
                (float)(Math.Round(180 * z / Math.PI * 1e5) / 1e5)
                );
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

        public static float GetRotationX(Matrix3 mtx)
        {
            return (float)(180 * Math.Atan2(mtx.M23, mtx.M33) / Math.PI);
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

        public static float GetRotationY(Matrix3 mtx)
        {
            return (float)(180 * Math.Atan2(mtx.M31, mtx.M11) / Math.PI);
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

        public static float GetRotationZ(Matrix3 mtx)
        {
            return (float)(180 * Math.Atan2(mtx.M12, mtx.M22) / Math.PI);
        }

        public static void Deconstruct<K, V>(this KeyValuePair<K, V> kvp, out K key, out V value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }
    }
}
