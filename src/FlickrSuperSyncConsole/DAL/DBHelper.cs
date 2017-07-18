using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlickrSuperSyncConsole.DAL
{
    public class DBHelper
    {
        private static string dbFileName = "MyDatabase.sqlite";

        #region Public methods
        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(String.Concat("Data Source=", dbFileName, "; Version=3;"));
        }

        static DBHelper()
        {
            Initialize();    
        }
        #endregion

        private static void Initialize()
        {
            if (!File.Exists(dbFileName))
            {
                SQLiteConnection.CreateFile(dbFileName);
            }

            CreateTables();
        }

        private static void CreateTables()
        {
            using(var con = GetConnection()){

                con.Open();

                string sql = "create table IF NOT EXISTS session (key varchar(100), value varchar(100))";
                using (var command = new SQLiteCommand(sql, con))
                {
                    command.ExecuteNonQuery();
                }

                sql = "create table IF NOT EXISTS Photo (PhotoId varchar(20), PhotoSetID varchar(20), OriginalUrl varchar(200), Title varchar(100), DownloadDate DATETIME, Error varchar(2000))";
                using (var command = new SQLiteCommand(sql, con))
                {
                    command.ExecuteNonQuery();
                }

                sql = "create table IF NOT EXISTS Album (PhotoSetID varchar(20), Title varchar(100))";
                using (var command = new SQLiteCommand(sql, con))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
