/**** BEGIN LICENSE BLOCK ****

BSD 3-Clause License

Copyright (c) 2022, the wind.
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

**** END LICENCE BLOCK ****/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TableViewExample
{
    using Wind.Controls;

    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent ();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Padding = new System.Windows.Forms.Padding (10);
            tv.Parent = this;
            tv.Dock = DockStyle.Fill;
            ExampleCell.VR.AutoSize = true;
            ExampleCell.ColumnHR.AutoSize = true;
            //ExampleCell.ColumnHR.Header = true;
            tv.Model = new ExampleModel ();

            /*Dictionary<EventArgs, int> t = new Dictionary<EventArgs, int> ();
            Stopwatch s = new Stopwatch ();
            List<EventArgs> _lst = new List<EventArgs> ();
            for (int i = 0; i < 10000; i++) _lst.Add (new EventArgs ());
            s.Start ();
            foreach (var i in _lst) t.Add (i, 10);
            s.Stop ();
            MessageBox.Show(s.ElapsedMilliseconds.ToString());*/
        }

        private WTableViewBridge tv = new WTableView ();
    }// public partial class FormMain

    // Now that all of this works, creating a TreeViewCell is meh :).
    class ExampleCell : IWTableViewCell // (ColumnHeader + Data)Cell; usually you want these to be separate cells - more simple code
    {
        private bool _update = false;
        private bool _column_header = false;

        // there will be text rendering
        private string _text = "";
        private int _tx = 0, _ty = 0, _tw = 0, _th = 0;
        private StringFormat _text_fmt = new StringFormat ();
        private Font _text_font = SystemFonts.DefaultFont; // never do this at a render proc

        static bool _VR_SizeChanged_the_way_is_shut = false; // an event handler shouldn't cause events it has to handle

        public ExampleCell(bool column_header, string text)
        {
            _column_header = column_header;
            _text = text;
            _update = true;
            _text_fmt.Alignment = StringAlignment.Center;
            _text_fmt.LineAlignment = StringAlignment.Center;
            VR.SizeChanged += (d) =>
            {
                if (_VR_SizeChanged_the_way_is_shut)
                    throw new Exception ("Event-driven! Please do not trouble trouble 'till it troubles you.");
                _VR_SizeChanged_the_way_is_shut = true;
                try
                {
                    // put all event handling code here, only
                    UpdateLayout ();
                }
                finally { _VR_SizeChanged_the_way_is_shut = false; }
            };
        }
        // use global defaults
        public static WTableViewRange HR = new WTableViewRange ();
        public static WTableViewRange VR = new WTableViewRange ();
        public static WTableViewRange ColumnHR = new WTableViewRange ();

        #region ITableViewCell Members
        public WTableViewRange HRange { get { return _column_header ? ColumnHR : HR; } }
        public WTableViewRange VRange { get { return VR; } }
        public bool Selected { get { return false; } set { } }
        public bool Changed { get { return _update; } set { _update = value; } }
        private void UpdateLayout()
        {
            // This is foolish - I really need "freetype2" here; how to auto "..." the string so it can fit given width?
            // Measure 20 times?
            _tx = WTableView.Align2 (VR.Size - _tw) / 2; // horizontal center| this is just an example - otherwise you should use the range alignment;
            _ty = WTableView.Align2 ((_column_header ? ColumnHR.Size : HR.Size) - _th) / 2; // vertical center | its highly recommended to create a CellRenderer to handle these things
            //Debug.Write (string.Format ("w: {0}, tw: {1}, tx: {2}, ", VR.Size, _tw, _tx));
            //Debug.WriteLine (string.Format ("h: {0}, th: {1}, ty: {2}", HR.Size, _th, _ty));
        }
        public Size Measure(WGraphicsContext gc)
        {
            if (!_update) return new Size (VR.Size, HR.Size);
            var ts = gc.MeasureText (_text, SystemFonts.DefaultFont);
            _tw = ts.Width;
            _th = ts.Height;
            _update = false;
            VR.UpdateSize (WTableView.Align2 (_tw));
            if (_column_header) ColumnHR.UpdateSize (WTableView.Align2 (_th));
            else HR.UpdateSize (WTableView.Align2 (_th));
            UpdateLayout ();
            return new Size (VR.Size, _column_header ? ColumnHR.Size : HR.Size); // update the range - a.k.a. layout
        }
        public void Render(WGraphicsContext gc)
        {
            gc.FillRectangle ((_column_header ? Brushes.PaleGreen : Brushes.LightBlue), 0, 0, VR.Size,
                _column_header ? ColumnHR.Size : HR.Size);
            //TODO it takes a lot of time and code to not render out of the cell; just use "freetype2" here
            if (_tx < 0 || _ty < 0)
                gc.DrawString (_text, SystemFonts.DefaultFont, Brushes.Black,
                    new RectangleF (0, 0, VR.Size, HR.Size), _text_fmt);
            else gc.DrawString (_text, _text_font, Brushes.Black, _tx, _ty);
        }
        public bool New { get { return false; } set { } }
        #endregion
    }// class ExampleCell

    class ExampleModel : IWTableViewModel
    {
        class CellStream // this should be your actual data
        {
            public CellStream(bool h) { hcell = h; }
            public bool hcell = false;
            public long row_ptr = 0; // rc pointer
            public long col_ptr = 0; //
            public ExampleCell cell = null;
            //public IEnumerable<ITableViewCell> Get(int num = 1)
            public IWTableViewCell Get()
            {
                /*if (num < 1) return null;
                var result = new List<ExampleCell> (num);
                for (int i = 0; i < num; i++) result.Add (
                    new ExampleCell (hcell, string.Format ("x:{0}, y:{1}", col_ptr, row_ptr)));
                return result;*/
                return new ExampleCell (hcell, string.Format ("x:{0}, y:{1}", col_ptr, row_ptr));
            }
            //private long _rptr = 0;
            //private long _cptr = 0;
            //public void Push() { _rptr = row_ptr; _cptr = col_ptr; }
            //public void Pop() { row_ptr = _rptr; col_ptr = _cptr; }
        }// class CellStream
        CellStream header_cell_stream = new CellStream (true);
        CellStream data_cell_stream = new CellStream (false);
        CellStream[] streams = null;
        private int _stream = 0;
        public ExampleModel()
        {
            streams = new CellStream[2] { header_cell_stream, data_cell_stream };
        }
        #region ITableViewModel Members
        public IWTableViewCell Current { get { return streams[_stream].Get (); } }
        public bool Move(int column_offset, int row_offset)
        {
            if (column_offset != 0) { streams[_stream].col_ptr += column_offset; }
            if (row_offset != 0)
            {
                if (0 == _stream) return false; // column header is one row
                streams[_stream].row_ptr += row_offset;
            }
            return true;
        }
        public IWTableViewModel ColumnHeaderStream() { _stream = 0; return this; }
        public IWTableViewModel DataStream() { _stream = 1; return this; }
        public void Set(IEnumerable<IWTableViewCell> cells) { throw new NotImplementedException (); }
        public long SizeVertical { get { return 0; } } // endless stream
        public long SizeHorizontal { get { return 0; } } // endless stream
        #endregion
    }// class ExampleModel
}
