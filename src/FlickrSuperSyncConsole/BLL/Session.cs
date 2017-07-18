using FlickrSuperSyncConsole.DAL;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlickrSuperSyncConsole.BLL
{
    public static class Session
    {
        public static void setVal(string key, string val)
        {
            var value = getVal(key);

            using (var con = DBHelper.GetConnection())
            {
                con.Open();

                var sql = string.Concat("update session set value = '", val, "' where key = '", key, "'");
                if (String.IsNullOrEmpty(value))
                {
                    sql = string.Concat("insert into session (key, value) values ('", key, "','", val, "')");
                }
                
                using(var command = new SQLiteCommand(sql, con))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public static string getVal(string key)
        {
            string val = "";

            using (var con = DBHelper.GetConnection())
            {
                con.Open();

                using (var command = new SQLiteCommand(String.Concat("select * from session where key = '", key, "'"), con)) {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            val = reader["value"].ToString();
                        }
                    }
                }
            }

            return val;
        }
    }
}
