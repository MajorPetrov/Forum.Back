using System;

namespace Forum.Data
{
    public static class CacheKeys
    {
        public static string Forum { get { return "_Forum"; } }
        public static string Post { get { return "_Post"; } }
        public static string PinnedPosts { get { return "_PinnedPosts"; } }
        public static string PostsByPage { get { return "_PostsByPage"; } }
        public static string RepliesByPage { get { return "_RepliesByPage"; } }
        public static TimeSpan Expiration { get { return TimeSpan.FromMinutes(5); } }
    }
}