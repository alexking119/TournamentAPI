using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TournamentAPI.Tests.DataStoreTests
{
    public class SqlConnectionMock : IDbConnection
    {
        private int _nonQueryReturnValue;
        private IDataReader _idr;
        private bool _failConnection;

        public SqlConnectionMock(int nonQueryReturnValue, IDataReader idr, bool failConnection = false)
        {
            _nonQueryReturnValue = nonQueryReturnValue;
            _idr = idr;
            _failConnection = failConnection;
        }

        public void Dispose()
        {
            this.State = ConnectionState.Closed;
        }

        public IDbTransaction BeginTransaction()
        {
            return null;
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            State = ConnectionState.Closed;
        }

        public void ChangeDatabase(string databaseName)
        {
            throw new NotImplementedException();
        }

        public IDbCommand CreateCommand()
        {
            IDbCommand command = new SqlCommandMock(_nonQueryReturnValue, _idr, this);
            return command;
        }

        public void Open()
        {
            if (_failConnection)
            {
                State = ConnectionState.Broken;
                throw new InvalidOperationException();
            }
            State = ConnectionState.Open;
        }

        public string ConnectionString { get; set; }
        public int ConnectionTimeout { get; }
        public string Database { get; }
        public ConnectionState State { get; private set; }
    }
}
