using Devmasters;
using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using HlidacStatu.Repositories.Cache;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories
{
    public static class BookmarkRepo
    {
        private static FusionCache _cache = new FusionCache(new FusionCacheOptions()
        {
            DefaultEntryOptions = CachingOptions.Simple1m,
            CacheName = "BookmarkRepoCache",
        });
        

        public static bool IsItemBookmarked(IBookmarkable item, string userId)
        {
            return IsItemBookmarked(Bookmark.ItemTypes.SingleItem, Bookmark.GetBookmarkId(item), userId);
        }

        public static bool IsItemBookmarked(Bookmark.ItemTypes type, string itemId, string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            var bookmarks = _cache.GetOrSet<Bookmark[]>($"bookmark:{userId}", _=>
            {
                using DbEntities db = new DbEntities();
                return db.Bookmarks.Where(b => b.UserId == userId).ToArray();
                    
                    
            });
            
            return bookmarks.Any(m => m.ItemType == (int)type && m.ItemId == itemId);

        }

        public static void SetBookmark(string name, string url, Bookmark.ItemTypes type, string itemId, string userId)
        {
            if (IsItemBookmarked(type, itemId, userId))
                return;

            Bookmark b = new Bookmark()
            {
                Name = TextUtil.ShortenText(name, 250),
                Folder = "",
                Created = DateTime.Now,
                Url = url,
                BookmarkType = type,
                ItemId = itemId,
                UserId = userId
            };
            Save(b);
            _cache.Remove($"bookmark:{userId}");
        }

        public static void DeleteBookmark(Bookmark.ItemTypes type, string itemId, string userId)
        {
            using DbEntities db = new DbEntities();
            var b = db.Bookmarks.FirstOrDefault(m =>
                m.ItemType == (int)type && m.ItemId == itemId && m.UserId == userId);
            if (b != null)
            {
                db.Bookmarks.Remove(b);
                db.SaveChanges();
            }

            _cache.Remove($"bookmark:{userId}");
        }

        private static void Save(Bookmark bookmark)
        {
            using DbEntities db = new DbEntities();
            if (bookmark.Id == default(Guid))
            {
                bookmark.Id = Guid.NewGuid();
                db.Bookmarks.Add(bookmark);
            }
            else
            {
                db.Bookmarks.Attach(bookmark);
                db.Entry(bookmark).State = EntityState.Modified;
            }

            db.SaveChanges();
        }
    }
}