//
// MetadataRowWriter.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Generated by /CodeGen/cecil-gen.rb do not edit
// <%=Time.now%>
//
// (C) 2005 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace Mono.Cecil.Metadata {

	using System;
	using System.Collections;

	using Mono.Cecil.Binary;

	class MetadataRowWriter : BaseMetadataRowVisitor {

		MetadataRoot m_root;
		MemoryBinaryWriter m_binaryWriter;
		IDictionary m_ciCache;

		int m_blobHeapIdxSz;
		int m_stringsHeapIdxSz;
		int m_guidHeapIdxSz;

		public MetadataRowWriter (MetadataTableWriter mtwv)
		{
			m_binaryWriter = mtwv.GetWriter ();
			m_root = mtwv.GetMetadataRoot ();
			m_ciCache = new Hashtable ();
		}

		void WriteBlobPointer (uint pointer)
		{
			WriteByIndexSize (pointer, m_blobHeapIdxSz);
		}

		void WriteStringPointer (uint pointer)
		{
			WriteByIndexSize (pointer, m_stringsHeapIdxSz);
		}

		void WriteGuidPointer (uint pointer)
		{
			WriteByIndexSize (pointer, m_guidHeapIdxSz);
		}

		void WriteTablePointer (uint pointer, int rid)
		{
			WriteByIndexSize (pointer, GetNumberOfRows (rid) < (1 << 16) ? 2 : 4);
		}

		void WriteMetadataToken (MetadataToken token, CodedIndex ci)
		{
			WriteByIndexSize (Utilities.CompressMetadataToken (ci, token),
				Utilities.GetCodedIndexSize (
					ci, new Utilities.TableRowCounter (GetNumberOfRows), m_ciCache));
		}

		int GetNumberOfRows (int rid)
		{
			IMetadataTable t = m_root.Streams.TablesHeap [rid];
			if (t == null || t.Rows == null)
				return 0;
			return t.Rows.Count;
		}

		void WriteByIndexSize (uint value, int size)
		{
			if (size == 4)
				m_binaryWriter.Write (value);
			else if (size == 2)
				m_binaryWriter.Write ((ushort) value);
			else
				throw new MetadataFormatException ("Non valid size for indexing");
		}

<% $tables.each { |table|
		parameters = ""
		table.columns.each { |col|
			parameters += col.type
			parameters += " "
			parameters += col.field_name[1..col.field_name.length]
			parameters += ", " if (table.columns.last != col)
		}
%>		public <%=table.row_name%> Create<%=table.row_name%> (<%=parameters%>)
		{
			<%=table.row_name%> row = new <%=table.row_name%> ();
<% table.columns.each { |col| %>			row.<%=col.property_name%> = <%=col.field_name[1..col.field_name.length]%>;
<% } %>			return row;
		}

<% } %>		public override void VisitRowCollection (RowCollection coll)
		{
			m_blobHeapIdxSz = m_root.Streams.BlobHeap != null ?
				m_root.Streams.BlobHeap.IndexSize : 2;
			m_stringsHeapIdxSz = m_root.Streams.StringsHeap != null ?
				m_root.Streams.StringsHeap.IndexSize : 2;
			m_guidHeapIdxSz = m_root.Streams.GuidHeap != null ?
				m_root.Streams.GuidHeap.IndexSize : 2;
		}

<% $tables.each { |table| %>		public override void Visit<%=table.row_name%> (<%=table.row_name%> row)
		{
<% table.columns.each { |col|
 if (col.target.nil?)
%>			<%=col.write_binary("row", "m_binaryWriter")%>;
<% elsif (col.target == "BlobHeap")
%>			WriteBlobPointer (row.<%=col.property_name%>);
<% elsif (col.target == "StringsHeap")
%>			WriteStringPointer (row.<%=col.property_name%>);
<% elsif (col.target == "GuidHeap")
%>			WriteGuidPointer (row.<%=col.property_name%>);
<% elsif (col.type == "MetadataToken")
%>			WriteMetadataToken (row.<%=col.property_name%>, CodedIndex.<%=col.target%>);
<% else
%>			WriteTablePointer (row.<%=col.property_name%>, <%=col.target%>Table.RId);
<% end
}%>		}

<% } %>	}
}