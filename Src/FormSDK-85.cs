using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _8085
{
    public partial class FormSDK_85 : Form
    {
        #region Members

        // Led display 
        public SevenSegment sevenSegmentAddress0;
        public SevenSegment sevenSegmentAddress1;
        public SevenSegment sevenSegmentAddress2;
        public SevenSegment sevenSegmentAddress3;
        public SevenSegment sevenSegmentAddress4;
        public SevenSegment sevenSegmentAddress5;
        public SevenSegment sevenSegmentData0;
        public SevenSegment sevenSegmentData1;

        // Keyboard
        public Key keyReset;
        public Key keyVectIntr;
        public Key keyC;
        public Key keyD;
        public Key keyE;
        public Key keyF;
        public Key keySingleStep;
        public Key keyGo;
        public Key key8;
        public Key key9;
        public Key keyA;
        public Key keyB;
        public Key keySubstMem;
        public Key keyExamReg;
        public Key key4;
        public Key key5;
        public Key key6;
        public Key key7;
        public Key keyNext;
        public Key keyExec;
        public Key key0;
        public Key key1;
        public Key key2;
        public Key key3;

        // Initial location of window
        int x, y;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public FormSDK_85(int x, int y)
        {
            InitializeComponent();

            this.x = x;
            this.y = y;

            // Most left digit
            sevenSegmentAddress3 = new SevenSegment();
            sevenSegmentAddress3.Location = new Point(14, 4);
            Controls.Add(sevenSegmentAddress3);

            sevenSegmentAddress2 = new SevenSegment();
            sevenSegmentAddress2.Location = new Point(sevenSegmentAddress3.Width + 18, 4);
            Controls.Add(sevenSegmentAddress2);

            sevenSegmentAddress1 = new SevenSegment();
            sevenSegmentAddress1.Location = new Point(sevenSegmentAddress3.Width + sevenSegmentAddress2.Width + 22, 4);
            Controls.Add(sevenSegmentAddress1);

            sevenSegmentAddress0 = new SevenSegment();
            sevenSegmentAddress0.Location = new Point(sevenSegmentAddress3.Width + sevenSegmentAddress2.Width + sevenSegmentAddress1.Width + 26, 4);
            Controls.Add(sevenSegmentAddress0);

            sevenSegmentData1 = new SevenSegment();
            sevenSegmentData1.Location = new Point(sevenSegmentAddress3.Width + sevenSegmentAddress2.Width + sevenSegmentAddress1.Width + sevenSegmentAddress0.Width + 34, 4);
            Controls.Add(sevenSegmentData1);

            // Most right digit
            sevenSegmentData0 = new SevenSegment();
            sevenSegmentData0.Location = new Point(sevenSegmentAddress3.Width + sevenSegmentAddress2.Width + sevenSegmentAddress1.Width + sevenSegmentAddress0.Width + sevenSegmentData1.Width + 38, 4);
            Controls.Add(sevenSegmentData0);

            // Button positions
            int startX = 14;
            int startY = 4 + sevenSegmentAddress3.Height + 20;

            keyReset = new Key(70, 70, "RESET");
            keyReset.Location = new Point(startX, startY);
            Controls.Add(keyReset);

            startX += keyReset.Width + 4;

            keyVectIntr = new Key(70, 70, "VECT INTR");
            keyVectIntr.Location = new Point(startX, startY);
            Controls.Add(keyVectIntr);

            startX += keyVectIntr.Width + 4;

            keyC = new Key(70, 70, "", "C");
            keyC.Location = new Point(startX, startY);
            Controls.Add(keyC);

            startX += keyC.Width + 4;

            keyD = new Key(70, 70, "", "D");
            keyD.Location = new Point(startX, startY);
            Controls.Add(keyD);

            startX += keyD.Width + 8;

            keyE = new Key(70, 70, "", "E");
            keyE.Location = new Point(startX, startY);
            Controls.Add(keyE);

            startX += keyE.Width + 4;

            keyF = new Key(70, 70, "", "F");
            keyF.Location = new Point(startX, startY);
            Controls.Add(keyF);

            startX  = 14;
            startY += keyE.Height + 4;

            keySingleStep = new Key(70, 70, "SINGLE STEP", "");
            keySingleStep.Location = new Point(startX, startY);
            Controls.Add(keySingleStep);

            startX += keySingleStep.Width + 4;

            keyGo = new Key(70, 70, "GO", "");
            keyGo.Location = new Point(startX, startY);
            Controls.Add(keyGo);

            startX += keyGo.Width + 4;

            key8 = new Key(70, 70, "H", "8");
            key8.Location = new Point(startX, startY);
            Controls.Add(key8);

            startX += key8.Width + 4;

            key9 = new Key(70, 70, "L", "9");
            key9.Location = new Point(startX, startY);
            Controls.Add(key9);

            startX += key9.Width + 8;

            keyA = new Key(70, 70, "", "A");
            keyA.Location = new Point(startX, startY);
            Controls.Add(keyA);

            startX += keyA.Width + 4;

            keyB = new Key(70, 70, "", "B");
            keyB.Location = new Point(startX, startY);
            Controls.Add(keyB);

            startX  = 14;
            startY += keyB.Height + 4;

            keySubstMem = new Key(70, 70, "SUBST MEM", "");
            keySubstMem.Location = new Point(startX, startY);
            Controls.Add(keySubstMem);

            startX += keySubstMem.Width + 4;

            keyExamReg = new Key(70, 70, "EXAM REG", "");
            keyExamReg.Location = new Point(startX, startY);
            Controls.Add(keyExamReg);

            startX += keyExamReg.Width + 4;

            key4 = new Key(70, 70, "SPH", "4");
            key4.Location = new Point(startX, startY);
            Controls.Add(key4);

            startX += key4.Width + 4;

            key5 = new Key(70, 70, "SPL", "5");
            key5.Location = new Point(startX, startY);
            Controls.Add(key5);

            startX += key5.Width + 8;

            key6 = new Key(70, 70, "PCH", "6");
            key6.Location = new Point(startX, startY);
            Controls.Add(key6);

            startX += key6.Width + 4;

            key7 = new Key(70, 70, "PCL", "7");
            key7.Location = new Point(startX, startY);
            Controls.Add(key7);

            startX  = 14;
            startY += key7.Height + 4;

            keyNext = new Key(70, 70, "NEXT ,", "");
            keyNext.Location = new Point(startX, startY);
            Controls.Add(keyNext);

            startX += keyNext.Width + 4;

            keyExec = new Key(70, 70, "EXEC .", "");
            keyExec.Location = new Point(startX, startY);
            Controls.Add(keyExec);

            startX += keyExec.Width + 4;

            key0 = new Key(70, 70, "", "0");
            key0.Location = new Point(startX, startY);
            Controls.Add(key0);

            startX += key0.Width + 4;

            key1 = new Key(70, 70, "", "1");
            key1.Location = new Point(startX, startY);
            Controls.Add(key1);

            startX += key1.Width + 8;

            key2 = new Key(70, 70, "", "2");
            key2.Location = new Point(startX, startY);
            Controls.Add(key2);

            startX += key2.Width + 4;

            key3 = new Key(70, 70, "|", "3");
            key3.Location = new Point(startX, startY);
            Controls.Add(key3);
        }

        #endregion

        #region EventHandlers

        /// <summary>
        /// Form loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormSDK_85_Load(object sender, EventArgs e)
        {
            // Set location of window
            this.Location = new Point(x, y); 
        }

        #endregion
    }
}
