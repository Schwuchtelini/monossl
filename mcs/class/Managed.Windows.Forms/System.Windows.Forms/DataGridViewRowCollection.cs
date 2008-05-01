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
// Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
//
// Author:
//	Pedro Martínez Juliá <pedromj@gmail.com>
//


#if NET_2_0

using System.ComponentModel;
using System.Collections;
using System.ComponentModel.Design.Serialization;

namespace System.Windows.Forms
	{
	[DesignerSerializerAttribute ("System.Windows.Forms.Design.DataGridViewRowCollectionCodeDomSerializer, " + Consts.AssemblySystem_Design,
				      "System.ComponentModel.Design.Serialization.CodeDomSerializer, " + Consts.AssemblySystem_Design)]
	[ListBindable (false)]
	public class DataGridViewRowCollection : IList, ICollection, IEnumerable
	{
		private ArrayList list;
		private DataGridView dataGridView;
		private bool raiseEvent = true;

		public DataGridViewRowCollection (DataGridView dataGridView)
		{
			this.dataGridView = dataGridView;
			list = new ArrayList ();
		}

		public int Count {
			get { return list.Count; }
		}

		int ICollection.Count {
			get { return Count; }
		}

		bool IList.IsFixedSize {
			get { return list.IsFixedSize; }
		}

		bool IList.IsReadOnly {
			get { return list.IsReadOnly; }
		}

		bool ICollection.IsSynchronized {
			get { return list.IsSynchronized; }
		}

		object IList.this [int index] {
			get {
				return this[index];
			}
			set { list[index] = value as DataGridViewRow; }
		}

		public DataGridViewRow this [int index] {
			get {
				// Accessing a System.Windows.Forms.DataGridViewRow with this indexer causes the row to become unshared. 
				// To keep the row shared, use the System.Windows.Forms.DataGridViewRowCollection.SharedRow method. 
				// For more information, see Best Practices for Scaling the Windows Forms DataGridView Control.
				DataGridViewRow row = (DataGridViewRow) list [index];
				if (row.Index == -1) {
					row = (DataGridViewRow) row.Clone ();
					row.SetIndex (index);
					list [index] = row;
				}
				return row;
			}
		}

		object ICollection.SyncRoot {
			get { return list.SyncRoot; }
		}

		public event CollectionChangeEventHandler CollectionChanged;

		[DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden)]
		public virtual int Add ()
		{
			return Add (dataGridView.RowTemplate.Clone () as DataGridViewRow);
		}

		int IList.Add (object value)
		{
			return Add (value as DataGridViewRow);
		}

		internal int AddInternal (DataGridViewRow dataGridViewRow, bool sharable)
		{
			if (dataGridView.Columns.Count == 0)
				throw new InvalidOperationException ("DataGridView has no columns.");
			
			int result;
			
			// 
			// Add the row just before the editing row (if there is an editing row).
			// 
			int editing_index = -1;
			if (DataGridView != null && DataGridView.EditingRow != null && DataGridView.EditingRow != dataGridViewRow) {
				editing_index = list.IndexOf (DataGridView.EditingRow);
				DataGridView.EditingRow.SetIndex (list.Count);
			}
			
			if (editing_index >= 0) {
				list.Insert (editing_index, dataGridViewRow);
				result = editing_index;
			} else {
				result = list.Add (dataGridViewRow);
			}
			
			if (sharable && CanBeShared (dataGridViewRow)) {
				dataGridViewRow.SetIndex (-1);
			} else {
				dataGridViewRow.SetIndex (result);
			}
			dataGridViewRow.SetDataGridView (dataGridView);

			for (int i = 0; i < dataGridViewRow.Cells.Count; i++) {
				dataGridViewRow.Cells [i].SetOwningColumn (dataGridView.Columns [i]);
			}

			OnCollectionChanged (new CollectionChangeEventArgs (CollectionChangeAction.Add, dataGridViewRow));
			if (raiseEvent) {
				DataGridView.OnRowsAddedInternal (new DataGridViewRowsAddedEventArgs (result, 1));
			}
			return result;
		}

		public virtual int Add (DataGridViewRow dataGridViewRow)
		{
			if (dataGridView.DataSource != null)
				throw new InvalidOperationException ("DataSource of DataGridView is not null.");
			return AddInternal (dataGridViewRow, true);
		}
		
		private bool CanBeShared (DataGridViewRow row)
		{
			// We don't currently support shared rows
			return false;
			
			//foreach (DataGridViewCell cell in row.Cells) {
			//        if (cell.Value != null)
			//                return false;
			//        if (cell.ToolTipText != string.Empty)
			//                return false;
			//        if (cell.ContextMenuStrip != null)
			//                return false;
			//}

			//return true;
		}
		

		[DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden)]
		public virtual int Add (int count)
		{
			if (count <= 0)
				throw new ArgumentOutOfRangeException("Count is less than or equeal to 0.");
			if (dataGridView.DataSource != null)
				throw new InvalidOperationException("DataSource of DataGridView is not null.");
			if (dataGridView.Columns.Count == 0)
				throw new InvalidOperationException("DataGridView has no columns.");

			raiseEvent = false;
			int result = 0;
			for (int i = 0; i < count; i++)
				result = Add (dataGridView.RowTemplate.Clone () as DataGridViewRow);
			DataGridView.OnRowsAdded (new DataGridViewRowsAddedEventArgs (result - count + 1, count));
			raiseEvent = true;
			return result;
		}

		[DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden)]
		public virtual int Add (params object[] values)
		{
			if (values == null)
				throw new ArgumentNullException("values is null.");
			if (dataGridView.VirtualMode)
				throw new InvalidOperationException("DataGridView is in virtual mode.");

			DataGridViewRow row = (DataGridViewRow)dataGridView.RowTemplateFull;
			int result = AddInternal (row, false);
			row.SetValues(values);
			return result;
		}

		public virtual int AddCopies (int indexSource, int count)
		{
			raiseEvent = false;
			int lastIndex = 0;
			for (int i = 0; i < count; i++)
				lastIndex = AddCopy(indexSource);
			DataGridView.OnRowsAdded (new DataGridViewRowsAddedEventArgs (lastIndex - count + 1, count));
			raiseEvent = true;
			return lastIndex;
		}

		public virtual int AddCopy (int indexSource)
		{
			return Add ((list [indexSource] as DataGridViewRow).Clone () as DataGridViewRow);
		}

		[DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden)]
		public virtual void AddRange (params DataGridViewRow [] dataGridViewRows)
		{
			if (dataGridView.DataSource != null)
				throw new InvalidOperationException ("DataSource of DataGridView is not null.");

			raiseEvent = false;
			int count = 0;
			int lastIndex = -1;
			foreach (DataGridViewRow row in dataGridViewRows) {
				lastIndex = Add (row);
				count++;
			}
			DataGridView.OnRowsAdded (new DataGridViewRowsAddedEventArgs (lastIndex - count + 1, count));
			raiseEvent = true;
		}

		public virtual void Clear ()
		{
			list.Clear();
		}

		bool IList.Contains (object value)
		{
			return Contains (value as DataGridViewRow);
		}

		public virtual bool Contains (DataGridViewRow dataGridViewRow)
		{
			return list.Contains (dataGridViewRow);
		}

		void ICollection.CopyTo (Array array, int index)
		{
			list.CopyTo (array, index);
		}

		public void CopyTo (DataGridViewRow[] array, int index)
		{
			list.CopyTo (array, index);
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return list.GetEnumerator ();
		}

		public int GetFirstRow (DataGridViewElementStates includeFilter)
		{
			for (int i = 0; i < list.Count; i++) {
				DataGridViewRow row = (DataGridViewRow) list [i];
				if ((row.State & includeFilter) != 0)
					return i;
			}
			return -1;
		}

		public int GetFirstRow (DataGridViewElementStates includeFilter, DataGridViewElementStates excludeFilter)
		{
			for (int i = 0; i < list.Count; i++) {
				DataGridViewRow row = (DataGridViewRow) list [i];
				if (((row.State & includeFilter) != 0) && ((row.State & excludeFilter) == 0))
					return i;
			}
			return -1;
		}

		public int GetLastRow (DataGridViewElementStates includeFilter)
		{
			for (int i = list.Count - 1; i >= 0; i--) {
				DataGridViewRow row = (DataGridViewRow) list [i];
				if ((row.State & includeFilter) != 0)
					return i;
			}
			return -1;
		}

		public int GetNextRow (int indexStart, DataGridViewElementStates includeFilter)
		{
			for (int i = indexStart + 1; i < list.Count; i++) {
				DataGridViewRow row = (DataGridViewRow) list [i];
				if ((row.State & includeFilter) != 0)
					return i;
			}
			return -1;
		}

		public int GetNextRow (int indexStart, DataGridViewElementStates includeFilter, DataGridViewElementStates excludeFilter)
		{
			for (int i = indexStart + 1; i < list.Count; i++) {
				DataGridViewRow row = (DataGridViewRow) list [i];
				if (((row.State & includeFilter) != 0) && ((row.State & excludeFilter) == 0))
					return i;
			}
			return -1;
		}

		public int GetPreviousRow (int indexStart, DataGridViewElementStates includeFilter)
		{
			for (int i = indexStart - 1; i >= 0; i--) {
				DataGridViewRow row = (DataGridViewRow) list [i];
				if ((row.State & includeFilter) != 0)
					return i;
			}
			return -1;
		}

		public int GetPreviousRow (int indexStart, DataGridViewElementStates includeFilter, DataGridViewElementStates excludeFilter)
		{
			for (int i = indexStart - 1; i >= 0; i--) {
				DataGridViewRow row = (DataGridViewRow) list [i];
				if (((row.State & includeFilter) != 0) && ((row.State & excludeFilter) == 0))
					return i;
			}
			return -1;
		}

		public int GetRowCount (DataGridViewElementStates includeFilter)
		{
			int result = 0;
			foreach (DataGridViewRow row in list)
				if ((row.State & includeFilter) != 0)
					result ++;
			return result;
		}

		public int GetRowsHeight (DataGridViewElementStates includeFilter)
		{
			int result = 0;
			foreach (DataGridViewRow row in list)
				if ((row.State & includeFilter) != 0)
					result += row.Height;
			return result;
		}

		public virtual DataGridViewElementStates GetRowState (int rowIndex)
		{
			return (list [rowIndex] as DataGridViewRow).State;
		}

		int IList.IndexOf (object value)
		{
			return IndexOf (value as DataGridViewRow);
		}

		public int IndexOf (DataGridViewRow dataGridViewRow)
		{
			return list.IndexOf (dataGridViewRow);
		}

		void IList.Insert (int index, object value)
		{
			Insert (index, value as DataGridViewRow);
		}

		public virtual void Insert (int rowIndex, DataGridViewRow dataGridViewRow)
		{
			dataGridViewRow.SetIndex (rowIndex);
			dataGridViewRow.SetDataGridView (dataGridView);
			list[rowIndex] = dataGridViewRow;
			OnCollectionChanged (new CollectionChangeEventArgs (CollectionChangeAction.Add, dataGridViewRow));
			if (raiseEvent)
				DataGridView.OnRowsAdded (new DataGridViewRowsAddedEventArgs (rowIndex, 1));
		}

		public virtual void Insert (int rowIndex, int count)
		{
			int index = rowIndex;
			raiseEvent = false;
			for (int i = 0; i < count; i++)
				Insert (index++, dataGridView.RowTemplate.Clone ());
			DataGridView.OnRowsAdded (new DataGridViewRowsAddedEventArgs (rowIndex, count));
			raiseEvent = true;
		}

		public virtual void Insert (int rowIndex, params object[] values)
		{
			if (values == null)
				throw new ArgumentNullException ("Values is null.");
			if (dataGridView.VirtualMode || dataGridView.DataSource != null)
				throw new InvalidOperationException ();
			DataGridViewRow row = new DataGridViewRow ();
			row.SetValues (values);
			Insert (rowIndex, row);
		}

		public virtual void InsertCopies (int indexSource, int indexDestination, int count)
		{
			raiseEvent = false;
			int index = indexDestination;
			for (int i = 0; i < count; i++)
				InsertCopy (indexSource, index++);
			DataGridView.OnRowsAdded (new DataGridViewRowsAddedEventArgs (indexDestination, count));
			raiseEvent = true;
		}

		public virtual void InsertCopy (int indexSource, int indexDestination)
		{
			Insert (indexDestination, (list [indexSource] as DataGridViewRow).Clone());
		}

		public virtual void InsertRange (int rowIndex, params DataGridViewRow [] dataGridViewRows)
		{
			raiseEvent = false;
			int index = rowIndex;
			int count = 0;
			foreach (DataGridViewRow row in dataGridViewRows) {
				Insert (index++, row);
				count++;
			}
			DataGridView.OnRowsAdded (new DataGridViewRowsAddedEventArgs (rowIndex, count));
			raiseEvent = true;
		}

		void IList.Remove (object value)
		{
			Remove (value as DataGridViewRow);
		}

		public virtual void Remove (DataGridViewRow dataGridViewRow)
		{
			list.Remove (dataGridViewRow);
			OnCollectionChanged (new CollectionChangeEventArgs (CollectionChangeAction.Remove, dataGridViewRow));
			DataGridView.OnRowsRemoved (new DataGridViewRowsRemovedEventArgs (dataGridViewRow.Index, 1));
		}

		public virtual void RemoveAt (int index)
		{
			DataGridViewRow row = this [index];
			list.RemoveAt (index);
			OnCollectionChanged (new CollectionChangeEventArgs (CollectionChangeAction.Remove, row));
			DataGridView.OnRowsRemoved (new DataGridViewRowsRemovedEventArgs (index, 1));
		}

		public DataGridViewRow SharedRow (int rowIndex)
		{
			return (DataGridViewRow) list [rowIndex];
		}

		internal int SharedRowIndexOf (DataGridViewRow row)
		{
			return list.IndexOf (row);
		}

		protected DataGridView DataGridView {
			get { return dataGridView; }
		}

		protected ArrayList List {
			get { return list; }
		}

		protected virtual void OnCollectionChanged (CollectionChangeEventArgs e)
		{
			if (CollectionChanged != null)
				CollectionChanged (this, e);
		}

		internal void InternalAdd (DataGridViewRow dataGridViewRow)
		{
			dataGridViewRow.SetDataGridView (dataGridView);
			
			// Add the row just before the editing row (if there is an editing row).
			if (DataGridView != null && DataGridView.EditingRow != null && DataGridView.NewRowIndex >= 0) {
				DataGridView.EditingRow.SetIndex (list.Count);
				list.Insert (DataGridView.NewRowIndex, dataGridViewRow);
			} else
				list.Add (dataGridViewRow);
				
			dataGridViewRow.SetIndex (list.IndexOf (dataGridViewRow));
		}

		internal ArrayList RowIndexSortedArrayList {
			get {
				ArrayList result = (ArrayList) list.Clone();
				result.Sort(new RowIndexComparator());
				return result;
			}
		}
		
		internal void Sort (IComparer comparer)
		{
			// Note: you will probably want to call
			// invalidate after using this.
			if (DataGridView != null && DataGridView.EditingRow != null)
				list.Sort (0, Count - 1, comparer);
			else
				list.Sort (comparer);
			
			for (int i = 0; i < list.Count; i++)
				(list[i] as DataGridViewRow).SetIndex (i);
		}

		private class RowIndexComparator : IComparer 
		{
			public int Compare (object o1, object o2)
			{
				DataGridViewRow row1 = (DataGridViewRow) o1;
				DataGridViewRow row2 = (DataGridViewRow) o2;
				if (row1.Index < row2.Index) {
					return -1;
				} else if (row1.Index > row2.Index) {
					return 1;
				} else {
					return 0;
				}
			}
		}
	}
}

#endif
