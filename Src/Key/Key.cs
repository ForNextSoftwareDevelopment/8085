using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace _8085
{
    public partial class Key: UserControl
    {
        #region Members

        private bool pressed = false;

        private int radiusCenter = 26;
        private int radiusBorder = 36;
        private int widthBorder = 12;
        private Color colorBackground = Color.DarkGreen;
        private Color colorBorder = Color.Gainsboro;
        private Color colorCenter = Color.White;

        // Text on the button
        private string text = "";
        private string textBig = "";

        // Button pressed
        public bool Pressed { get { return pressed; } set { pressed = value; Invalidate(); } }

        // Radius of rounded center box corners
        public int RadiusCenter { get { return radiusCenter; } set { radiusCenter = value; Invalidate(); } }

        // Radius of rounded border corners
        public int RadiusBorder { get { return radiusBorder; } set { radiusBorder = value; Invalidate(); } }

        // Width of border
        public int WidthBorder { get { return widthBorder; } set { widthBorder = value; Invalidate(); } }

        // Background color
        public Color ColorBackground { get { return colorBackground; } set { colorBackground = value; Invalidate(); } }

        // Color of key edge
        public Color ColorBorder { get { return colorBorder; } set { colorBorder = value; Invalidate(); } }

        // Color of key
        public Color ColorCenter { get { return colorCenter; } set { colorCenter = value; Invalidate(); } }

        #endregion

        #region Constructor

        /// <summary>
        /// Base constructor
        /// </summary>
        public Key()
        {
            SuspendLayout();

            this.text = "";
            this.textBig = "Key";

            Name = "Key";

            ForeColor = Color.Black;
            Size = new Size(70, 70);
            Paint += new PaintEventHandler(Key_Paint);
            Resize += new EventHandler(Key_Resize);

            TabStop = false;
            DoubleBuffered = true;

            ResumeLayout();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="text"></param>
        /// <param name="textBig"></param>
        public Key(int width, int height, string text, string textBig = "")
        {
            SuspendLayout();

            this.text = text;
            this.textBig = textBig;

            if (textBig != "")
            {
                Name = "Key" + textBig;
            } else if (text != "")
            {
                Name = "Key" + text;
            } else
            {
                Name = "Unknown";
            }

            Size = new Size(width, height);
            Paint += new PaintEventHandler(Key_Paint);
            Resize += new EventHandler(Key_Resize);

            TabStop = false;
            DoubleBuffered = true;

            ResumeLayout();
        }

        #endregion

        #region EventHandlers

        /// <summary>
        /// Button clicked
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClick(EventArgs e)
        {
            Pressed = true;
            base.OnClick(e);
        }

        /// <summary>
        /// (Re-)Paint background
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // base.OnPaintBackground(e);
            e.Graphics.Clear(colorBackground);
        }

        /// <summary>
        /// Key resized
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Key_Resize(object sender, EventArgs e) { Invalidate(); }

        /// <summary>
        /// (Re-)Paint 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Key_Paint(object sender, PaintEventArgs e)
        {
            Brush brushCenter = new SolidBrush(colorCenter);
            if (pressed) brushCenter = new SolidBrush(Color.Red);
            Pen penBorder = new Pen(colorBorder, widthBorder);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Default;

            GraphicsPath path = new GraphicsPath();
            Rectangle corner = new Rectangle(0, 0, radiusCenter, radiusCenter);
            path.AddArc(corner, 180, 90);
            
            corner.X = Width - radiusCenter;
            path.AddArc(corner, 270, 90);
            
            corner.Y = Height - radiusCenter;
            path.AddArc(corner, 0, 90);
            
            corner.X = 0;
            path.AddArc(corner, 90, 90);
            
            path.CloseFigure();

            e.Graphics.FillPath(brushCenter, path);

            GraphicsPath pathBorder = new GraphicsPath();
            Rectangle cornerBorder = new Rectangle(0, 0, radiusBorder, radiusBorder);
            pathBorder.AddArc(cornerBorder, 180, 90);
            
            cornerBorder.X = Width - radiusBorder - 1;
            pathBorder.AddArc(cornerBorder, 270, 90);

            cornerBorder.Y = Height - radiusBorder - 1;
            pathBorder.AddArc(cornerBorder, 0, 90);

            cornerBorder.X = 0;
            pathBorder.AddArc(cornerBorder, 90, 90);

            pathBorder.CloseFigure();

            e.Graphics.DrawPath(penBorder, pathBorder);

            Font font = new Font("Tahoma", 12.0F, FontStyle.Regular);
            Font fontBig = new Font("Tahoma", 18.0F, FontStyle.Bold);

            string[] texts = text.Split(' ');
            if (texts.Length > 1)
            {
                // 2 text lines, ignore bigText
                e.Graphics.DrawString(texts[0], font, new SolidBrush(ForeColor), (Width - e.Graphics.MeasureString(texts[0], font).Width) / 2, Height / 2 - font.Height);
                e.Graphics.DrawString(texts[1], font, new SolidBrush(ForeColor), (Width - e.Graphics.MeasureString(texts[1], font).Width) / 2, Height / 2);
            } else 
            if ((textBig != "") && (text != ""))
            {
                // Big text on top of small text
                e.Graphics.DrawString(textBig, fontBig, new SolidBrush(ForeColor), (Width - e.Graphics.MeasureString(textBig, fontBig).Width) / 2, Height / 2 - fontBig.Height);
                e.Graphics.DrawString(text, font, new SolidBrush(ForeColor), (Width - e.Graphics.MeasureString(text, font).Width) / 2, Height / 2);
            } else
            if ((textBig != "") && (text == ""))
            {
                // Big text only
                e.Graphics.DrawString(textBig, fontBig, new SolidBrush(ForeColor), (Width - e.Graphics.MeasureString(textBig, fontBig).Width) / 2, Height / 2 - fontBig.Height);
            } else
            {
                // 1 line of text
                e.Graphics.DrawString(text, font, new SolidBrush(ForeColor), (Width - e.Graphics.MeasureString(text, font).Width) / 2, Height / 2 - font.Height / 2);
            }
        }

        #endregion
    }
}
