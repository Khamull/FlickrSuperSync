using FlickrNet;
using FlickrSuperSyncConsole.BLL;
using FlickrSuperSyncConsole.DAL;
using FlickrSuperSyncConsole.Entities;
using FlickrSuperSyncConsole.Log;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FlickrSuperSyncConsole
{
    class Program
    {
        private static Flickr Flickr;
        private static String TargetFolder = @"C:\Flickr Photos";
        private static List<string> albums;
        private static bool Reset;

        static void Main(string[] args)
        {
            ParseArguments(args);

            try
            {
                if (!Reset && PhotosOnQueue() > 0)
                {
                    DownloadFromQueue();
                }
                else
                {
                    if (Reset)
                    {
                        ResetQueue();
                    }

                    Connect();

                    if (albums== null)
                    {
                        QueueAllPhotos();
                    }
                    else
                    {
                        var flickrAlbums = Flickr.PhotosetsGetList();

                        var filteredFlickrAlbums = flickrAlbums.Where(a => albums.Contains(a.Title));

                        foreach (var flickrAlbum in filteredFlickrAlbums)
                        {
                            QueueAlbum(flickrAlbum.PhotosetId, flickrAlbum.Title);
                        }
                    }

                    DownloadFromQueue();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            Console.ReadKey();
        }

        private static void ResetQueue()
        {
            using (var con = DBHelper.GetConnection())
            {
                con.Open();

                var sql = String.Concat("delete from Photo");
                using (var command = new SQLiteCommand(sql, con))
                {
                    command.ExecuteNonQuery();
                }

                sql = String.Concat("delete from Album");
                using (var command = new SQLiteCommand(sql, con))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void ParseArguments(string[] args)
        {
            if (args.Length < 1)
            {
                throw new ArgumentException("Missing argunemts.");
            }

            TargetFolder = args[0];

            if (args.Length > 1 && !string.IsNullOrEmpty(args[1]))
            {
                albums = args[1].Split(',').ToList();
            }

            if (args.Length > 2)
            {
                switch (args[2])
                {
                    case "-R":
                        Reset = true;
                        break;
                    default:
                        throw new ArgumentException("Invalid argument.");
                }
            }
        }

        private static void Connect()
        {
            Flickr = new Flickr("33a5fb0c8ad6bc796cd18aac473cc8ba", "da789b1efc2a786b");

            var authToken = Session.getVal("AuthToken");
            var authSecret = Session.getVal("AuthSecret");

            if (string.IsNullOrEmpty(authToken))
            {
                OAuthRequestToken requestToken = Flickr.OAuthGetRequestToken("oob");
                string url = Flickr.OAuthCalculateAuthorizationUrl(requestToken.Token, AuthLevel.Write);

                Process.Start(url);

                Console.WriteLine("Plese enter the authorization key");
                var authorizationKey = Console.ReadLine();

                if (string.IsNullOrEmpty(authorizationKey))
                {
                    throw new Exception("Authorization key not found");
                }

                var accessToken = Flickr.OAuthGetAccessToken(requestToken, authorizationKey);

                authToken = accessToken.Token;
                authSecret = accessToken.TokenSecret;

                Session.setVal("AuthToken", authToken);
                Session.setVal("AuthSecret", authSecret);
            }

            Flickr.OAuthAccessTokenSecret = authSecret;
            Flickr.OAuthAccessToken = authToken;
        }

        private static void QueueAllPhotos()
        {
            QueueAlbum(null, null);
        }

        private static void QueueAlbum(string photoSetID, string title)
        {
            int page = 0;
            string sql = "";
            List<FlickrNet.Photo> finalPhotos = new List<FlickrNet.Photo>();

            if (!String.IsNullOrEmpty(photoSetID))
            {
                using (var con = DBHelper.GetConnection())
                {
                    sql = string.Format("insert into Album (PhotoSetID,Title) Values('{0}', '{1}')");

                    using (var command = new SQLiteCommand(sql, con))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }

            var photos = GetPhotos(page, photoSetID);
            finalPhotos.AddRange(photos);

            while (photos.Count == 500)
            {
                Logger.Info(String.Concat("Getting photos info, page:", page));

                page++;
                photos = GetPhotos(page, photoSetID);

                finalPhotos.AddRange(photos);
            }

            int checkPointValue = 1000;
            int count = 1;
            using (var con = DBHelper.GetConnection())
            {
                con.Open();

                foreach (var photo in finalPhotos)
                {
                    if (count % checkPointValue == 0)
                    {
                        Logger.Info(String.Format("Queuwing photos, {0} of {1}", count, finalPhotos.Count));
                    }

                    sql = String.Format(
                        "insert into Photo (photoSetID,PhotoId,OriginalUrl,Title) Values ('{0}','{1}','{2}','{3}')",
                        photoSetID ?? "", photo.PhotoId, photo.OriginalUrl, photo.Title);

                    using (var command = new SQLiteCommand(sql, con))
                    {
                        command.ExecuteNonQuery();
                    }

                    count++;
                }
            }
        }

        private static int PhotosOnQueue()
        {
            int result = 0;

            var sql = String.Concat("select count(1) total from Photo");

            using (var con = DBHelper.GetConnection())
            {
                con.Open();

                using (var command = new SQLiteCommand(sql, con))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            result = Convert.ToInt32(reader["total"]);
                        }
                    }
                }
            }

            return result;
        }

        private static List<Entities.Photo> GetPenddingPhotosOnQueue()
        {
            var penddingPhotos = new List<Entities.Photo>();

            var sql = String.Concat("select * from Photo where DownloadDate is null");

            using (var con = DBHelper.GetConnection())
            {
                con.Open();

                using (var command = new SQLiteCommand(sql, con))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            penddingPhotos.Add(PhotoFromHeader(reader));
                        }
                    }
                }
            }

            return penddingPhotos;
        }

        private static Entities.Photo PhotoFromHeader(SQLiteDataReader reader)
        {
            var photo = new Entities.Photo()
            {
                PhotoId = reader["PhotoId"].ToString(),
                Title = reader["Title"].ToString(),
                OriginalUrl = reader["OriginalUrl"].ToString(),
                DownloadDate = string.IsNullOrEmpty(reader["DownloadDate"].ToString())? (DateTime?)null : Convert.ToDateTime(reader["DownloadDate"]),
                Error = reader["Error"].ToString()
            };

            return photo;
        }

        private static void DownloadFromQueue()
        {
            int total = 0;
            int current = 0;

            total = PhotosOnQueue();
            var queuedPhotos = GetPenddingPhotosOnQueue();

            current = (total - queuedPhotos.Count()) +1;

            foreach(var queued in queuedPhotos)
            {
                Logger.Info(String.Format("[{0} of {1}] {2}", current, total, queued.Title));

                try
                {
                    DownloadPhoto(queued.OriginalUrl, queued.PhotoId, queued.Title);
                    SetQueueAsDownloaded(queued);
                }
                catch(Exception e)
                {
                    var message = String.Format("Error downloading photo. PhotoID: {0}, Title: {1}, URL: {2}", queued.PhotoId, queued.Title, queued.OriginalUrl);
                    Logger.Error(message, e);
                }

                current++;
            }
        }

        private static void SetQueueAsDownloaded(Entities.Photo queued)
        {
            var sql = String.Format("update Photo set DownloadDate = '{0}' where PhotoId = '{1}'", DateTime.Now, queued.PhotoId);

            using (var con = DBHelper.GetConnection())
            {
                con.Open();

                using (var command = new SQLiteCommand(sql, con))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void DownloadPhoto(string originalUrl, string photoId, string title)
        {
            var fileName = string.Format("{0}_{1}", photoId, title);

            var titleExtension = Path.GetExtension(title);
            if (string.IsNullOrEmpty(titleExtension) || titleExtension.Length > 3)
            {
                var extension = Path.GetExtension(originalUrl);
                fileName += extension;
            }

            using (var client = new WebClient())
            {
                var targetFile = string.Format(@"{0}\{1}", TargetFolder, fileName);
                client.DownloadFile(originalUrl, targetFile);
            }
        }

        private static PagedPhotoCollection GetPhotos(int page, string photoSetID = null)
        {
            int itemsPerPage = 500;
            int maxTries = 10;
            int tries = 0;

            PagedPhotoCollection photos = null;
            Exception lastError = null;

            while (tries <= maxTries)
            {
                try
                {
                    if (string.IsNullOrEmpty(photoSetID))
                    {
                        photos = Flickr.PeopleGetPhotos(page: page, perPage: itemsPerPage, extras: PhotoSearchExtras.OriginalFormat);
                    }
                    else
                    {
                        photos = Flickr.PhotosetsGetPhotos(photosetId: photoSetID, page: page, perPage: itemsPerPage, extras: PhotoSearchExtras.OriginalFormat);
                    }

                    break;
                }
                catch(Exception e)
                {
                    lastError = e;
                    tries++;
                }
            }

            if(tries> maxTries)
            {
                throw lastError;
            }

            return photos;
        }
    }
}
