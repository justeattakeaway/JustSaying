using System.Data.SqlClient;
using System.Reflection;

namespace JustEat.Simples.DataAccess.UnitTests
{
    public static class ExceptionHelpers
    {
        public static SqlException MakeSqlDeadlockException()
        {
            return MakeSqlExceptionInner(1205);
        }

        public static SqlException MakeSqlException()
        {
            return MakeSqlExceptionInner(1);
        }


        private static SqlException MakeSqlExceptionInner(int errorNumber)
        {

            var collection = typeof(SqlErrorCollection)
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0]
                .Invoke(null);

            var types = new[] { typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string), typeof(string), typeof(int) };

            var error = typeof(SqlError)
                .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, types, null)
                .Invoke(new object[] { errorNumber, (byte)2, (byte)3, "server name", "error message", "proc", 100 });

            typeof(SqlErrorCollection)
                .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(collection, new[] { error });


            var e = typeof(SqlException)
                        .GetMethod("CreateException", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(SqlErrorCollection), typeof(string) }, null)
                        .Invoke(null, new[] { collection, "7.0.0" }) as SqlException;

            return e;
        }
    }
}