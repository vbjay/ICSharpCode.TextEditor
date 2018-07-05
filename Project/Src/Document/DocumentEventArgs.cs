// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;

namespace ICSharpCode.TextEditor.Document
{
	/// <summary>
	/// This delegate is used for document events.
	/// </summary>
	public delegate void DocumentEventHandler(object sender, DocumentEventArgs e);
	
	/// <summary>
	/// This class contains more information on a document event
	/// </summary>
	public class DocumentEventArgs : EventArgs
	{
	    /// <returns>
		/// always a valid Document which is related to the Event.
		/// </returns>
		public IDocument Document { get; }

	    /// <returns>
		/// -1 if no offset was specified for this event
		/// </returns>
		public int Offset { get; }

	    /// <returns>
		/// null if no text was specified for this event
		/// </returns>
		public string Text { get; }

	    /// <returns>
		/// -1 if no length was specified for this event
		/// </returns>
		public int Length { get; }

	    /// <summary>
		/// Creates a new instance off <see cref="DocumentEventArgs"/>
		/// </summary>
		public DocumentEventArgs(IDocument document) : this(document, -1, -1, null)
		{
		}
		
		/// <summary>
		/// Creates a new instance off <see cref="DocumentEventArgs"/>
		/// </summary>
		public DocumentEventArgs(IDocument document, int offset) : this(document, offset, -1, null)
		{
		}
		
		/// <summary>
		/// Creates a new instance off <see cref="DocumentEventArgs"/>
		/// </summary>
		public DocumentEventArgs(IDocument document, int offset, int length) : this(document, offset, length, null)
		{
		}
		
		/// <summary>
		/// Creates a new instance off <see cref="DocumentEventArgs"/>
		/// </summary>
		public DocumentEventArgs(IDocument document, int offset, int length, string text)
		{
			this.Document = document;
			this.Offset   = offset;
			this.Length   = length;
			this.Text     = text;
		}
		public override string ToString()
		{
			return String.Format("[DocumentEventArgs: Document = {0}, Offset = {1}, Text = {2}, Length = {3}]",
			                     Document,
			                     Offset,
			                     Text,
			                     Length);
		}
	}
}
