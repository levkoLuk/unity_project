using System;
using UnityEngine;

namespace Moderator.SocialNetwork
{
    [Serializable]
    public sealed class PostData
    {
        public string id;
        public string username;
        public string displayName;
        [TextArea] public string text;
        public Sprite image;
        public int likes;
        public int comments;
        public int shares;
        public string timestamp;
        public bool saved;
        public bool deleted;
        public bool likedByModerator;
        public bool repostedByModerator;
        public bool deletedByBan;
        public bool hasMapLink;
    }
}
