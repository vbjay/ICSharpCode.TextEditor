// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;

namespace ICSharpCode.TextEditor.Document
{
    public class HighlightInfo
    {
        public bool BlockSpanOn;
        public bool Span;
        public Span CurSpan;
        
        public HighlightInfo(Span curSpan, bool span, bool blockSpanOn)
        {
            CurSpan     = curSpan;
            Span        = span;
            BlockSpanOn = blockSpanOn;
        }
    }
}
