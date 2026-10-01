using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moderator.Investigation
{
    public sealed class InvestigationState : MonoBehaviour
    {
        public readonly HashSet<string> Searches = new(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> VisitedPages = new();
        public readonly HashSet<string> Evidence = new();
        public readonly HashSet<string> BlockedUsers = new();
        public readonly HashSet<string> DeletedPosts = new();
        public readonly HashSet<string> UnlockedUsers = new() { "user.alex_92" };
        public readonly HashSet<string> CompletedQuests = new();

        public event Action Changed;

        public void RecordSearch(string query) { Searches.Add(Normalize(query)); Changed?.Invoke(); }
        public void VisitPage(string id) { VisitedPages.Add(id); Changed?.Invoke(); }
        public void FindEvidence(string id) { if (!string.IsNullOrEmpty(id)) Evidence.Add(id); Changed?.Invoke(); }
        public void SetBlocked(string id, bool blocked) { if (blocked) BlockedUsers.Add(id); else BlockedUsers.Remove(id); Changed?.Invoke(); }
        public void SetPostDeleted(string id, bool deleted) { if (deleted) DeletedPosts.Add(id); else DeletedPosts.Remove(id); Changed?.Invoke(); }
        public void UnlockUser(string id) { if (!string.IsNullOrEmpty(id)) UnlockedUsers.Add(id); Changed?.Invoke(); }
        public void CompleteQuest(string id) { CompletedQuests.Add(id); Changed?.Invoke(); }

        public static string Normalize(string value) => string.Join(" ", (value ?? "").Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
