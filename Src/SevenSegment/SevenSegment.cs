using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _8085
{
    public partial class SevenSegment : UserControl
    {
        #region Members

        private Point[][] segmentsPoints;

        private int segmentsValue = 0;

        private int segmentsWidth = 8;

        private Color colorBackground = Color.Black;
        private Color colorDark = Color.FromArgb(0xFF, 0x60, 0x00, 0x00);
        private Color colorLight = Color.Red;

        // Background color
        public Color ColorBackground { get { return colorBackground; } set { colorBackground = value; Invalidate(); } }

        // Color of inactive LED segments.
        public Color ColorDark { get { return colorDark; } set { colorDark = value; Invalidate(); } }
        
        // Color of active LED segments.
        public Color ColorLight { get { return colorLight; } set { colorLight = value; Invalidate(); } }

        // Value of LED segments
        public int SegmentsValue { get { return segmentsValue; } set { segmentsValue = value; Invalidate(); } }

        // Value of LED segments width  
        public int SegementsWidth { get { return segmentsWidth; } set { segmentsWidth = value; Invalidate(); } }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // base.OnPaintBackground(e);
            e.Graphics.Clear(colorBackground);
        }

        #endregion

        #region Constructor

        public SevenSegment()
        {
            SuspendLayout();
            Name = "SevenSegment";
            Size = new Size(70, 96);
            Paint += new PaintEventHandler(SevenSegment_Paint);
            Resize += new EventHandler(SevenSegment_Resize);
            ResumeLayout(false);

            TabStop = false;
            Padding = new Padding(12, 6, 6, 6);
            DoubleBuffered = true;

            segmentsPoints = new Point[8][];
            for (int i = 0; i < 8; i++) segmentsPoints[i] = new Point[4];

            CalculatePoints();

            ResumeLayout();
        }

        #endregion

        #region EventHandlers
        private void SevenSegment_Resize(object sender, EventArgs e) { Invalidate(); }

        /// <summary>
        /// (Re) Paint 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SevenSegment_Paint(object sender, PaintEventArgs e)
        {
            Brush brushLight = new SolidBrush(colorLight);
            Brush brushDark = new SolidBrush(colorDark);

            Matrix trans = new Matrix();
            trans.Shear(-0.1F, 0.0F);
            e.Graphics.Transform = trans;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Default;

            // Draw elements based on whether the corresponding bit is high
            e.Graphics.FillPolygon((segmentsValue & 0x10) == 0x10 ? brushLight : brushDark, segmentsPoints[0]);
            e.Graphics.FillPolygon((segmentsValue & 0x20) == 0x20 ? brushLight : brushDark, segmentsPoints[1]);
            e.Graphics.FillPolygon((segmentsValue & 0x40) == 0x40 ? brushLight : brushDark, segmentsPoints[2]);
            e.Graphics.FillPolygon((segmentsValue & 0x80) == 0x80 ? brushLight : brushDark, segmentsPoints[3]);
            e.Graphics.FillPolygon((segmentsValue & 0x01) == 0x01 ? brushLight : brushDark, segmentsPoints[4]);
            e.Graphics.FillPolygon((segmentsValue & 0x02) == 0x02 ? brushLight : brushDark, segmentsPoints[5]);
            e.Graphics.FillPolygon((segmentsValue & 0x04) == 0x04 ? brushLight : brushDark, segmentsPoints[6]);
            e.Graphics.FillPolygon((segmentsValue & 0x08) == 0x08 ? brushLight : brushDark, segmentsPoints[7]);
        }

        #endregion

        #region Methods

        private void CalculatePoints()
        {
            // All 3 horizontal segments    
            int startHorizontalX = Padding.Left + SegementsWidth + 2;
            int endHorizontalX = Width - Padding.Right - SegementsWidth - 2;

            // Top horizontal segment
            int startHorizontalTopRowY = Padding.Top;
            int endHorizontalTopRowY = Padding.Top + SegementsWidth;

            // Middle horizontal segment
            int startHorizontalMiddleRowY = Height / 2 - segmentsWidth / 2;
            int endHorizontalMiddleRowY = Height / 2 + segmentsWidth / 2;

            // Bottom horizontal segment
            int startHorizontalBottomRowY = Height - Padding.Bottom - segmentsWidth;
            int endHorizontalBottomRowY = Height - Padding.Bottom;

            // Top vertical segments 
            int startVerticalTopY = Padding.Top;
            int endVerticalTopY = Height / 2 - 2;

            // Bottom vertical segments 
            int startVerticalBottomY = Height / 2 + 2;
            int endVerticalBottomY = Height - Padding.Bottom;

            // First Column vertical segments
            int startVerticalLeftX = Padding.Left;
            int endVerticalLeftX = Padding.Left + segmentsWidth;

            // Last Column vertical segments
            int startVerticalRightX = Width - Padding.Right - segmentsWidth;
            int endVerticalRightX = Width - Padding.Right;

            segmentsPoints[0][0].X = startHorizontalX; segmentsPoints[0][0].Y = startHorizontalTopRowY;
            segmentsPoints[0][1].X = endHorizontalX;   segmentsPoints[0][1].Y = startHorizontalTopRowY;
            segmentsPoints[0][2].X = endHorizontalX;   segmentsPoints[0][2].Y = endHorizontalTopRowY;
            segmentsPoints[0][3].X = startHorizontalX; segmentsPoints[0][3].Y = endHorizontalTopRowY;

            segmentsPoints[1][0].X = startVerticalRightX; segmentsPoints[1][0].Y = startVerticalTopY;
            segmentsPoints[1][1].X = endVerticalRightX;   segmentsPoints[1][1].Y = startVerticalTopY; 
            segmentsPoints[1][2].X = endVerticalRightX;   segmentsPoints[1][2].Y = endVerticalTopY;
            segmentsPoints[1][3].X = startVerticalRightX; segmentsPoints[1][3].Y = endVerticalTopY;

            segmentsPoints[2][0].X = startVerticalRightX; segmentsPoints[2][0].Y = startVerticalBottomY;
            segmentsPoints[2][1].X = endVerticalRightX;   segmentsPoints[2][1].Y = startVerticalBottomY;
            segmentsPoints[2][2].X = endVerticalRightX;   segmentsPoints[2][2].Y = endVerticalBottomY;
            segmentsPoints[2][3].X = startVerticalRightX; segmentsPoints[2][3].Y = endVerticalBottomY;

            segmentsPoints[3][0].X = startHorizontalX; segmentsPoints[3][0].Y = startHorizontalBottomRowY;
            segmentsPoints[3][1].X = endHorizontalX;   segmentsPoints[3][1].Y = startHorizontalBottomRowY;
            segmentsPoints[3][2].X = endHorizontalX;   segmentsPoints[3][2].Y = endHorizontalBottomRowY;
            segmentsPoints[3][3].X = startHorizontalX; segmentsPoints[3][3].Y = endHorizontalBottomRowY;

            segmentsPoints[4][0].X = startVerticalLeftX; segmentsPoints[4][0].Y = startVerticalBottomY;
            segmentsPoints[4][1].X = endVerticalLeftX;   segmentsPoints[4][1].Y = startVerticalBottomY;
            segmentsPoints[4][2].X = endVerticalLeftX;   segmentsPoints[4][2].Y = endVerticalBottomY;
            segmentsPoints[4][3].X = startVerticalLeftX; segmentsPoints[4][3].Y = endVerticalBottomY;

            segmentsPoints[5][0].X = startVerticalLeftX; segmentsPoints[5][0].Y = startVerticalTopY;
            segmentsPoints[5][1].X = endVerticalLeftX;   segmentsPoints[5][1].Y = startVerticalTopY;
            segmentsPoints[5][2].X = endVerticalLeftX;   segmentsPoints[5][2].Y = endVerticalTopY;
            segmentsPoints[5][3].X = startVerticalLeftX; segmentsPoints[5][3].Y = endVerticalTopY;

            segmentsPoints[6][0].X = startHorizontalX; segmentsPoints[6][0].Y = startHorizontalMiddleRowY;
            segmentsPoints[6][1].X = endHorizontalX;   segmentsPoints[6][1].Y = startHorizontalMiddleRowY;
            segmentsPoints[6][2].X = endHorizontalX;   segmentsPoints[6][2].Y = endHorizontalMiddleRowY;
            segmentsPoints[6][3].X = startHorizontalX; segmentsPoints[6][3].Y = endHorizontalMiddleRowY;

            segmentsPoints[7][0].X = Width - 2;                 segmentsPoints[7][0].Y = startHorizontalBottomRowY;
            segmentsPoints[7][1].X = Width - 2 + segmentsWidth; segmentsPoints[7][1].Y = startHorizontalBottomRowY;
            segmentsPoints[7][2].X = Width - 2 + segmentsWidth; segmentsPoints[7][2].Y = endHorizontalBottomRowY;
            segmentsPoints[7][3].X = Width - 2;                 segmentsPoints[7][3].Y = endHorizontalBottomRowY;
        }

        #endregion
    }
}
