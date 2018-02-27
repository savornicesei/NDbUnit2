/*
 * NDbUnit2
 * https://github.com/savornicesei/NDbUnit2
 * This source code is released under the Apache 2.0 License; see the accompanying license file.
 *
 */
using System;
using System.Data;
using Microsoft.Data.Sqlite;

namespace NDbUnit.Core.SqlLite
{
    public class SqlLiteDbUnitTest : NDbUnitTest<SqliteConnection>
    {
        public SqlLiteDbUnitTest(string connectionString)
            : base(connectionString)
        {
        }

        public SqlLiteDbUnitTest(SqliteConnection connection)
            : base(connection)
        {
        }

        protected override IDbDataAdapter CreateDataAdapter(IDbCommand command)
        {
            return new SQLiteDataAdapter((SqliteCommand)command);
        }

        protected override IDbCommandBuilder CreateDbCommandBuilder(DbConnectionManager<SqliteConnection> connectionManager)
        {
            return new SqlLiteDbCommandBuilder(connectionManager);
        }
        
        //protected override IDbCommandBuilder CreateDbCommandBuilder(IDbConnection connection)
        //{
        //    return new SqlLiteDbCommandBuilder(connection);
        //}

        //protected override IDbCommandBuilder CreateDbCommandBuilder(string connectionString)
        //{
        //    return new SqlLiteDbCommandBuilder(connectionString);
        //}

        protected override IDbOperation CreateDbOperation()
        {
            return new SqlLiteDbOperation();
        }

    }

    [Obsolete("Use SqlLiteDbUnitTest class in place of this.")]
    public class SqlLiteUnitTest : SqlLiteDbUnitTest
    {
        public SqlLiteUnitTest(string connectionString) : base(connectionString)
        {
        }

        public SqlLiteUnitTest(SqliteConnection connection) : base(connection)
        {
        }
    }
}
