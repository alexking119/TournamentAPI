using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TournamentAPI.Tests.DataStoreTests
{
    public class DataReaderMock : IDataReader
    {
        private List<List<Object>> _list = new List<List<object>>();
        private int _activeRecordIndex = 0;

        public DataReaderMock(params string[] fieldNames)
        {
            List<object> listOfFieldNames = new List<object>();

            foreach(string name in fieldNames)
            {
                listOfFieldNames.Add(name);
            }

            _list.Add(listOfFieldNames);
            FieldCount = fieldNames.Length;
        }

        public void Dispose()
        {
            
        }

        public string GetName(int i)
        {
            throw new NotImplementedException();
        }

        public string GetDataTypeName(int i)
        {
            throw new NotImplementedException();
        }

        public Type GetFieldType(int i)
        {
            throw new NotImplementedException();
        }

        public object GetValue(int i)
        {
            return _list[_activeRecordIndex][i];
        }

        public int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public int GetOrdinal(string name)
        {
            throw new NotImplementedException();
        }

        public bool GetBoolean(int i)
        {
            throw new NotImplementedException();
        }

        public byte GetByte(int i)
        {
            throw new NotImplementedException();
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            throw new NotImplementedException();
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public Guid GetGuid(int i)
        {
            throw new NotImplementedException();
        }

        public short GetInt16(int i)
        {
            throw new NotImplementedException();
        }

        public int GetInt32(int i)
        {
            return 0;
        }

        public long GetInt64(int i)
        {
            throw new NotImplementedException();
        }

        public float GetFloat(int i)
        {
            throw new NotImplementedException();
        }

        public double GetDouble(int i)
        {
            throw new NotImplementedException();
        }

        public string GetString(int i)
        {
            throw new NotImplementedException();
        }

        public decimal GetDecimal(int i)
        {
            throw new NotImplementedException();
        }

        public DateTime GetDateTime(int i)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        public bool IsDBNull(int i)
        {
            throw new NotImplementedException();
        }

        public int FieldCount { get; }

        object IDataRecord.this[int i]
        {
            get { throw new NotImplementedException(); }
        }

        object IDataRecord.this[string name]
        {
            get
            {
                int index = _list[0].IndexOf(name);
                if (index >= 0)
                {
                    return _list[_activeRecordIndex][index];
                }
                throw new ArgumentException("Element not found in field names");
            }
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public bool NextResult()
        {
            throw new NotImplementedException();
        }

        public bool Read()
        {
            return ++_activeRecordIndex < _list.Count;
        }

        public void AddRecord(List<object> recordValues)
        {
            _list.Add(recordValues);
        }

        public int Depth => _list.Count;

        public bool IsClosed { get; }
        public int RecordsAffected { get; }
    }
}
