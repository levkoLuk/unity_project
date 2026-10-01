using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moderator.SocialNetwork
{
    [Serializable]
    public sealed class UserData
    {
        public string id;
        public string username;
        public string displayName;
        public Sprite avatar;
        public string registrationDate;
        public string bio;
        public int reports;
        public int riskScore;
        public bool isBanned;
        public bool isBlocked;
        public bool isVerified;
        public List<PostData> posts = new();
    }
}
