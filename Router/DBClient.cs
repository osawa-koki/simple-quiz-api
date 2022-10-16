using System.Data;
using System.Data.SqlClient;

namespace DBMod
{
	internal enum SQLMethod
	{
		Select,
		SelectAll,
		Execute,
	}

	internal class DBClient
	{
		private static string? connection_string;

		internal static void Init(string? _connection_string)
		{
			connection_string = _connection_string;
		}

		private string _sql = "";
		private readonly List<dynamic> _sqlParams = new(){};
		private readonly Dictionary<string, SqlDbType> _sqlParamsDataType = new(){};

		internal void Add(string sql)
		{
			_sql += $" {sql} ";
		}

		internal void AddParam(dynamic param)
		{
			_sqlParams.Add(param);
		}

		internal void SetDataType(string name, SqlDbType sqlDbType)
		{
			_sqlParamsDataType[name] = sqlDbType;
		}

		private void Reset()
		{
			_sql = "";
			_sqlParams.Clear();
			_sqlParamsDataType.Clear();
		}

		private List<Dictionary<string, object?>> Run(SQLMethod sqlmethod)
		{
			if (connection_string == null) Error("接続文字列が指定されていません。");

			using var connection = new SqlConnection(connection_string);
			connection.Open();

			using var command = connection.CreateCommand();
			command.CommandText = _sql;

			foreach (var paramType in _sqlParamsDataType)
			{
				command.Parameters.Add(new SqlParameter(paramType.Key, paramType.Value));
			}

			for (int i = 0; i < _sqlParams.Count; i++)
			{
				command.Parameters[i].Value = _sqlParams[i];
			}

			if (sqlmethod != SQLMethod.Execute)
			{
				List<Dictionary<string, object?>> answer = new(){};
				using (SqlDataReader reader = command.ExecuteReader())
				{
					Reset();
					if (sqlmethod == SQLMethod.Select)
					{
						if (!reader.Read()) return answer;

						Dictionary<string, object?> field = new(){};

						for (var j = 0; j < reader.FieldCount; j++)
						{
							var cell_value = reader.GetValue(j);
							field[reader.GetName(j)] = cell_value != DBNull.Value ? cell_value : null;
						}
						answer.Add(field);
						return answer;

					}
					else
					{
						while (reader.Read())
						{
							Dictionary<string, object?> field = new(){};

							for (var j = 0; j < reader.FieldCount; j++)
							{
							var cell_value = reader.GetValue(j);
							field[reader.GetName(j)] = cell_value != DBNull.Value ? cell_value : null;
							}
							answer.Add(field);
						}
					}
				}
				return answer;
			}
			else
			{
				command.ExecuteNonQuery();
				Reset();
				return new List<Dictionary<string, object?>>{};
			}
		}

		internal Dictionary<string, object?>? Select()
		{
			var result = Run(SQLMethod.Select);
			if (result.Count == 0) return null;
			return result[0];
		}

		internal List<Dictionary<string, object?>> SelectAll()
		{
			return Run(SQLMethod.SelectAll);
		}

		internal void Execute()
		{
			Run(SQLMethod.Execute);
			return;
		}


		private static void Error(string message)
		{
			Console.WriteLine(" ***** DB実行時エラー ***** ");
			Console.WriteLine(message);
			Console.WriteLine("");

			Environment.Exit(-1);
		}

	}

}


