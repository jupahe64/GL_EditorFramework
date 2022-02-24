using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GL_EditorFramework
{
    public class SuggestingTextBox : TextBox
    {
        protected SuggestionDropDown suggestionsDropDown = new SuggestionDropDown();
        readonly Control focusControl = new Label() { Size = new Size() };

        public event CancelEventHandler ValueEntered;

        public bool SuggestClear { get; set; } = false;

        public string[] PossibleSuggestions { get; set; } = Array.Empty<string>();

        public bool FilterSuggestions { get; set; } = true;

        public SuggestingTextBox()
        {
            suggestionsDropDown.ItemSelected += SuggestionsDropDown_ItemSelected;
        }

        private void SuggestionsDropDown_ItemSelected(object sender, EventArgs e)
        {
            Text = suggestionsDropDown.SelectedSuggestion;

            var args = new CancelEventArgs();
            ValueEntered?.Invoke(this, args);
            if (args.Cancel)
                ForeColor = Color.Red; //mark the value red to indicate it's invalid
            else
                ForeColor = SystemColors.ControlText;

            ignoreFocusChange = true;
            focusControl.Focus(); //because Microsoft forgot to put in Unfocus() smh
            suggestionsDropDown.Hide(); //because OnLostFocus won't get called
            ignoreFocusChange = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            ignoreFocusChange = true;

            if (e.KeyCode == Keys.Return && Focused)
            {
                var args = new CancelEventArgs();
                ValueEntered?.Invoke(this, args);
                if (!args.Cancel)
                {
                    ForeColor = SystemColors.ControlText;
                    focusControl.Focus(); //because Microsoft forgot to put in Unfocus() smh
                    suggestionsDropDown.Hide(); //because OnLostFocus won't get called
                    e.SuppressKeyPress = true;
                }
                else
                    ForeColor = Color.Red;
            }
            else
                base.OnKeyDown(e);

            ignoreFocusChange = false;
        }

        private bool ignoreFocusChange = false;

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);

            if (ignoreFocusChange)
                return;

            ignoreFocusChange = true;

            suggestionsDropDown.Font = Font;
            suggestionsDropDown.Show(PointToScreen(new Point(-2, Height-2)), Width, Text, PossibleSuggestions, SuggestClear, FilterSuggestions);
            ignoreFocusChange = false;
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);

            if (ignoreFocusChange)
                return;

            Form parentForm = this.FindForm();

            if (parentForm != null && parentForm.ContainsFocus) //another Control inside the parent form got focused, we can be very sure that this was intentional
            {
                var args = new CancelEventArgs();
                ValueEntered?.Invoke(this, args);
                if (args.Cancel)
                    ForeColor = Color.Red; //mark the value red to indicate it's invalid
                else
                    ForeColor = SystemColors.ControlText;
            }

            suggestionsDropDown.Hide();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);

            suggestionsDropDown.UpdateSuggestions(Text);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            Form parentForm = this.FindForm();

            if (parentForm != null && !parentForm.Controls.Contains(focusControl))
            {
                parentForm.Controls.Add(focusControl);

                parentForm.Move += MyLocationChanged;

                parentForm.Resize += MyLocationChanged;

                parentForm.FormClosed += ParentForm_FormClosed;
            }

            SuspendLayout();
            suggestionsDropDown.Width = Width;
        }

        private void ParentForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            suggestionsDropDown.Close();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);

            suggestionsDropDown.Font = Font;
        }

        private void MyLocationChanged(object sender, EventArgs e)
        {
            suggestionsDropDown.Location = PointToScreen(new Point(-2, Height - 2));
        }
    }

    public class SuggestionDropDown : Form
    {
        public SuggestionDropDown()
        {
            SetStyle(
        ControlStyles.AllPaintingInWmPaint |
        ControlStyles.UserPaint |
        ControlStyles.OptimizedDoubleBuffer,
        true);

            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            BackColor = SystemColors.ControlLightLight;
            displayControl.AutoScrollMinSize = new Size(0, 0);

            typeof(ScrollableControl).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, displayControl, new object[] { true });

            displayControl.MouseMove += DisplayControl_MouseMove;
            displayControl.Scroll += DisplayControl_Scroll;
            displayControl.MouseDown += DisplayControl_MouseDown;
            displayControl.MouseUp += DisplayControl_MouseUp;
            displayControl.Paint += DisplayControl_Paint;
            
            Padding = new Padding(1);
            displayControl.Dock = DockStyle.Fill;
            Controls.Add(displayControl);

            animTimer.Tick += AnimTimer_Tick;

            Height = 0;
        }

        float animProgress = 0f;

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            if (animProgress == 1.0)
            {
                animTimer.Stop();
                animProgress = 0f;
            }
            else
            {
                animProgress += 0.125f;

                int newHeight = (int)(finalHeight * animProgress);

                mouseY -= newHeight - Height;

                Height = newHeight;

                Refresh();
            }
        }

        Timer animTimer = new Timer() { Interval = 1 };

        int contentWidth = 100;
        int minWidth = 100;
        public new int Width
        {
            get => minWidth;
            set
            {
                minWidth = value;

                if (value > contentWidth)
                    base.Width = minWidth;

                Refresh();
            }
        }

        private void DisplayControl_MouseMove(object sender, MouseEventArgs e)
        {
            mouseY = e.Y;
            Refresh();
        }

        private void DisplayControl_Scroll(object sender, ScrollEventArgs se)
        {
            mouseY -= se.NewValue - se.OldValue; //makes no sense but it makes sure that hoveredIndex doesn't change when scrolling
        }

        private void DisplayControl_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDown = true;
        }

        private void DisplayControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                if (hoveredIndex == -1)
                {
                    SelectedSuggestion = string.Empty;
                }
                else if (hoveredIndex == -2)
                {
                    UpdateSuggestions(string.Empty);
                    return;
                }
                else
                {
                    SelectedSuggestion = suggestions[hoveredIndex];
                }

                mouseDown = false;

                Hide();
                ItemSelected?.Invoke(this, null);
            }
        }

        private void DisplayControl_Paint(object sender, PaintEventArgs e)
        {
            int rowHeight = (int)Math.Ceiling(Font.GetHeight(DeviceDpi)) + 4;

            int y = displayControl.AutoScrollPosition.Y;

            if (animTimer.Enabled)
                y -= (finalHeight - Height);

            if (suggestClear)
            {
                if (mouseY >= y && mouseY < y + rowHeight)
                {
                    e.Graphics.FillRectangle(SystemBrushes.Highlight, new Rectangle(0, y, Bounds.Width, rowHeight));
                    e.Graphics.DrawString(CLEAR_STRING, Font, SystemBrushes.HighlightText, new Point(0, y + 2));
                    hoveredIndex = -1;
                }
                else
                    e.Graphics.DrawString(CLEAR_STRING, Font, SystemBrushes.ControlText, new Point(0, y+2));

                y += rowHeight;
            }

            for (int i = 0; i < suggestions.Length; i++)
            {
                if (mouseY >= y && mouseY < y + rowHeight)
                {
                    e.Graphics.FillRectangle(SystemBrushes.Highlight, new Rectangle(0, y, Bounds.Width, rowHeight));
                    e.Graphics.DrawString(suggestions[i], Font, SystemBrushes.HighlightText, new Point(0, y+2));
                    hoveredIndex = i;
                }
                else
                    e.Graphics.DrawString(suggestions[i], Font, SystemBrushes.ControlText, new Point(0, y+2));

                y += rowHeight;
            }

            if (displayShowAll)
            {
                if (mouseY >= y)
                {
                    e.Graphics.FillRectangle(SystemBrushes.Highlight, new Rectangle(0, y, Bounds.Width, rowHeight));
                    e.Graphics.DrawString(SHOW_ALL_STRING, Font, SystemBrushes.HighlightText, new Point(0, y + 2));
                    hoveredIndex = -2;
                }
                else
                    e.Graphics.DrawString(SHOW_ALL_STRING, Font, SystemBrushes.ControlText, new Point(0, y + 2));
            }
        }

        ScrollableControl displayControl = new ScrollableControl();

        private string[] possibleSuggestions = Array.Empty<string>();

        private string[] suggestions = Array.Empty<string>();

        private bool suggestClear = false;

        private bool filterSuggestions = false;

        private bool displayShowAll = false;

        public void UpdateSuggestions(string filterString, bool allowKeepCurrentSuggestions = true)
        {
            if (filterSuggestions)
            {
                List<string> suggestionList = new List<string>();

                for (int i = 0; i < possibleSuggestions.Length; i++)
                {
                    if (possibleSuggestions[i].StartsWith(filterString, StringComparison.OrdinalIgnoreCase))
                        suggestionList.Add(possibleSuggestions[i]);
                }
                if (suggestionList.Count > 0)
                    suggestions = suggestionList.ToArray();
                else if (!allowKeepCurrentSuggestions)
                    suggestions = possibleSuggestions;
            }
            else
            {
                suggestions = possibleSuggestions;
            }

            int maxHeight = 10 * (Font.Height+4);

            displayShowAll = possibleSuggestions.Length > suggestions.Length;

            int desiredHeight = (suggestions.Length + (suggestClear ? 1 : 0) + (displayShowAll ? 1 : 0))
                * (Font.Height+4);

            int newHeight;

            if (desiredHeight > maxHeight)
            {
                displayControl.AutoScrollMinSize = new Size(0, desiredHeight);
                newHeight = maxHeight;
            }
            else
            {
                displayControl.AutoScrollMinSize = new Size(0, 0);
                newHeight = desiredHeight;
            }

            float contentWidth_f = 100;

            float paddingRight = AutoScrollMinSize.Height == 0 ? 0 : SystemInformation.VerticalScrollBarWidth;

            using (var g = CreateGraphics())
            {
                for (int i = 0; i < suggestions.Length; i++)
                {
                    contentWidth_f = Math.Max(g.MeasureString(suggestions[i], Font).Width + paddingRight, contentWidth_f);
                }
            }

            contentWidth = (int)contentWidth_f;

            SetBounds(0, 0, Math.Max(contentWidth, minWidth), newHeight, BoundsSpecified.Size);

            Refresh();
        }

        int finalHeight = 0;

        #region make absolutely sure that this form can't be focused on
        private const int SW_SHOWNOACTIVATE = 4;
        private const int HWND_TOPMOST = -1;
        private const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        static extern bool SetWindowPos(
             int hWnd,             // Window handle
             int hWndInsertAfter,  // Placement-order handle
             int X,                // Horizontal position
             int Y,                // Vertical position
             int cx,               // Width
             int cy,               // Height
             uint uFlags);         // Window positioning flags

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        static void ShowInactiveTopmost(Form frm)
        {
            ShowWindow(frm.Handle, SW_SHOWNOACTIVATE);
            SetWindowPos(frm.Handle.ToInt32(), HWND_TOPMOST,
            frm.Left, frm.Top, frm.Width, frm.Height,
            SWP_NOACTIVATE);
        }
        #endregion

        public void Show(Point location, int width, string currentText, string[] possibleSuggestions, bool suggestClear, bool filterSuggestions)
        {
            mouseY = -1;
            hoveredIndex = -2;

            this.filterSuggestions = filterSuggestions;
            this.possibleSuggestions = possibleSuggestions;
            this.suggestClear = suggestClear;
            Location = location;

            minWidth = width;

            UpdateSuggestions(currentText, false);

            finalHeight = Height;

            Height = 0;

            ShowInactiveTopmost(this);


            animTimer.Start();
        }

        int mouseY = -1;
        int hoveredIndex = -2;

        public string SelectedSuggestion { get; private set; }

        public event EventHandler ItemSelected;

        bool mouseDown = false;
        public static string CLEAR_STRING = "<Clear>";
        public static string SHOW_ALL_STRING = "Show All";

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.DrawRectangle(SystemPens.Highlight, new Rectangle(0, 0, Bounds.Width - 1, Height - 1));
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams baseParams = base.CreateParams;

                const int WS_EX_NOACTIVATE = 0x08000000;
                const int WS_EX_TOOLWINDOW = 0x00000080;
                const int WS_EX_COMPOSITED = 0x02000000;

                const int CS_DROPSHADOW = 0x20000;
                
                baseParams.ExStyle |= (int)(WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);

                if (Environment.OSVersion.Version.Major >= 6)
                    baseParams.ExStyle |= WS_EX_COMPOSITED;

                baseParams.ClassStyle |= CS_DROPSHADOW;

                return baseParams;
            }
        }

        private const int WM_MOUSEACTIVATE = 0x0021, MA_NOACTIVATE = 0x0003;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }
            base.WndProc(ref m);
        }
    }
}
