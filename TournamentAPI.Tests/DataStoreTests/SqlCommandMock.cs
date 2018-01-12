using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TournamentAPI.Tests.DataStoreTests;

namespace TournamentAPI.Tests.DataStoreTests
{
    public class SqlCommandMock : IDbCommand
    {
        private int _nonQueryReturnValue;
        private IDataReader _idr;

        public SqlCommandMock(int nonQueryReturnValue, IDataReader idr, IDbConnection connection = null)
        {
            _nonQueryReturnValue = nonQueryReturnValue;
            _idr = idr;
            if (connection != null)
            {
                Connection = connection;
            }
            Parameters = new ParameterCollection();
        }

        public IDbConnection Connection { get; set; }
        public IDbTransaction Transaction { get; set; }
        public string CommandText { get; set; }
        public int CommandTimeout { get; set; }
        public CommandType CommandType { get; set; }

        public IDataParameterCollection Parameters {
            get;
            set;
        }

        public UpdateRowSource UpdatedRowSource { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public IDbDataParameter CreateParameter()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            
        }

        public int ExecuteNonQuery()
        {
            return _nonQueryReturnValue;
        }

        public IDataReader ExecuteReader()
        {
            return _idr;
        }

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            return _idr;
        }

        public object ExecuteScalar()
        {
            throw new NotImplementedException();
        }

        public void Prepare()
        {
            throw new NotImplementedException();
        }
    }
}
