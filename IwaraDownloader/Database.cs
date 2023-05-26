using Microsoft.Data.Sqlite;
using System.Collections.Specialized;
using System.Text.Json;
using static Dawnlc.Module.Utils;
using System.Collections.ObjectModel;

namespace Dawnlc.Module
{
    public class Database
    {
        public ObservableCollection<Video> Videos { get; set; }
        private static SqliteConnection DBConnect { get; set; } = new SqliteConnection($"Data Source={Path.Combine(Env.Path, $"{Env.Name}.db")}");

        private static SqliteDataReader ExecuteReaderCommand(string command)
        {
            SqliteCommand DBCommand = DBConnect.CreateCommand();
            DBCommand.CommandText = command;
            return DBCommand.ExecuteReader();
        }
        private static int ExecuteCommand(string command)
        {
            SqliteCommand DBCommand = DBConnect.CreateCommand();
            DBCommand.CommandText = command;
            return DBCommand.ExecuteNonQuery();
        }
        public Database()
        {
            SqliteCommand DBCommand = DBConnect.CreateCommand();
            DBConnect.Open();
            ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS 'Videos' (
                    ID            TEXT PRIMARY KEY UNIQUE,
                    Source        TEXT NOT NULL,
                    Name          TEXT NOT NULL,
                    Alias         TEXT NOT NULL,
                    Author        TEXT NOT NULL,
                    Tag           TEXT NOT NULL,
                    Info          TEXT NOT NULL,
                    UploadTime    DATETIME NOT NULL,
                    DownloadTime  DATETIME NOT NULL,
                    Size          INTEGER NOT NULL,
                    Path          TEXT NOT NULL,
                    [Exists]      BOOLEAN NOT NULL,
                    Hash          BLOB NOT NULL
                );
            ");
            using (var reader = ExecuteReaderCommand(@"SELECT * FROM 'Videos'"))
            {
                Videos = reader.ConvertTo<Video>();
            }
            SqliteCommand INSERT_Videos = new($"INSERT INTO 'Videos' (ID,Source,Name,Alias,Author,Tag,Info,UploadTime,DownloadTime,Size,Path,[Exists],Hash) VALUES (@ID,@Source,@Name,@Alias,@Author,@Tag,@Info,@UploadTime,@DownloadTime,@Size,@Path,@Exists,@Hash)", DBConnect);
            SqliteCommand DELETE_Videos = new($"DELETE FROM 'Videos' WHERE ID=@ID", DBConnect);
            SqliteCommand UPDATE_Videos = new($"UPDATE 'Videos' SET ID=@ID,Source=@Source,Name=@Name,Alias=@Alias,Author=@Author,Tag=@Tag,Info=@Info,UploadTime=@UploadTime,DownloadTime=@DownloadTime,Size=@Size,Path=@Path,[Exists]=@Exists,Hash=@Hash WHERE ID=@ID", DBConnect);
            Videos.CollectionChanged += (sender, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (e.NewItems != null)
                        {
                            foreach (Video item in e.NewItems)
                            {
                                INSERT_Videos.Parameters.AddRange(new SqliteParameter[] 
                                {
                                    new("@ID",item.ID),
                                    new("@Source",item.Source),
                                    new("@Name",item.Name),
                                    new("@Alias",item.Alias),
                                    new("@Author",item.Author),
                                    new("@Tag",JsonSerializer.Serialize(item.Tag)),
                                    new("@Info",item.Info),
                                    new("@UploadTime",item.UploadTime),
                                    new("@DownloadTime",item.DownloadTime),
                                    new("@Size",item.Size),
                                    new("@Path",item.Path),
                                    new("@Exists",item.Exists),
                                    new("@Hash",item.Hash)
                                });
                                INSERT_Videos.ExecuteNonQuery();
                                INSERT_Videos.Parameters.Clear();
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        if (e.OldItems != null)
                        {
                            foreach (Video item in e.OldItems)
                            {
                                DELETE_Videos.Parameters.AddWithValue("@ID", item.ID);
                                DELETE_Videos.ExecuteNonQuery();
                                DELETE_Videos.Parameters.Clear();
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Reset:
                        if (e.NewItems != null)
                        {
                            foreach (Video item in e.NewItems)
                            {
                                UPDATE_Videos.Parameters.AddRange(new SqliteParameter[]
                                {
                                    new("@ID",item.ID),
                                    new("@Source",item.Source),
                                    new("@Name",item.Name),
                                    new("@Alias",item.Alias),
                                    new("@Author",item.Author),
                                    new("@Tag",JsonSerializer.Serialize(item.Tag)),
                                    new("@Info",item.Info),
                                    new("@UploadTime",item.UploadTime),
                                    new("@DownloadTime",item.DownloadTime),
                                    new("@Size",item.Size),
                                    new("@Path",item.Path),
                                    new("@Exists",item.Exists),
                                    new("@Hash",item.Hash)
                                });
                                UPDATE_Videos.ExecuteNonQuery();
                                UPDATE_Videos.Parameters.Clear();
                            }
                        }
                        break;
                    default:
                        break;
                }
            };
        }
    }
}
