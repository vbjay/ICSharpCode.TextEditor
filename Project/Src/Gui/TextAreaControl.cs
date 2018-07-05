// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor
{
	/// <summary>
	/// This class paints the textarea.
	/// </summary>
	[ToolboxItem(false)]
	public class TextAreaControl : Panel
	{
	    private TextEditorControl motherTextEditorControl;

	    private HRuler     hRuler     = null;

	    private VScrollBar vScrollBar = new VScrollBar();
	    private HScrollBar hScrollBar = new HScrollBar();
	    private TextArea   textArea;
	    private bool       doHandleMousewheel = true;
	    private bool       disposed;
		
		public TextArea TextArea {
			get {
				return textArea;
			}
		}
		
		public SelectionManager SelectionManager {
			get {
				return textArea.SelectionManager;
			}
		}
		
		public Caret Caret {
			get {
				return textArea.Caret;
			}
		}
		
		[Browsable(false)]
		public IDocument Document {
			get {
				if (motherTextEditorControl != null)
					return motherTextEditorControl.Document;
				return null;
			}
		}
		
		public ITextEditorProperties TextEditorProperties {
			get {
				if (motherTextEditorControl != null)
					return motherTextEditorControl.TextEditorProperties;
				return null;
			}
		}
		
		public VScrollBar VScrollBar {
			get {
				return vScrollBar;
			}
		}
		
		public HScrollBar HScrollBar {
			get {
				return hScrollBar;
			}
		}
		
		public bool DoHandleMousewheel {
			get {
				return doHandleMousewheel;
			}
			set {
				doHandleMousewheel = value;
			}
		}
		
		public TextAreaControl(TextEditorControl motherTextEditorControl)
		{
			this.motherTextEditorControl = motherTextEditorControl;
			
			this.textArea                = new TextArea(motherTextEditorControl, this);
			Controls.Add(textArea);
			
			vScrollBar.ValueChanged += new EventHandler(VScrollBarValueChanged);
			Controls.Add(this.vScrollBar);
			
			hScrollBar.ValueChanged += new EventHandler(HScrollBarValueChanged);
			Controls.Add(this.hScrollBar);
			ResizeRedraw = true;
			
			Document.TextContentChanged += DocumentTextContentChanged;
			Document.DocumentChanged += AdjustScrollBarsOnDocumentChange;
			Document.UpdateCommited  += DocumentUpdateCommitted;
		}
		
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (!disposed) {
					disposed = true;
					Document.TextContentChanged -= DocumentTextContentChanged;
					Document.DocumentChanged -= AdjustScrollBarsOnDocumentChange;
					Document.UpdateCommited  -= DocumentUpdateCommitted;
					motherTextEditorControl = null;
					if (vScrollBar != null) {
						vScrollBar.Dispose();
						vScrollBar = null;
					}
					if (hScrollBar != null) {
						hScrollBar.Dispose();
						hScrollBar = null;
					}
					if (hRuler != null) {
						hRuler.Dispose();
						hRuler = null;
					}
				}
			}
			base.Dispose(disposing);
		}

	    private void DocumentTextContentChanged(object sender, EventArgs e)
		{
			// after the text content is changed abruptly, we need to validate the
			// caret position - otherwise the caret position is invalid for a short amount
			// of time, which can break client code that expects that the caret position is always valid
			Caret.ValidateCaretPos();
		}
		
		protected override void OnResize(System.EventArgs e)
		{
			base.OnResize(e);
		    UpdateLayout();
		}

	    private bool adjustScrollBarsOnNextUpdate;
	    private Point scrollToPosOnNextUpdate;

	    private void AdjustScrollBarsOnDocumentChange(object sender, DocumentEventArgs e)
		{
			if (motherTextEditorControl.IsInUpdate == false) {
				AdjustScrollBarsClearCache();
				UpdateLayout();
			} else {
				adjustScrollBarsOnNextUpdate = true;
			}
		}

	    private void DocumentUpdateCommitted(object sender, EventArgs e)
		{
			if (motherTextEditorControl.IsInUpdate == false) {
				Caret.ValidateCaretPos();
				
				// AdjustScrollBarsOnCommittedUpdate
				if (!scrollToPosOnNextUpdate.IsEmpty) {
					ScrollTo(scrollToPosOnNextUpdate.Y, scrollToPosOnNextUpdate.X);
				}
				if (adjustScrollBarsOnNextUpdate) {
					AdjustScrollBarsClearCache();
					UpdateLayout();
				}
			}
		}

	    private int[] lineLengthCache;
	    private const int LineLengthCacheAdditionalSize = 100;

	    private void AdjustScrollBarsClearCache()
		{
			if (lineLengthCache != null) {
				if (lineLengthCache.Length < this.Document.TotalNumberOfLines + 2 * LineLengthCacheAdditionalSize) {
					lineLengthCache = null;
				} else {
					Array.Clear(lineLengthCache, 0, lineLengthCache.Length);
				}
			}
		}
		
		public void UpdateLayout()
		{
			if (this.textArea == null)
				return;

			adjustScrollBarsOnNextUpdate = false;

		    var view = textArea.TextView;

		    var currentVisibilites = GetScrollVisibilities(hScrollBar.Visible, vScrollBar.Visible);
		    var visited = new HashSet<ScrollVisibilities>();

		    // Start walking through any layout state transitions and see whether it ends up in a stable state.
		    var fromVisibilities = currentVisibilites;
		    while (true) {
		        if (!visited.Add(fromVisibilities))
                    // Returning to a visited state -- unstable, so make no change
		            break;
		        var bounds = Measure(fromVisibilities);
		        var to = ComputeScrollBarVisibilities(bounds.textArea.Size);
		        if (to.visibilities == fromVisibilities) {
                    // Layout is stable -- apply it
                    ApplyLayout(fromVisibilities, to.maxLength, bounds);
		            break;
		        }
		        fromVisibilities = to.visibilities;
		    }

		    return;

		    ScrollVisibilities GetScrollVisibilities(bool h, bool v) {
		        return (h ? ScrollVisibilities.H : ScrollVisibilities.None)
		               | (v ? ScrollVisibilities.V : ScrollVisibilities.None);
		    }

            (Rectangle hRule, Rectangle textControl, Rectangle textArea, Rectangle hScroll, Rectangle vScroll)
		    Measure(ScrollVisibilities scrollVisibilities) {
		        var v = scrollVisibilities.HasFlag(ScrollVisibilities.V);
		        var h = scrollVisibilities.HasFlag(ScrollVisibilities.H);
		        var vScrollSize = v ? SystemInformation.VerticalScrollBarArrowHeight : 0;
		        var hScrollSize = h ? SystemInformation.HorizontalScrollBarArrowWidth : 0;
		        var x0 = textArea.LeftMargins.Where(margin => margin.IsVisible).Sum(margin => margin.Size.Width);

		        var hRuleBounds = hRuler != null
		            ? new Rectangle(
		                0,
		                0,
		                Width - vScrollSize,
		                textArea.TextView.FontHeight)
		            : default;

		        var textControlBounds = new Rectangle(
		            0,
		            hRuleBounds.Bottom,
		            Width - vScrollSize,
		            Height - hRuleBounds.Bottom - hScrollSize);

		        var textAreaBounds = new Rectangle(
		            x0,
		            hRuleBounds.Bottom,
		            Width - x0 - vScrollSize,
		            Height - hRuleBounds.Bottom - hScrollSize);

		        var vScrollBounds = v
		            ? new Rectangle(
		                textAreaBounds.Right,
		                0,
		                SystemInformation.HorizontalScrollBarArrowWidth,
		                Height - hScrollSize)
		            : default;

		        var hScrollBounds = h
		            ? new Rectangle(
		                0,
		                textAreaBounds.Bottom,
		                Width - vScrollSize,
		                SystemInformation.VerticalScrollBarArrowHeight)
		            : default;

		        return (hRuleBounds, textControlBounds, textAreaBounds, hScrollBounds, vScrollBounds);
		    }

		    (ScrollVisibilities visibilities, int maxLength) ComputeScrollBarVisibilities(Size size) {
		        var visibleLineCount = 1 + size.Height/view.FontHeight;
		        var visibleColumnCount = size.Width/view.WideSpaceWidth - 1;

		        var firstLine = view.FirstVisibleLine;

		        int lastLine = Document.GetFirstLogicalLine(firstLine + visibleLineCount);
		        if (lastLine >= Document.TotalNumberOfLines)
		            lastLine = Document.TotalNumberOfLines - 1;

		        if (lineLengthCache == null || lineLengthCache.Length <= lastLine)
		            lineLengthCache = new int[lastLine + LineLengthCacheAdditionalSize];

		        int maxLength = 0;
		        for (int lineNumber = firstLine; lineNumber <= lastLine; lineNumber++) {
		            LineSegment lineSegment = Document.GetLineSegment(lineNumber);
		            if (Document.FoldingManager.IsLineVisible(lineNumber)) {
		                if (lineLengthCache[lineNumber] > 0) {
		                    maxLength = Math.Max(maxLength, lineLengthCache[lineNumber]);
		                } else {
		                    int visualLength = view.GetVisualColumnFast(lineSegment, lineSegment.Length);
		                    lineLengthCache[lineNumber] = Math.Max(1, visualLength);
		                    maxLength = Math.Max(maxLength, visualLength);
		                }
		            }
		        }

		        var vScrollBarVisible = vScrollBar.Value != 0 || textArea.Document.TotalNumberOfLines >= visibleLineCount;
		        var hScrollBarVisible = hScrollBar.Value != 0 || maxLength > visibleColumnCount;

                return (GetScrollVisibilities(hScrollBarVisible, vScrollBarVisible), maxLength);
		    }

		    void ApplyLayout(ScrollVisibilities scrollVisibilities, int maxColumn, (Rectangle hRule, Rectangle textControl, Rectangle textArea, Rectangle hScroll, Rectangle vScroll) bounds) {
		        var visibleColumnCount = bounds.textArea.Width/view.WideSpaceWidth - 1;

		        vScrollBar.Minimum = 0;
		        // number of visible lines in document (folding!)
		        vScrollBar.Maximum = textArea.MaxVScrollValue;
		        vScrollBar.LargeChange = Math.Max(0, bounds.textArea.Height);
		        vScrollBar.SmallChange = Math.Max(0, view.FontHeight);
		        vScrollBar.Visible = scrollVisibilities.HasFlag(ScrollVisibilities.V);
		        vScrollBar.Bounds = bounds.vScroll;

		        hScrollBar.Minimum = 0;
		        hScrollBar.Maximum = Math.Max(maxColumn, visibleColumnCount - 1);
		        hScrollBar.LargeChange = Math.Max(0, visibleColumnCount - 1);
		        hScrollBar.SmallChange = Math.Max(0, view.SpaceWidth);
		        hScrollBar.Visible = scrollVisibilities.HasFlag(ScrollVisibilities.H);
		        hScrollBar.Bounds = bounds.hScroll;

		        if (hRuler != null)
		            hRuler.Bounds = bounds.hRule;

		        textArea.Bounds = bounds.textControl;
		    }
		}

        [Flags]
	    private enum ScrollVisibilities
	    {
            None = 0,
            H = 1,
            V = 2
	    }

		public void OptionsChanged()
		{
			textArea.OptionsChanged();
			
			if (textArea.TextEditorProperties.ShowHorizontalRuler) {
				if (hRuler == null) {
					hRuler = new HRuler(textArea);
					Controls.Add(hRuler);
				    UpdateLayout();
				} else {
					hRuler.Invalidate();
				}
			} else {
				if (hRuler != null) {
					Controls.Remove(hRuler);
					hRuler.Dispose();
					hRuler = null;
				    UpdateLayout();
				}
			}
			
			UpdateLayout();
		}

	    private void VScrollBarValueChanged(object sender, EventArgs e)
		{
			textArea.VirtualTop = new Point(textArea.VirtualTop.X, vScrollBar.Value);
			textArea.Invalidate();
			UpdateLayout();
		}

	    private void HScrollBarValueChanged(object sender, EventArgs e)
		{
			textArea.VirtualTop = new Point(hScrollBar.Value * textArea.TextView.WideSpaceWidth, textArea.VirtualTop.Y);
			textArea.Invalidate();
		}

	    private Util.MouseWheelHandler mouseWheelHandler = new Util.MouseWheelHandler();
		
		public void HandleMouseWheel(MouseEventArgs e)
		{
			int scrollDistance = mouseWheelHandler.GetScrollAmount(e);
			if (scrollDistance == 0)
				return;
			if ((Control.ModifierKeys & Keys.Control) != 0 && TextEditorProperties.MouseWheelTextZoom) {
				if (scrollDistance > 0) {
					motherTextEditorControl.Font = new Font(motherTextEditorControl.Font.Name,
					                                        motherTextEditorControl.Font.Size + 1);
				} else {
					motherTextEditorControl.Font = new Font(motherTextEditorControl.Font.Name,
					                                        Math.Max(6, motherTextEditorControl.Font.Size - 1));
				}
			} else {
				if (TextEditorProperties.MouseWheelScrollDown)
					scrollDistance = -scrollDistance;
				int newValue = vScrollBar.Value + vScrollBar.SmallChange * scrollDistance;
				vScrollBar.Value = Math.Max(vScrollBar.Minimum, Math.Min(vScrollBar.Maximum - vScrollBar.LargeChange + 1, newValue));
			}
		}
		
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);
			if (DoHandleMousewheel) {
				HandleMouseWheel(e);
			}
		}
		
		public void ScrollToCaret()
		{
			ScrollTo(textArea.Caret.Line, textArea.Caret.Column);
		}
		
		public void ScrollTo(int line, int column)
		{
			if (motherTextEditorControl.IsInUpdate) {
				scrollToPosOnNextUpdate = new Point(column, line);
				return;
			} else {
				scrollToPosOnNextUpdate = Point.Empty;
			}
			
			ScrollTo(line);
			
			int curCharMin  = (int)(this.hScrollBar.Value - this.hScrollBar.Minimum);
			int curCharMax  = curCharMin + textArea.TextView.VisibleColumnCount;
			
			int pos = textArea.TextView.GetVisualColumn(line, column);
			
			if (textArea.TextView.VisibleColumnCount < 0) {
				hScrollBar.Value = 0;
			} else {
				if (pos < curCharMin) {
					hScrollBar.Value = (int)(Math.Max(0, pos - scrollMarginHeight));
				} else {
					if (pos > curCharMax) {
						hScrollBar.Value = (int)Math.Max(0, Math.Min(hScrollBar.Maximum, (pos - textArea.TextView.VisibleColumnCount + scrollMarginHeight)));
					}
				}
			}
		}

	    private int scrollMarginHeight  = 3;
		
		/// <summary>
		/// Ensure that <paramref name="line"/> is visible.
		/// </summary>
		public void ScrollTo(int line)
		{
			line = Math.Max(0, Math.Min(Document.TotalNumberOfLines - 1, line));
			line = Document.GetVisibleLine(line);
			int curLineMin = textArea.TextView.FirstPhysicalLine;
			if (textArea.TextView.LineHeightRemainder > 0) {
				curLineMin ++;
			}
			
			if (line - scrollMarginHeight + 3 < curLineMin) {
				this.vScrollBar.Value =  Math.Max(0, Math.Min(this.vScrollBar.Maximum, (line - scrollMarginHeight + 3) * textArea.TextView.FontHeight)) ;
				VScrollBarValueChanged(this, EventArgs.Empty);
			} else {
				int curLineMax = curLineMin + this.textArea.TextView.VisibleLineCount;
				if (line + scrollMarginHeight - 1 > curLineMax) {
					if (this.textArea.TextView.VisibleLineCount == 1) {
						this.vScrollBar.Value =  Math.Max(0, Math.Min(this.vScrollBar.Maximum, (line - scrollMarginHeight - 1) * textArea.TextView.FontHeight)) ;
					} else {
						this.vScrollBar.Value = Math.Min(this.vScrollBar.Maximum,
						                                 (line - this.textArea.TextView.VisibleLineCount + scrollMarginHeight - 1)* textArea.TextView.FontHeight) ;
					}
					VScrollBarValueChanged(this, EventArgs.Empty);
				}
			}
		}
		
		/// <summary>
		/// Scroll so that the specified line is centered.
		/// </summary>
		/// <param name="line">Line to center view on</param>
		/// <param name="treshold">If this action would cause scrolling by less than or equal to
		/// <paramref name="treshold"/> lines in any direction, don't scroll.
		/// Use -1 to always center the view.</param>
		public void CenterViewOn(int line, int treshold)
		{
			line = Math.Max(0, Math.Min(Document.TotalNumberOfLines - 1, line));
			// convert line to visible line:
			line = Document.GetVisibleLine(line);
			// subtract half the visible line count
			line -= textArea.TextView.VisibleLineCount / 2;
			
			int curLineMin = textArea.TextView.FirstPhysicalLine;
			if (textArea.TextView.LineHeightRemainder > 0) {
				curLineMin ++;
			}
			if (Math.Abs(curLineMin - line) > treshold) {
				// scroll:
				this.vScrollBar.Value =  Math.Max(0, Math.Min(this.vScrollBar.Maximum, (line - scrollMarginHeight + 3) * textArea.TextView.FontHeight)) ;
				VScrollBarValueChanged(this, EventArgs.Empty);
			}
		}
		
		public void JumpTo(int line)
		{
			line = Math.Max(0, Math.Min(line, Document.TotalNumberOfLines - 1));
			string text = Document.GetText(Document.GetLineSegment(line));
			JumpTo(line, text.Length - text.TrimStart().Length);
		}
		
		public void JumpTo(int line, int column)
		{
			textArea.Focus();
			textArea.SelectionManager.ClearSelection();
			textArea.Caret.Position = new TextLocation(column, line);
			textArea.SetDesiredColumn();
			ScrollToCaret();
		}
		
		public event MouseEventHandler ShowContextMenu;
		
		protected override void WndProc(ref Message m)
		{
			if (m.Msg == 0x007B) { // handle WM_CONTEXTMENU
				if (ShowContextMenu != null) {
					long lParam = m.LParam.ToInt64();
					int x = unchecked((short)(lParam & 0xffff));
					int y = unchecked((short)((lParam & 0xffff0000) >> 16));
					if (x == -1 && y == -1) {
						Point pos = Caret.ScreenPosition;
						ShowContextMenu(this, new MouseEventArgs(MouseButtons.None, 0, pos.X, pos.Y + textArea.TextView.FontHeight, 0));
					} else {
						Point pos = PointToClient(new Point(x, y));
						ShowContextMenu(this, new MouseEventArgs(MouseButtons.Right, 1, pos.X, pos.Y, 0));
					}
				}
			}
			base.WndProc(ref m);
		}
		
		protected override void OnEnter(EventArgs e)
		{
			// SD2-1072 - Make sure the caret line is valid if anyone
			// has handlers for the Enter event.
			Caret.ValidateCaretPos();
			base.OnEnter(e);
		}
	}
}
