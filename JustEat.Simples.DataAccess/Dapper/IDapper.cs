using System.Collections.Generic;
using System.Data;
using System;
using System.Diagnostics.CodeAnalysis;

namespace JE.Api.Order.Database
{
	public interface IDapper
	{
		IEnumerable<T> Query<T>(string sql, dynamic parameters = null);
       
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "An alternative implementation would be more complicated for a client to use.")]
        Tuple<IEnumerable<T1>, IEnumerable<T2>> QueryMultiple<T1, T2>(string sql, dynamic parameters);

        int Execute(string sql, dynamic parameters);

        int DeadlockRetryExecute(string sql, dynamic parameters, int retryTimes);
    }
}