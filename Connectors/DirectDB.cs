﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog;

namespace HlidacStatu.Connectors
{
    public class DirectDB
    {
        private static readonly ILogger _logger = Log.ForContext<DirectDB>();
        
        public static string DefaultCnnStr = Devmasters.Config.GetWebConfigValue("OldEFSqlConnection");

        public static T GetValue<T>(string sql, CommandType type = CommandType.Text, IDataParameter[] param = null, string cnnString = null)
        {
            return GetList<T>(GetRowValues<T>, sql, type, param, cnnString).FirstOrDefault();
        }
        public static string ListResultToHtmlTable<T>(IEnumerable<T> data)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(1024);
            sb.AppendLine("<table>");
            foreach (var item in data)
            {
                sb.AppendLine("<tr>");
                foreach (var prop in item.GetType().GetProperties())
                {
                    sb.AppendLine($"<td>{prop.GetValue(item)}</td>");
                }
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
            return sb.ToString();
        }

        public static IEnumerable<T> GetList<T>(string sql, CommandType type = CommandType.Text, IDataParameter[] param = null, string cnnString = null)
        {
            return GetList<T>(GetRowValues<T>, sql, type, param, cnnString);
        }
        public static IEnumerable<Tuple<T1, T2>> GetList<T1, T2>(string sql, CommandType type = CommandType.Text, IDataParameter[] param = null, string cnnString = null)
        {
            return GetList<Tuple<T1, T2>>(GetRowValues<T1, T2>, sql, type, param, cnnString);
        }
        public static IEnumerable<Tuple<T1, T2, T3>> GetList<T1, T2, T3>(string sql, CommandType type = CommandType.Text, IDataParameter[] param = null, string cnnString = null)
        {
            return GetList<Tuple<T1, T2, T3>>(GetRowValues<T1, T2, T3>, sql, type, param, cnnString);
        }
        public static IEnumerable<Tuple<T1, T2, T3, T4>> GetList<T1, T2, T3, T4>(string sql, CommandType type = CommandType.Text, IDataParameter[] param = null, string cnnString = null)
        {
            return GetList<Tuple<T1, T2, T3, T4>>(GetRowValues<T1, T2, T3, T4>, sql, type, param, cnnString);
        }

        public static IEnumerable<Tuple<T1, T2, T3, T4, T5>> GetList<T1, T2, T3, T4, T5>(string sql, CommandType type = CommandType.Text, IDataParameter[] param = null, string cnnString = null)
        {
            return GetList<Tuple<T1, T2, T3, T4, T5>>(GetRowValues<T1, T2, T3, T4, T5>, sql, type, param, cnnString);
        }
        public static IEnumerable<Tuple<T1, T2, T3, T4, T5, T6>> GetList<T1, T2, T3, T4, T5, T6>(string sql, CommandType type = CommandType.Text, IDataParameter[] param = null, string cnnString = null)
        {
            return GetList<Tuple<T1, T2, T3, T4, T5, T6>>(GetRowValues<T1, T2, T3, T4, T5, T6>, sql, type, param, cnnString);
        }
        public static IEnumerable<Tuple<T1, T2, T3, T4, T5, T6, T7>> GetList<T1, T2, T3, T4, T5, T6, T7>(string sql, CommandType type = CommandType.Text, IDataParameter[] param = null, string cnnString = null)
        {
            return GetList<Tuple<T1, T2, T3, T4, T5, T6, T7>>(GetRowValues<T1, T2, T3, T4, T5, T6, T7>, sql, type, param, cnnString);
        }


        private static IEnumerable<T> GetList<T>(Func<DataRow, T> fillFnc, string sql, CommandType type, IDataParameter[] param, string cnnString)
        {
            //bool isStruct = typeof(T).IsValueType && !typeof(T).IsEnum;
            using (var p = new Devmasters.PersistLib())
            {
                var res = new List<T>();
                try
                {
                    var ds = p.ExecuteDataset(cnnString ?? DefaultCnnStr, type, sql, param);
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        res.Add(fillFnc(dr)); //GetRowValue<T>(dr, 0)
                    }
                    return res;
                }
                catch (Exception e)
                {
                    string paramStr = "";
                    if (param != null)
                    {
                        paramStr = string.Join(";", param.Select(s => $"{s.ParameterName}={s.Value}"));
                    }
                    _logger.Error(e, $"SQL error: {sql} Params:{paramStr}");
                    throw;
                }
            }
        }
        private static T1 GetRowValues<T1>(DataRow dr)
        {
            return GetRowValue<T1>(dr, 0);
        }
        private static Tuple<T1, T2> GetRowValues<T1, T2>(DataRow dr)
        {
            return new Tuple<T1, T2>(
                GetRowValue<T1>(dr, 0),
                GetRowValue<T2>(dr, 1)
                );
        }
        private static Tuple<T1, T2, T3> GetRowValues<T1, T2, T3>(DataRow dr)
        {
            return new Tuple<T1, T2, T3>(
                GetRowValue<T1>(dr, 0),
                GetRowValue<T2>(dr, 1),
                GetRowValue<T3>(dr, 2)
                );
        }
        private static Tuple<T1, T2, T3, T4> GetRowValues<T1, T2, T3, T4>(DataRow dr)
        {
            return new Tuple<T1, T2, T3, T4>(
                GetRowValue<T1>(dr, 0),
                GetRowValue<T2>(dr, 1),
                GetRowValue<T3>(dr, 2),
                GetRowValue<T4>(dr, 3)
                );
        }

        private static Tuple<T1, T2, T3, T4, T5> GetRowValues<T1, T2, T3, T4, T5>(DataRow dr)
        {
            return new Tuple<T1, T2, T3, T4, T5>(
                GetRowValue<T1>(dr, 0),
                GetRowValue<T2>(dr, 1),
                GetRowValue<T3>(dr, 2),
                GetRowValue<T4>(dr, 3),
                GetRowValue<T5>(dr, 4)
                );
        }

        private static Tuple<T1, T2, T3, T4, T5, T6> GetRowValues<T1, T2, T3, T4, T5, T6>(DataRow dr)
        {
            return new Tuple<T1, T2, T3, T4, T5, T6>(
                GetRowValue<T1>(dr, 0),
                GetRowValue<T2>(dr, 1),
                GetRowValue<T3>(dr, 2),
                GetRowValue<T4>(dr, 3),
                GetRowValue<T5>(dr, 4),
                GetRowValue<T6>(dr, 5)
                );
        }

        private static Tuple<T1, T2, T3, T4, T5, T6, T7> GetRowValues<T1, T2, T3, T4, T5, T6, T7>(DataRow dr)
        {
            return new Tuple<T1, T2, T3, T4, T5, T6, T7>(
                GetRowValue<T1>(dr, 0),
                GetRowValue<T2>(dr, 1),
                GetRowValue<T3>(dr, 2),
                GetRowValue<T4>(dr, 3),
                GetRowValue<T5>(dr, 4),
                GetRowValue<T6>(dr, 5),
                GetRowValue<T7>(dr, 6)
                );
        }

        private static T GetRowValue<T>(DataRow dr, int index)
        {
            var dval = dr[index];
            bool isDbNull = Devmasters.PersistLib.IsNull(dval);

            var baseType = Nullable.GetUnderlyingType(typeof(T));
            if (baseType != null)
            {
                if (isDbNull)
                    return default(T);
                else
                    return (T)Convert.ChangeType(dr[index], baseType);
            }
            else
            {
                if (isDbNull)
                    return default(T);
                else
                    return (T)dr[index];
            }
        }

        public static void NoResult(string sql, params IDataParameter[] param)
        {
            NoResult(sql, CommandType.Text, param);
        }

        public static void NoResult(string sql, CommandType type, IDataParameter[] param = null, string cnnString = null)
        {
            using (var p = new Devmasters.PersistLib())
            {
                try
                {
                    p.ExecuteNonQuery(cnnString ?? DefaultCnnStr, type, sql, param);

                }
                catch (Exception e)
                {
                    string paramStr = "";
                    if (param != null)
                    {
                        paramStr = string.Join(";", param.Select(s => $"{s.ParameterName}={s.Value}"));
                    }
                    _logger.Error(e, $"SQL error: {sql} Params:{paramStr}");
                    throw;
                }
            }
        }
        public static string GetRawSql(string text, params IDataParameter[] pars)
        {
            return GetRawSql(CommandType.Text, text, pars);

        }
            public static string GetRawSql(CommandType typ, string text, IDataParameter[] pars, string connString = null)
        {
            // vytahne spoj a inicializuje
            string _connStr = connString ?? DefaultCnnStr;
            var conn = new SqlConnection(_connStr);

            // nastaveni prikazu
            IDbCommand _comm = new SqlCommand();
            _comm.CommandTimeout = 120;
            _comm.CommandText = text;
            _comm.CommandType = typ;
            _comm.Connection = conn;

            // jestlize zadany parametry, pak pripoj
            AttachParameters(_comm, pars);

            string query = _comm.CommandText;

            foreach (SqlParameter p in _comm.Parameters)
            {
                query = Regex.Replace(query,$@"{Regex.Escape("@" + p.ParameterName)}\b",
                    ParameterValueForSQL(p), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                //query = query.Replace("@" + p.ParameterName, ParameterValueForSQL(p));
            }
            return query;
        }
        /// <summary>
        /// pridej parametry do prikazu
        /// </summary>
        /// <param name="comm">prikaz</param>
        /// <param name="pars">sada parametru</param>
        private static void AttachParameters(IDbCommand comm, IDataParameter[] pars)
        {
            // ohlidani vstupu
            if (comm == null)
                throw new ArgumentNullException("command");
            if (pars == null)
                return;

            // ve smycce pridej vsechny parametry
            foreach (IDataParameter p in pars)
            {
                // osetreni db. nuly
                if (p.Value == null)
                    p.Value = DBNull.Value;

                comm.Parameters.Add(p);
            }
        }

        private static String ParameterValueForSQL(SqlParameter sp)
        {
            String retval = "";
            if (Devmasters.PersistLib.IsNull(sp.Value))
                return "null";

            switch (sp.SqlDbType)
            {
                case SqlDbType.Char:
                case SqlDbType.NChar:
                case SqlDbType.NText:
                case SqlDbType.NVarChar:
                case SqlDbType.Text:
                case SqlDbType.Time:
                case SqlDbType.VarChar:
                case SqlDbType.Xml:
                    retval = "N'" + sp.Value.ToString().Replace("'", "''") + "'";
                    break;

                case SqlDbType.Date:
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                    retval = "'" + ((DateTime)sp.Value).ToString("yyy-MM-dd hh:mm:ss").Replace("'", "''") + "'";
                    break;


                case SqlDbType.Bit:
                    retval = ((bool)sp.Value) ? "1" : "0";
                    break;

                case SqlDbType.DateTimeOffset:
                default:
                    retval = sp.Value.ToString().Replace("N'", "''");
                    break;
            }

            return retval;
        }
    }
}
