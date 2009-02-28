/*
 *
 * NDbUnit
 * Copyright (C)2005
 * http://www.ndbunit.org
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 *
 */

using System;
using System.IO;
using System.Data;
using System.Collections.Specialized;

namespace NDbUnit.Core
{
	/// <summary>
	///	The database operation to perform.
	/// </summary>
	public enum DbOperationFlag
	{
		/// <summary>No operation.</summary>
		None, 
		/// <summary>Insert rows into a set of database tables.</summary>
		Insert, 
		/// <summary>Insert rows into a set of database tables.  Allow identity 
		/// inserts to occur.</summary>
		InsertIdentity, 
		/// <summary>Delete rows from a set of database tables.</summary>
		Delete, 
		/// <summary>Delete all rows from a set of database tables.</summary>
		DeleteAll, 
		/// <summary>Update rows in a set of database tables.</summary>
		Update, 
		/// <summary>Refresh rows in a set of database tables.  Rows that exist 
		/// in the database are updated.  Rows that don't exist are inserted.</summary>
		Refresh,
		/// <summary>Composite operation of DeleteAll and Insert.</summary>
		CleanInsert,	
		/// <summary>Composite operation of DeleteAll and InsertIdentity.</summary>
		CleanInsertIdentity
	}

	/// <summary>
	/// Represents a unit test database initialization operation.
	/// </summary>
	public interface INDbUnitTest
	{
		/// <summary>
		/// Performs a database operation.
		/// </summary>
		/// <param name="dbOperationFlag">The database operation to perform.</param>
		/// <exception cref="NDbUnitException" />
		void PerformDbOperation(DbOperationFlag dbOperationFlag);
		/// <summary>
		/// Read in an xml schema file whose schema represents a set of
		/// tables in a database.  This schema file is used to build the
		/// database modification commands that are used to update the 
		/// database.  This is the first function that must be called 
		/// prior to any other functions being called on the interface.  
		/// Otherwise, an exception will be thrown.
		/// </summary>
		/// <remarks>The xml schema file can be generated by dragging and 
		/// dropping tables from Visual Studio's server explorer into a 
		/// DataSet.xsd file.</remarks>
		/// <param name="xmlSchemaFile">The schema file.</param>
		/// <exception cref="ArgumentException" />
		void ReadXmlSchema(string xmlSchemaFile);
		/// <summary>
		/// Read in an xml schema whose schema represents a set of
		/// tables in a database.  This schema is used to build the
		/// database modification commands that are used to update the 
		/// database.  This is the first function that must be called 
		/// prior to any other functions being called on the interface.  
		/// Otherwise, an exception will be thrown.
		/// </summary>
		/// <param name="xmlSchema">The schema stream.</param>
		/// <exception cref="ArgumentException" />
		void ReadXmlSchema(Stream xmlSchema);
		/// <summary>
		/// Read in an xml file whose data should be conform to the schema 
		/// of the file specified in the call to <see cref="ReadXmlSchema" />.  
		/// If the data does not conform to the schema, it will be ignored.  
		/// This data is used to update the database.
		/// </summary>
		/// <param name="xmlFile">The xml file.</param>
		/// <exception cref="ArgumentException" />
		void ReadXml(string xmlFile);
		/// <summary>
		/// Read in an xml stream whose data should be conform to the schema 
		/// specified in the call to <see cref="ReadXmlSchema" />.  
		/// If the data does not conform to the schema, it will be ignored.  
		/// This data is used to update the database.
		/// </summary>
		/// <param name="xml">The xml stream.</param>
		/// <exception cref="ArgumentException" />
		void ReadXml(Stream xml);
		/// <summary>
		/// Gets a <see cref="DataSet" /> object that contains both the 
		/// internal schema information and data.
		/// </summary>
		/// <returns><see cref="DataSet" /></returns>
		/// <exception cref="NDbUnitException" />
		DataSet CopyDataSet();
		/// <summary>
		/// Gets a <see cref="DataSet" /> object that contains only the 
		/// internal schema information.
		/// </summary>
		/// <returns><see cref="DataSet" /></returns>
		/// <exception cref="NDbUnitException" />
		DataSet CopySchema();
		/// <summary>
		/// Gets a data set from the database tables.  Includes all
		/// table names in the xml schema.
		/// </summary>
		/// <exception cref="NDbUnitException" />
		DataSet GetDataSetFromDb();
		/// <summary>
		/// Gets a data set from the database tables.
		/// </summary>
		/// <param name="tableNames">The list of table names in the xml 
		/// schema to export. If null, then all table names in the xml 
		/// schema will be exported.</param>
		/// <exception cref="NDbUnitException" />
		DataSet GetDataSetFromDb(StringCollection tableNames);
		/// <summary>
		/// Gets or sets the beginning character or characters to use 
		/// when working with database objects (for example, tables or 
		/// columns) whose names contain characters such as spaces or 
		/// reserved tokens.
		/// </summary>
		string QuotePrefix
		{
			get;
			set;
		}
		/// <summary>
		/// Gets or sets the ending character or characters to use 
		/// when working with database objects (for example, tables or 
		/// columns) whose names contain characters such as spaces or 
		/// reserved tokens.
		/// </summary>
		string QuoteSuffix
		{
			get;
			set;
		}
	}

	public class OperationEventArgs : EventArgs
	{
		private IDbTransaction _dbTransaction = null;

		public OperationEventArgs()
		{
		}

		public IDbTransaction DbTransaction
		{
			get{ return _dbTransaction; }
			set{ _dbTransaction = value; }
		}
	}

	#region Public Delegates

	public delegate void PreOperationEvent(object sender, OperationEventArgs args);
	public delegate void PostOperationEvent(object sender, OperationEventArgs args);

	#endregion

	/// <summary>
	/// The base class implementation of all NDbUnit unit test data adapters.
	/// </summary>
	public abstract class NDbUnitTest : INDbUnitTest
	{
		#region Private Fields

		private DataSet _ds = null;
		private string _xmlSchemaFile = "";
		private string _xmlFile = "";
		private bool _initialized = false;

		#endregion

		#region Public Methods

		public NDbUnitTest()
		{
		}

		#endregion

		#region Public Events

		public event PreOperationEvent PreOperation;
		public event PostOperationEvent PostOperation;

		#endregion

		#region Public Interface Implementation

		public void ReadXmlSchema(Stream xmlSchema)
		{
			IDbCommandBuilder dbCommandBuilder = GetDbCommandBuilder();
			dbCommandBuilder.BuildCommands(xmlSchema);

			DataSet dsSchema = dbCommandBuilder.GetSchema();

			_ds = dsSchema.Clone();

			_initialized = true;
		}

		public void ReadXmlSchema(string xmlSchemaFile)
		{
			if (null == xmlSchemaFile || String.Empty == xmlSchemaFile)
			{
				throw new ArgumentException("Schema file cannot be null or empty", "xmlSchemaFile");
			}

			if (_xmlSchemaFile.ToLower() != xmlSchemaFile.ToLower())
			{
				Stream stream = null;
				try 
				{
					stream = new FileStream(xmlSchemaFile, System.IO.FileMode.Open,
						FileAccess.Read, FileShare.Read);
					ReadXmlSchema(stream);
				}
				finally
				{
					if(stream != null)
					{
						stream.Close();
					}
				}
				_xmlSchemaFile = xmlSchemaFile;
			}

			_initialized = true;
		}

		public void ReadXml(Stream xml)
		{
			_ds.Clear();
			_ds.ReadXml(xml);
		}

		public void ReadXml(string xmlFile)
		{
			if (null == xmlFile || String.Empty == xmlFile)
			{
				throw new ArgumentException("Xml file cannot be null or empty", "xmlFile");
			}

			if (_xmlFile.ToLower() != xmlFile.ToLower())
			{	
				Stream stream = null;
				try 
				{
					stream = new FileStream(xmlFile, System.IO.FileMode.Open);
					ReadXml(stream);
				}
				finally
				{
					if(stream != null) 
					{
						stream.Close();
					}
				}
				_xmlFile = xmlFile;
			}
		}

		public DataSet CopyDataSet()
		{
			checkInitialized();
			return _ds.Copy();
		}

		public DataSet CopySchema()
		{
			checkInitialized();
			return _ds.Clone();
		}

		public void PerformDbOperation(DbOperationFlag dbOperationFlag)
		{
			checkInitialized();

			if (dbOperationFlag == DbOperationFlag.None)
			{
				return;
			}

			IDbCommandBuilder dbCommandBuilder = GetDbCommandBuilder();
			IDbOperation dbOperation = GetDbOperation();

			IDbTransaction dbTransaction = null;
			IDbConnection dbConnection = dbCommandBuilder.Connection;
			try
			{
				dbConnection.Open();
				dbTransaction = dbConnection.BeginTransaction();

				OperationEventArgs args = new OperationEventArgs();
				args.DbTransaction = dbTransaction;

				if (null != PreOperation)
				{
					PreOperation(this, args);
				}

				switch(dbOperationFlag)
				{
					case DbOperationFlag.Insert:
					{
						dbOperation.Insert(_ds, dbCommandBuilder, dbTransaction);

						break;
					}
					case DbOperationFlag.InsertIdentity:
					{
						dbOperation.InsertIdentity(_ds, dbCommandBuilder, dbTransaction);

						break;
					}
					case DbOperationFlag.Delete:
					{
						dbOperation.Delete(_ds, dbCommandBuilder, dbTransaction);

						break;
					}
					case DbOperationFlag.DeleteAll:
					{
						dbOperation.DeleteAll(dbCommandBuilder, dbTransaction);

						break;
					}
					case DbOperationFlag.Refresh:
					{
						dbOperation.Refresh(_ds, dbCommandBuilder, dbTransaction);
	
						break;
					}
					case DbOperationFlag.Update:
					{
						dbOperation.Update(_ds, dbCommandBuilder, dbTransaction);

						break;
					}
					case DbOperationFlag.CleanInsert:
					{
						dbOperation.DeleteAll(dbCommandBuilder, dbTransaction);
						dbOperation.Insert(_ds, dbCommandBuilder, dbTransaction);

						break;
					}
					case DbOperationFlag.CleanInsertIdentity:
					{
						dbOperation.DeleteAll(dbCommandBuilder, dbTransaction);
						dbOperation.InsertIdentity(_ds, dbCommandBuilder, dbTransaction);

						break;
					}
				}

				if (null != PostOperation)
				{
					PostOperation(this, args);
				}

				dbTransaction.Commit();
			}
			catch(Exception e)
			{
				if (dbTransaction != null)
				{
					dbTransaction.Rollback();
				}

				throw(e);
			}
			finally
			{
				if (ConnectionState.Open == dbConnection.State)
				{
					dbConnection.Close();
				}
			}
		}
		
		public DataSet GetDataSetFromDb()
		{
			return GetDataSetFromDb(null);
		}

		public DataSet GetDataSetFromDb(StringCollection tableNames)
		{
			checkInitialized();

			IDbCommandBuilder dbCommandBuilder = GetDbCommandBuilder();
			if (null == tableNames)
			{
				tableNames = new StringCollection();
				foreach(DataTable dt in _ds.Tables)
				{
					tableNames.Add(dt.TableName);
				}
			}

			IDbConnection dbConnection = dbCommandBuilder.Connection;
			try
			{
				dbConnection.Open();
				DataSet dsToFill = _ds.Clone();
				foreach(string tableName in tableNames)
				{
					OnGetDataSetFromDb(tableName, ref dsToFill, dbConnection);
				}

				return dsToFill;
			}
			catch(Exception e)
			{
				throw(e);
			}
			finally
			{
				if (ConnectionState.Open == dbConnection.State)
				{
					dbConnection.Close();
				}
			}
		}

		public string QuotePrefix
		{
			get
			{
				IDbCommandBuilder dbCommandBuilder = GetDbCommandBuilder();
				return dbCommandBuilder.QuotePrefix;
			}

			set
			{
				IDbCommandBuilder dbCommandBuilder = GetDbCommandBuilder();
				dbCommandBuilder.QuotePrefix = value;
			}
		}

		public string QuoteSuffix
		{
			get
			{
				IDbCommandBuilder dbCommandBuilder = GetDbCommandBuilder();
				return dbCommandBuilder.QuoteSuffix;
			}

			set
			{
				IDbCommandBuilder dbCommandBuilder = GetDbCommandBuilder();
				dbCommandBuilder.QuoteSuffix = value;
			}
		}

		#endregion

		#region Protected Abstract Methods

		protected abstract IDbCommandBuilder GetDbCommandBuilder();
		protected abstract IDbOperation GetDbOperation();
		protected abstract void OnGetDataSetFromDb(string tableName, ref DataSet dsToFill, IDbConnection dbConnection);

		#endregion

		#region Private Methods

		private void checkInitialized()
		{
			if (!_initialized)
			{
				string message = "INDbUnitTest.ReadXmlSchema(string) or INDbUnitTest.ReadXmlSchema(Stream) must be called successfully";
				throw new NDbUnitException(message);
			}
		}

		#endregion
	}
}