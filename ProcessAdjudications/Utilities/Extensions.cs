using System;
using MySql.Data.MySqlClient;

namespace Adjudications.Utilities
{
    public static class Extensions
    {
        public static string GetStringValueOrEmpty(this MySqlParameter parameter)
        {
            return parameter.Value == DBNull.Value
                ? string.Empty
                : parameter.Value.ToString();
        }
    }
}