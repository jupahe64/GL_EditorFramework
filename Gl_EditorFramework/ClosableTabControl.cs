using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GL_EditorFramework.Properties;
using System.Runtime.InteropServices;

namespace GL_EditorFramework
{
    public partial class ClosableTabControl : TabControl
    {
        //most code from: https://social.technet.microsoft.com/wiki/contents/articles/50957.c-winform-tabcontrol-with-add-and-close-button.aspx

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
        private const int TCM_SETMINTABWIDTH = 0x1300 + 49;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SendMessage(Handle, TCM_SETMINTABWIDTH, IntPtr.Zero, (IntPtr)16);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        public ClosableTabControl()
        {
            DrawMode = TabDrawMode.OwnerDrawFixed;
            Padding = new Point(12, 4);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            var tabPage = TabPages[e.Index];
            var tabRect = GetTabRect(e.Index);
            tabRect.Inflate(-2, -2);

            var closeImage = Resources.CloseTabIcon;

            var imageRect = new Rectangle(
                    (tabRect.Right - closeImage.Width),
                    tabRect.Top + (tabRect.Height - closeImage.Height) / 2,
                    closeImage.Width,
                    closeImage.Height);

            var topLeft = PointToScreen(Location);

            if (hovered && imageRect.Contains(MousePosition.X - topLeft.X, MousePosition.Y - topLeft.Y))
                closeImage = Resources.CloseTabIconHover;

            

            e.Graphics.DrawImage(closeImage,
                (tabRect.Right - closeImage.Width),
                tabRect.Top + (tabRect.Height - closeImage.Height) / 2);
            TextRenderer.DrawText(e.Graphics, tabPage.Text, tabPage.Font,
                tabRect, tabPage.ForeColor, TextFormatFlags.Left);
            

            base.OnDrawItem(e);
        }

        bool hovered = false;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Refresh();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            hovered = true;
            Refresh();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hovered = false;
            Refresh();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            for (var i = 0; i < TabPages.Count; i++)
            {
                var tabRect = GetTabRect(i);
                tabRect.Inflate(-2, -2);
                var closeImage = new Bitmap(Resources.CloseTabIcon);
                var imageRect = new Rectangle(
                    (tabRect.Right - closeImage.Width),
                    tabRect.Top + (tabRect.Height - closeImage.Height) / 2,
                    closeImage.Width,
                    closeImage.Height);
                if (imageRect.Contains(e.Location))
                {
                    TabPages.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
