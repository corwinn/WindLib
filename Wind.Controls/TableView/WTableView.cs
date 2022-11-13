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

//TODO write me a test unit; ASAP

#define VISUAL_DBG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace Wind.Controls
{
    // Bridge the TableView to the underlying Control. Handle as much un-ignore-able-window_manager_details" as possible here -
    // the TableView has its own mess to deal with. A.k.a. : lower the number of responsibilities per actor, to 1.
    public class WTableViewBridge : WControl
    {
        protected IWTableViewModel _model = null;
        public WTableViewBridge()
            : base ()
        {
            this.ResizeRedraw = true;
            this.Grid = new GridRenderer ();
            _vscroll.Parent = this; _vscroll.Model = _vscroll_model;
            _hscroll.Parent = this; _hscroll.Model = _hscroll_model;
            _vscroll.Visible = _hscroll.Visible = false;
        }
        protected override bool NoRender { get { return null == _model || base.NoRender; } }
        protected override void Render(Graphics gc)
        {
            Grid.Render (gc);
            if (_hscroll.Visible && _vscroll.Visible) gc.FillRectangle (WStyle.FillBtnInactive, _scroll_cube);
        }
        // protected Bitmap _back_buf = null; changes nothing yet, so no need to //LATER - region update, update. Changed only, etc.
        protected override void OnResize(EventArgs e)
        {
            base.OnResize (e); base.RenderArea = this.ClientRectangle; LayoutTheScrollBars ();
            // if (RenderArea.Width > 0 && RenderArea.Height > 0)
            //     _back_buf = new Bitmap (RenderArea.Width, RenderArea.Height);
        }
        public IWTableViewModel Model { get { return _model; } set { SetModel (value); } }
        private void SetModel(IWTableViewModel value) { if (value != _model) { _model = value; OnModelChanged (); } }
        protected virtual void OnModelChanged() { }
        protected class GridRenderer // {Use = true, Visible = false, Width = 2} will essentially be a cell padding.
        {//TODO this could be at separate unit: TableViewGridRenderer.cs
            public int Width = 0;
            public bool Visible = false;
            public bool Use = false;
            public virtual void Render(Graphics gc) { }
        }
        /// <summary>Choose whether or not the control renders a grid between your cells.</summary>
        protected GridRenderer Grid { get; private set; }
        //TODO selection Renderer - as the Grid; allow the user to decide.

        // scrollbars
        protected WTableViewScrollModel _vscroll_model = new WTableViewScrollModel ();
        protected WTableViewScrollModel _hscroll_model = new WTableViewScrollModel ();
        protected WVerticalScrollBar _vscroll = new WVerticalScrollBar ();
        protected WHorizontalScrollBar _hscroll = new WHorizontalScrollBar ();
        protected Rectangle _scroll_cube = new Rectangle ();

        protected void LayoutTheScrollBars()
        {
            _vscroll.Location = new Point (RenderArea.Width - _vscroll.Width, 0);
            _vscroll.Height = RenderArea.Height - (_hscroll.Visible ? _hscroll.Height : 0);
            _hscroll.Location = new Point (0, RenderArea.Height - _hscroll.Height);
            _hscroll.Width = RenderArea.Width - (_vscroll.Visible ? _vscroll.Width : 0);
            _scroll_cube.Location = new Point (_hscroll.Left + _hscroll.Width, _vscroll.Top + _vscroll.Height);
            _scroll_cube.Size = new Size (_vscroll.Width, _hscroll.Height);
        }
    }// WTableViewBridge

    // A table view of a data model. Useful when you want to display/edit your data in tabular manner:
    //   tble_view.Model = my_data_model;
    //
    // This is my 4th iteration over this code. There's almost nothing remaining from the 1st one.
    // Each iteration has but a simple goal: no code repetition, with "short and simple" in mind.
    public class WTableView : WTableViewBridge
    {
        public static int Align2(int a) { return (a + 1) & ~1; }
        public WTableView()
            : base ()
        {
            this.Grid.Width = 3;
            this.Grid.Use = true;
            _vscroll_model.Scroll += (a, b) => { HandleScrollEvent (a, b); };//TODO _scroll_models are parameter
            _hscroll_model.Scroll += (a, b) => { HandleScrollEvent (a, b); };//

#if VISUAL_RND_DBG
            // Warning! You'll find this form difficult to click on.
            _dbg_timer.Interval = 250;
            _dbg_timer.Tick += (a, b) =>
            {
                var d = (2 == _dbg_rnd_gen.Next (3) ? -1 : 1);
                _sx += d * _dbg_rnd_gen.Next (20); ScrollTheModel (vertical: false, direction: d);
                _sy += d * _dbg_rnd_gen.Next (20); ScrollTheModel (vertical: true, direction: -d);

                this.FindForm ().Size = new Size (
                    800 + (2 == _dbg_rnd_gen.Next (3) ? -1 : 1) * _dbg_rnd_gen.Next (50),
                    600 + (2 == _dbg_rnd_gen.Next (3) ? -1 : 1) * _dbg_rnd_gen.Next (50));
                this.FindForm ().Location = new Point (
                    80 + (2 == _dbg_rnd_gen.Next (3) ? -1 : 1) * _dbg_rnd_gen.Next (50),
                    60 + (2 == _dbg_rnd_gen.Next (3) ? -1 : 1) * _dbg_rnd_gen.Next (50));
            };
            _dbg_timer.Start ();
        }
        private static Random _dbg_rnd_gen = new Random ();
        private static System.Windows.Forms.Timer _dbg_timer = new System.Windows.Forms.Timer ();
#else
        }
#endif

        private int _sx = 0, _sy = 0; // x and y modified by scroll (top left)
        private List<List<IWTableViewCell>> _cells = new List<List<IWTableViewCell>> ();
        private List<List<IWTableViewCell>> _column_header = new List<List<IWTableViewCell>> (); // not vertically scrollable

        private void ResetVisibleCellContainers()
        {
            foreach (var row in _cells) row.Clear ();
            _cells.Clear ();
            foreach (var row in _column_header) row.Clear ();
            _column_header.Clear ();
        }

        private void HandleScrollEvent(object sender, WScrollerEventArgs e)
        {
            int d = 0;
            bool vertical = object.ReferenceEquals (_vscroll_model, sender);
            bool horizontal = object.ReferenceEquals (_hscroll_model, sender);
            if (!(vertical != horizontal)) throw new ArgumentException ("Fishy sender", "sender");
            int big_delta = (vertical ? RenderArea.Height : RenderArea.Width) / 20;
            switch (e.Event)
            {
                case WScrollerEvent.Min | WScrollerEvent.Small: d = 1; break;
                case WScrollerEvent.Max | WScrollerEvent.Small: d = -1; break;
                case WScrollerEvent.Min | WScrollerEvent.Large: d = big_delta; break;
                case WScrollerEvent.Max | WScrollerEvent.Large: d = -big_delta; break;
                case WScrollerEvent.Distinct: d = -e.Amount * big_delta; break;
                default: throw new ArgumentException ("Unknown scroll event", "e.Event");
            }
            if (vertical) _sy += d; else _sx += d;
            ScrollTheModel (vertical, d);
            this.Refresh ();
        }

        //TODO update me when adding the column footer
        private bool Model_MoveSharedColumnPtrs(int offset) // column headers and the data share same vertical ranges
        {
            return new bool[] { _model.ColumnHeaderStream ().Move (offset, 0), _model.DataStream ().Move (offset, 0) }.Count (x => x) > 0;
        }
        private bool Model_MoveSharedRowPtrs(int offset)
        {
            return new bool[] { _model.DataStream ().Move (0, offset) }.Count (x => x) > 0;
        }

        //TODO update me when adding the column footer
        private IEnumerable<List<IWTableViewCell>> ScrollableColumns()
        {
            return _column_header.Where (row => row.Count > 0).Union (_cells.Where (row => row.Count > 0));
        }
        /*private IEnumerable<List<ITableViewCell>> ScrollableRows() - its always _cells, so
        {
            return _cells.Where (row => row.Count > 0);
        }*/
        private bool AnyColumns(IEnumerable<List<IWTableViewCell>> cell_set)
        {
            return cell_set.Count () > 0 && cell_set.Any (row => row.Count > 0);
        }

        // Because it will be thrown a lot until this code is refined by a test suite.
        public class DreadedException : Exception { public DreadedException(string message) : base (message: message) { } }
        private void CacheConsistencyCheck()//TODO remove the DreadedException when code is proven by a test suite
        {// Don't do this unless you're ready to handle a lot of dialogs. //TODO Do Log() instead.
            if (_cells.Count <= 0) return;
            Debug.Assert (1 == _cells.GroupBy (row => row.Count).Count (), "_cells shall always be a 2D array");
            if (1 != _cells.GroupBy (row => row.Count).Count ())
                throw new DreadedException ("Fixme:_cells shall always be a 2D array");
        }

        protected override void OnResize(EventArgs e)//TODONT filter these out to 16 per second; not simple;
        {
            bool h_grew = ClientRectangle.Height > this.RenderArea.Height,
                v_grew = ClientRectangle.Width > this.RenderArea.Width;
            //TODO these will be be gone when ScrollTheModel() is gone
            if (h_grew && v_grew)
            {
                this.RenderArea = new Rectangle (this.RenderArea.X, this.RenderArea.Y, this.RenderArea.Width, ClientRectangle.Height);
                ScrollTheModel (vertical: true, direction: -1);
                base.OnResize (e);
                ScrollTheModel (vertical: false, direction: 1);
            }
            base.OnResize (e);
            ScrollTheModel (vertical: true, direction: v_grew ? -1 : 1);
            ScrollTheModel (vertical: false, direction: h_grew ? 1 : -1);

            // Endless streams are easy. Smooth scrolling a finite number of ranges - thats hard.
            // Because only a fraction of your data is being rendered at time, only a limited number of ranges
            // will be auto-sized e.g. at any given time the scrollbars will reflect whatever was resized so far.
            _vscroll_model.SmallSize = _cells.Count;
            if (null != _model) _vscroll_model.LargeSize = _model.SizeVertical;
            //_vscroll.Visible = measure_result_h < 0;
            if (ScrollableColumns ().Count () > 0) _hscroll_model.SmallSize = ScrollableColumns ().First ().Count ();
            int cw = ScrollableColumns ().Count () <= 0 ? 0 :
                    _sx + GridSize () + ScrollableColumns ().First ().Sum (x => x.VRange.Size) + ScrollableColumns ().First ().Count * GridSize ();
            if (cw > RenderArea.Width) _hscroll_model.SmallSize = _hscroll_model.SmallSize - 1;
            if (null != _model) _hscroll_model.LargeSize = _model.SizeHorizontal;
            //_hscroll.Visible = measure_result_w < 0;
        }

        //TODO given what follows, perhaps _cell should become a net: Cell<T> { Cell<T> * Left, * Right, * Top, * Bottom; void Clip (); }?

        private void ScrollTheModel(bool vertical, int direction)//TODO the one function
        {
            if (null == _model || (_cells.Count <= 0 && _column_header.Count <= 0)) return;
            if (this.RenderArea.Width <= 0 || this.RenderArea.Height <= 0) return;
            // this is wrong: there could be a column header and no cells
            if (!vertical /*&& AnyColumns (ScrollableColumns ())*/) // there is at least one column
            {
                // check for a gap on the left
                if (_sx > 0)
                {
                    while (_sx > 0)
                        if (Model_MoveSharedColumnPtrs (-1)) // model being scrolled by (-1, 0)
                        {
                            Model_InsertColumn (0);
                            _sx -= ScrollableColumns ().First ()[0].VRange.Size + GridSize ();
                        }
                        else _sx = 0; // anchor to 0 when there are no more columns
                    RemoveNonVisibleRightmostRanges (); RemoveNonVisibleBottomRanges ();
                }

                // check for a gap on the right
                int cw = ScrollableColumns ().Count () <= 0 ? 0 :
                    _sx + GridSize () + ScrollableColumns ().First ().Sum (x => x.VRange.Size) + ScrollableColumns ().First ().Count * GridSize ();
                _hscroll.Visible = cw >= RenderArea.Width;
                int columns = ScrollableColumns ().Count () <= 0 ? 0 : ScrollableColumns ().First ().Count;
                if (cw < RenderArea.Width)
                {
                    while (cw < RenderArea.Width)
                        if (Model_MoveSharedColumnPtrs (columns)) // model being scrolled by (n, 0)
                        {
                            Model_InsertColumn (columns);
                            Model_MoveSharedColumnPtrs (-columns);
                            cw += ScrollableColumns ().First ()[0].VRange.Size + GridSize ();
                            columns = ScrollableColumns ().Count () <= 0 ? 0 : ScrollableColumns ().First ().Count;
                            _hscroll.Visible = cw >= RenderArea.Width;
                        }
                        else
                        {
                            if (cw > RenderArea.Width) _sx += (RenderArea.Width - cw); // anchor to RenderArea.Width if there is anything to anchor
                            break;
                        }
                    RemoveNonVisibleLeftmostRanges ();
                }
                RemoveNonVisibleBottomRanges ();
            }// horizontal scrolling
            else
            {
                // check the top gap
                var scrollable_top = 0;
                if (_sy > scrollable_top && _cells.Count > 0)
                {
                    while (_sy > scrollable_top)
                        if (Model_MoveSharedRowPtrs (-1)) // model being scrolled by (0, -1)
                        {
                            Model_InsertRow (0);
                            _sy -= _cells[0][0].HRange.Size + GridSize ();
                        }
                        else _sy = scrollable_top; // anchor to 0 when there are no more rows
                    // Model_InsertRow () could insert a different number of cells should column size change e.g. RemoveNonVisibleRightmostRanges().
                    RemoveNonVisibleBottomRanges (); RemoveNonVisibleRightmostRanges ();
                }

                // check the bottom gap
                var ch = _cells.Count > 0 ? _sy + GridSize () + ColumnHeaderHeight () + _cells.Sum (row => row[0].HRange.Size + GridSize ())
                    : RenderArea.Height;
                int rows = _cells.Count;
                if (ch < RenderArea.Height)
                {
                    while (ch < RenderArea.Height)
                    {
                        if (Model_MoveSharedRowPtrs (rows)) // model being scrolled by (0, n)
                        {
                            Model_InsertRow (rows);
                            Model_MoveSharedRowPtrs (-rows);
                            ch += _cells[_cells.Count - 1][0].HRange.Size + GridSize ();
                            rows = _cells.Count;
                        }
                        else
                        {
                            if (ch > RenderArea.Height) _sy += (RenderArea.Height - ch); // anchor to RenderArea.Height if there is anything to anchor
                            break;
                        }
                    }
                    RemoveNonVisibleTopRanges ();
                }
                RemoveNonVisibleRightmostRanges ();
            }// vertical scrolling
            CacheConsistencyCheck ();
        }// ScrollTheModel()

        private int ColumnHeaderHeight()
        {
            return _column_header.Count () > 0 ? _column_header.Sum (x => x[0].HRange.Size + GridSize ()) : 0;
        }

        private void RemoveNonVisibleRightmostRanges()//TODO Clip()
        {
            var vranges = ScrollableColumns ().First (); // any row is useful since vranges are shared across ScrollableColumns
            int last_visible_range = 0; int p = _sx + GridSize ();
            for (last_visible_range = 0; last_visible_range < vranges.Count; )
                if ((p += vranges[last_visible_range].VRange.Size + GridSize ()) < RenderArea.Width) last_visible_range++; else break;
            if (last_visible_range > 0)
                foreach (var r in ScrollableColumns ())
                    if (last_visible_range + 1 < r.Count) //TODO needed by the improper ModelChanged measuring
                        r.RemoveRange (last_visible_range + 1, r.Count - (last_visible_range + 1));
        }// RemoveNonVisibleRightmostRanges()
        private void RemoveNonVisibleLeftmostRanges()//TODO Clip()
        {
            var vranges = ScrollableColumns ().First (); // any row is useful ... ^
            int inv_columns = 0;
            for (inv_columns = 0; inv_columns < vranges.Count && _sx + GridSize () + vranges[inv_columns].VRange.Size <= 0; inv_columns++)
                _sx += GridSize () + vranges[inv_columns].VRange.Size;
            if (inv_columns > 0)
            {
                foreach (var r in ScrollableColumns ())
                    if (inv_columns <= r.Count) r.RemoveRange (0, inv_columns);
                Model_MoveSharedColumnPtrs (inv_columns); // since your cr pointer is my top left cell
            }
        }// RemoveNonVisibleLeftMostRanges()
        private void RemoveNonVisibleBottomRanges()//TODO Clip()
        {
            int last_visible_range = 0; int p = _sy + ColumnHeaderHeight () + GridSize ();
            for (last_visible_range = 0; last_visible_range < _cells.Count; )
                if ((p += _cells[last_visible_range][0].HRange.Size + GridSize ()) < RenderArea.Height) last_visible_range++; else break;
            if (last_visible_range > 0 && last_visible_range + 1 < _cells.Count)
                _cells.RemoveRange (last_visible_range + 1, _cells.Count - (last_visible_range + 1));
        }// RemoveNonVisibleBottomRanges()
        private void RemoveNonVisibleTopRanges()//TODO Clip()
        {
            int inv_rows = 0;
            for (inv_rows = 0; inv_rows < _cells.Count && _sy + GridSize () + _cells[inv_rows][0].HRange.Size <= 0; inv_rows++)
                _sy += GridSize () + _cells[inv_rows][0].HRange.Size;
            if (inv_rows > 0)
            {
                if (inv_rows <= _cells.Count) _cells.RemoveRange (0, inv_rows);
                Model_MoveSharedRowPtrs (inv_rows); // since your cr pointer is my top left cell
            }
        }// RemoveNonVisibleTopRanges()

        // All methods bridging the model to here, will be prefixed with Model_.

        private delegate bool Model_MoveNext(IWTableViewModel stream, ref int rows, ref int columns);
        private delegate void Model_OnCell(IWTableViewCell cell, int row, int column);
        private void Model_Walk(IWTableViewModel stream, WGraphicsContext gc, Model_OnCell on_cell, Model_MoveNext move_next)
        {// If you're looking for the "engine" - this is it. All the code around it, is "the details".
            if (null == stream.Current) return; // no cells
            int rows = 0; int columns = 0;
            do
            {
                var cell = stream.Current;
                cell.Measure (gc);
                on_cell (cell, rows, columns);
            } while (move_next (stream, ref rows, ref columns));
            stream.Move (-columns, -rows);
        }

        // Inserts exactly one column.
        // - 0     : insert new 1st column, a.k.a. R->L fill
        // - count : Add(), a.k.a. L->R fill
        private void Model_InsertColumn(int index)
        {
            using (var sys_gc = Graphics.FromHwnd (this.Handle))
            using (var gc = WGraphicsContext.FromGraphics (sys_gc))
            {
                int p = _sy + GridSize ();//TODO these walks in the RenderArea must be at one place
                if (_column_header.Count > 0) Model_Walk (_model.ColumnHeaderStream (), gc,
                    (cell, row, column) => { _column_header[row].Insert (index, cell); p += cell.HRange.Size + GridSize (); },
                    (IWTableViewModel stream, ref int r, ref int c) => { r++; return stream.Move (0, 1) && p < RenderArea.Height; });
                if (_cells.Count > 0) Model_Walk (_model.DataStream (), gc,
                    (cell, row, column) => { _cells[row].Insert (index, cell); p += cell.HRange.Size + GridSize (); },
                    (IWTableViewModel stream, ref int r, ref int c) => { r++; return stream.Move (0, 1) && p < RenderArea.Height; });
            }
        }// Model_InsertColumn()
        private void Model_InsertRow(int index) // - 0 : T->B fill; etc.
        {
            var new_row = new List<IWTableViewCell> ();
            _cells.Insert (index, new_row);
            using (var sys_gc = Graphics.FromHwnd (this.Handle))
            using (var gc = WGraphicsContext.FromGraphics (sys_gc))
            {
                int p = _sx + GridSize ();
                if (_cells.Count > 0) Model_Walk (_model.DataStream (), gc,
                    (cell, row, column) => { new_row.Add (cell); p += cell.VRange.Size + GridSize (); },
                    (IWTableViewModel stream, ref int r, ref int c) => { c++; return stream.Move (1, 0) && p < RenderArea.Width; });
            }
        }// Model_InsertRow()

        private int GridSize() { return this.Grid.Use ? this.Grid.Width : 0; }

        protected override void OnModelChanged()
        {
            int w = RenderArea.Width - GridSize (); // grid topmost line
            int h = RenderArea.Height - GridSize (); // grid leftmost line
            int vscroll_area_height = h;
            int hscroll_area_width = w;
            int measure_result_h = h; int measure_result_w = w;
            ResetVisibleCellContainers ();
            using (var sys_gc = Graphics.FromHwnd (this.Handle))
            using (var gc = WGraphicsContext.FromGraphics (sys_gc))
            {
                Model_Stream2View (ref measure_result_h, ref measure_result_w, _model.ColumnHeaderStream (), _column_header, gc);
                measure_result_w = w;
                Model_Stream2View (ref measure_result_h, ref measure_result_w, _model.DataStream (), _cells, gc);
                RemoveNonVisibleRightmostRanges ();
            }

            // scrollbars
            _vscroll_model.SmallSize = vscroll_area_height;
            _vscroll_model.LargeSize = _model.SizeVertical;
            _vscroll.Visible = measure_result_h < 0;
            _hscroll_model.SmallSize = hscroll_area_width;
            _hscroll_model.LargeSize = _model.SizeHorizontal;
            _hscroll.Visible = measure_result_w < 0;
            LayoutTheScrollBars ();
        }// protected override void OnModelChanged

        // Helper for the OnModelChanged() //TODO should disappear on the next iteration: the thing that happens
        //                                        on scroll, on resize, on model change, on cake, etc., shall be Model2View()
        private void Model_Stream2View(ref int h, ref int w, IWTableViewModel cell_stream, List<List<IWTableViewCell>> cell_set, WGraphicsContext gc)
        {
            int keep_h = h, ih = h, iw = w, keep_w = w;
            Model_Walk (cell_stream, gc,
                (cell, row, column) =>
                {

                    if (row >= cell_set.Count)
                        cell_set.Add (new List<IWTableViewCell> ());
                    ih -= cell.HRange.Size + GridSize ();
                    cell_set[row].Add (cell);
                    if (0 == row) iw -= cell.VRange.Size + GridSize ();//TODONT accuracy: use RemoveNonVisibleRightmostRanges()
                },
                (IWTableViewModel stream, ref int r, ref int c) =>
                {// scan column by column; because its more likely for column widths to change, rather than row heights
                    bool more_rows = stream.Move (0, 1);
                    if (!more_rows || ih < 0) // no more rows or space
                    {
                        if (iw >= 0)
                        {// add new column
                            ih = keep_h;
                            c++; int rows = r + (more_rows ? 1 : 0); r = 0; return stream.Move (1, -rows);
                        }
                        else { r = r + (more_rows ? 1 : 0); return false; }
                    }
                    else r++;
                    return ih >= 0;
                });
            h = ih; w = iw;
        }// Model_Stream2View()

        // Returns a clip rectangle that excludes the headers. "l"eft, "r"ight, "t"op and "b"ottom header size.
        private Rectangle DataCellsClip(int l, int r, int t, int b)
        {
            // The grid size is required in order to keep the grid visible when there is a header prior it
            //TODO test the bottom and right headers situation
            return new Rectangle (l + (l > 0 ? GridSize () : 0), t + (t > 0 ? GridSize () : 0),
                RenderArea.Width - (r + l), RenderArea.Height - (int)(b + t));
        }

        //R static Stopwatch r_tm = new Stopwatch ();
#if VISUAL_DBG
        static float _dbg_scale_factor = .8f;
        static Pen _dbg_red_dash = new Pen (Color.Red, 2);
        static Pen _dbg_render_area_pen = new Pen (Color.WhiteSmoke, 2);
        static Pen _dbg_sx_pen = new Pen (Color.Blue, 1);
        static Pen _dbg_sy_pen = new Pen (Color.Green, 1);
        static WTableView()
        {
            _dbg_red_dash.DashStyle = _dbg_render_area_pen.DashStyle =
                _dbg_sx_pen.DashStyle = _dbg_sy_pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
        }
#endif

        protected override void Render(Graphics sys_gc)
        {
#if VISUAL_DBG
            sys_gc.FillRectangle (Brushes.LightGray, this.RenderArea); // the grid
            sys_gc.TranslateTransform (.5f * RenderArea.Width, .5f * RenderArea.Height);
            sys_gc.ScaleTransform (_dbg_scale_factor, _dbg_scale_factor);
            sys_gc.TranslateTransform (-.5f * RenderArea.Width, -.5f * RenderArea.Height);
#endif

            //R r_tm.Reset (); r_tm.Start ();
            sys_gc.FillRectangle (Brushes.DarkCyan, this.RenderArea); // the grid
            //D var sys_gc = Graphics.FromImage(_back_buf); see above

            using (var gc = WGraphicsContext.FromGraphics (sys_gc))
            {
                //sys_gc.SetClip (DataCellsClip (l: 0, r: 0, t: 0, b: 0)); //TODO when left and right row headers become available
                if (_column_header.Count > 0)
                {
                    sys_gc.TranslateTransform (_sx + GridSize (), GridSize ()); // scrollable along y, but not along x
                    RenderCellSet (_column_header, sys_gc, gc);
                    sys_gc.TranslateTransform (-_sx, 0);
                    // The clip rectangle plays together with the transformation matrix, so:
                    var top_header_height = _column_header.Sum (x => x[0].HRange.Size) + (1 + _column_header.Count) * GridSize ();
                    sys_gc.TranslateTransform (0, -top_header_height);
#if VISUAL_DBG
                    sys_gc.DrawRectangle (_dbg_red_dash, 0, top_header_height, RenderArea.Width, RenderArea.Height - top_header_height);
#else
                    sys_gc.SetClip (DataCellsClip (l: 0, r: 0, t: top_header_height, b: 0));
#endif
                    sys_gc.TranslateTransform (0, top_header_height);
                }

                if (_cells.Count > 0)
                {
                    sys_gc.TranslateTransform (_sx + GridSize (), _sy + GridSize ());
                    RenderCellSet (_cells, sys_gc, gc);
                }
            }
            sys_gc.ResetClip ();
            sys_gc.ResetTransform ();
            base.Render (sys_gc);

#if VISUAL_DBG
            sys_gc.TranslateTransform (.5f * RenderArea.Width, .5f * RenderArea.Height);
            sys_gc.ScaleTransform (_dbg_scale_factor, _dbg_scale_factor);
            sys_gc.TranslateTransform (-.5f * RenderArea.Width, -.5f * RenderArea.Height);
            sys_gc.DrawLine (_dbg_sx_pen, _sx, -RenderArea.Height * (2 - _dbg_scale_factor), _sx, RenderArea.Height * (2 - _dbg_scale_factor));
            sys_gc.DrawLine (_dbg_sy_pen, -RenderArea.Width * (2 - _dbg_scale_factor), _sy, RenderArea.Width * (2 - _dbg_scale_factor), _sy);
            sys_gc.DrawRectangle (_dbg_render_area_pen, this.RenderArea);
#endif
            //D sys_gc2.DrawImage(_back_buf, 0, 0); see above
            //R r_tm.Stop ();
            //R Debug.WriteLine ("r time: " + r_tm.ElapsedMilliseconds + " ms");
        }// Render()

        private void RenderCellSet(List<List<IWTableViewCell>> cell_set, Graphics sys_gc, WGraphicsContext gc)
        {
#if VISUAL_DBG
            var x_sentinel = sys_gc.Transform.OffsetX / _dbg_scale_factor;
#else
            var x_sentinel = sys_gc.Transform.OffsetX;
#endif
            foreach (var row in cell_set)
            {
                var h = 0;
                for (int i = 0; i < row.Count; i++)
                {
                    var cell = row[i];
                    cell.Render (gc);
                    sys_gc.TranslateTransform (cell.VRange.Size + GridSize (), 0);
                    h = cell.HRange.Size;
                }
#if VISUAL_DBG
                sys_gc.TranslateTransform (-(sys_gc.Transform.OffsetX / _dbg_scale_factor - x_sentinel), h + GridSize ());
#else
                sys_gc.TranslateTransform (-(sys_gc.Transform.OffsetX - x_sentinel), h + GridSize ());
#endif
            }
            sys_gc.TranslateTransform (-GridSize (), -GridSize ());
        }// RenderCellSet()
    }// public class WTableView
}
