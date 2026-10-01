#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using Moderator.Investigation;
using Moderator.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Moderator.Core
{
    [AddComponentMenu("")]
    public sealed class StarterInvestigationSmokeProbe : MonoBehaviour
    {
        public static bool RunOnStart;

        private IEnumerator Start()
        {
            if (!RunOnStart) yield break;
            RunOnStart = false;
            yield return null;
            Require(UIFactory.LoadSprite("Art/Story/AlexNorthbridgePhoto") != null, "Alex story photo was not loaded");
            Click("BrowserIcon");
            yield return null;
            try
            {
                Require(All<Text>().Any(t => t.name == "Brand" && t.text.Contains("SEARCHER")), "Search home did not open");
                FindAnyObjectByType<Moderator.Browser.BrowserApp>().OpenSearch("Станция Нортбридж");
            }
            catch (Exception exception) { Debug.LogError("[StarterInvestigationSmoke] FAIL: " + exception.Message, this); Destroy(this); yield break; }
            yield return null;
            Click("SearchResult_page.northbridge_portal");
            yield return null;
            try
            {
                Require(All<ScrollRect>().Any(s => s.name == "PageScroll"), "Portal is not scrollable");
                Require(All<Transform>().Count(t => t.name.StartsWith("News_")) >= 3, "Multiple news articles were not created");
                Require(All<Transform>().Any(t => t.name == "Comment"), "Attached news comments are missing");
                Require(All<Button>().Any(b => b.name.StartsWith("InlineLink_фотографа")), "Inline photographer link is missing");
                Require(All<Button>().Any(b => b.name == "CommentUser_nightwatcher"), "Clickable comment username is missing");
                Click("InlineLink_фотографа");
            }
            catch (Exception exception) { Debug.LogError("[StarterInvestigationSmoke] FAIL: " + exception.Message, this); Destroy(this); yield break; }
            yield return null;
            try
            {
                Require(All<Button>().Count(b => b.name.StartsWith("BrowserTab_") && b.gameObject.activeInHierarchy) == 2, "Second browser tab was not created");
            }
            catch (Exception exception) { Debug.LogError("[StarterInvestigationSmoke] FAIL: " + exception.Message, this); Destroy(this); yield break; }
            yield return null;
            try
            {
                Require(All<Text>().Any(t => t.name == "ArticleTitle" && t.text == "ФОТОГРАФ"), "Inline link did not open photographer page");
                Click("History");
            }
            catch (Exception exception) { Debug.LogError("[StarterInvestigationSmoke] FAIL: " + exception.Message, this); Destroy(this); yield break; }
            yield return null;
            try
            {
                Require(All<Button>().Any(b => b.name.StartsWith("HistoryPage_")), "Visual history is empty");
                Click("BrowserTab_0");
            }
            catch (Exception exception) { Debug.LogError("[StarterInvestigationSmoke] FAIL: " + exception.Message, this); Destroy(this); yield break; }
            yield return null;
            Click("CommentUser_nightwatcher");
            yield return null;
            try
            {
                Require(All<Transform>().Any(t => t.name == "ProfileWindow" && t.gameObject.activeInHierarchy), "Username did not open FaceNet profile");

                var state = FindAnyObjectByType<InvestigationState>();
                state.FindEvidence("evidence.police_order_read");
                state.SetPostDeleted("user.alex_92.post", true);
                state.FindEvidence("evidence.alex_near_northbridge");
                state.FindEvidence("evidence.station_closed");
                state.FindEvidence("evidence.photographer_seen");
                Debug.Log("[StarterInvestigationSmoke] PASS: scrollable portal, attached comments, inline links, tabs, history and social profile links.");
            }
            catch (Exception exception) { Debug.LogError("[StarterInvestigationSmoke] FAIL: " + exception.Message, this); Destroy(this); yield break; }

            yield return new WaitForSecondsRealtime(.7f);
            var investigation = FindAnyObjectByType<InvestigationState>();
            investigation.SetPostDeleted("user.nightwatcher.post", true);
            yield return new WaitForSecondsRealtime(.7f);
            investigation.SetPostDeleted("user.northbridge_news.post", true);
            investigation.FindEvidence("evidence.police_confirms_closure");
            investigation.FindEvidence("evidence.october_witness");
            yield return new WaitForSecondsRealtime(.7f);
            try
            {
                Require(investigation.CompletedQuests.Contains("quest.wrong_account"), "Quest chain did not complete");
                Debug.Log("[StarterInvestigationSmoke] PASS: all three investigations still complete in sequence.");
            }
            catch (Exception exception) { Debug.LogError("[StarterInvestigationSmoke] FAIL: " + exception.Message, this); }
            Destroy(this);
        }

        private static T[] All<T>() where T : Component => FindObjectsByType<T>(FindObjectsInactive.Include);
        private static void Click(string name) => All<Button>().First(b => b.name == name && b.gameObject.activeInHierarchy).onClick.Invoke();
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
#endif
