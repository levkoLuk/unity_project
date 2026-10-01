#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using Moderator.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace Moderator.Core
{
    [AddComponentMenu("")]
    public sealed class PrototypeSmokeProbe : MonoBehaviour
    {
        private bool ran;
        private void Update() { if (!ran && Application.isPlaying) { ran = true; StartCoroutine(Run()); } }

        private IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(.25f);
            try
            {
                var existingSocial = All<Transform>().FirstOrDefault(t => t.name == "SocialWindow");
                if (existingSocial == null || !existingSocial.gameObject.activeSelf) Click("SocialIcon");
                else existingSocial.GetComponent<WindowController>().Open();
                Require(Find("SocialWindow").activeSelf, "Social did not open");
                Require(Button("MinimizeDecor").interactable && Button("MaximizeDecor").interactable, "Window controls are not interactive");
                Require(All<Text>().Any(t => t.name == "URL" && t.gameObject.activeInHierarchy && t.text == "https://searcher.local"), "Browser did not start on Searcher");
                Require(All<Button>().Count(b => b.name.StartsWith("QueueUser_")) == 3, "Queue does not contain three users");
                Require(All<Transform>().Count(t => t.name == "PostHistoryItem") == 3, "Old post history was not created");
                Require(Find("PostHistoryScroll").GetComponent<ScrollRect>() != null, "Post history is not scrollable");

                Click("DeletePost");
                Require(ButtonLabel("DeletePost") == "CONFIRM DELETE", "Delete confirmation is missing");

                Click("QueueUser_2");
                Require(All<Button>().Any(b => b.name == "LinkedWord_map" && b.gameObject.activeInHierarchy), "Linked word map was not created");
                Click("LinkedWord_map");
                Require(Find("BrowserWindow").activeSelf, "Map link did not open Browser");
                Require(All<Text>().Any(t => t.name == "URL" && t.gameObject.activeInHierarchy && t.text.Contains("northbridge")), "Linked page URL is incorrect");
                Find("BrowserWindow").GetComponent<WindowController>().Close();

                Click("Identity");
                Require(Find("ProfileWindow").activeSelf, "Profile did not open");
                Require(ButtonLabel("ProfileBanUser") == "ЗАБЛОКИРОВАТЬ", "Ban button is missing from profile header");
                Require(Find("ProfilePostActions").transform.GetSiblingIndex() >= 0, "Post actions are missing below profile post");
                Require(All<Text>().Any(t => t.name == "ModeratorMode" && t.gameObject.activeInHierarchy && t.text.Contains("MOD VIEW")), "Moderator-view indicator is missing");
                Require(!All<Button>().Any(b => b.name == "EditProfileDecor" || b.name == "ProfileLike" || b.name == "ProfileRepost"), "User-mode actions are visible in moderator view");
                Click("ProfileComment");
                Require(All<Transform>().Any(t => t.name == "ProfileComments" && t.gameObject.activeInHierarchy), "Profile comments did not open");
                Click("ProfileComment");
                Require(!All<Button>().Any(b => b.name == "BlockUser" || b.name == "OpenReviewQueue"), "Extra profile header buttons are still visible");
                Require(!All<Transform>().Any(t => t.name == "CaseStatus" && t.gameObject.activeInHierarchy), "Case card is still visible in profile header");

                Click("ProfileBanUser");
                Require(Find("UserBlockedBanner").activeSelf, "Blocked-user banner did not appear");
                Require(All<Transform>().Where(t => t.name == "DeletedOverlay" && t.gameObject.activeInHierarchy).Count() == 3, "Ban did not remove all active posts");
                Click("ProfileBanUser");
                Require(!Find("UserBlockedBanner").activeSelf, "Unban did not clear blocked-user banner");
                Require(All<Transform>().Where(t => t.name == "DeletedOverlay" && t.gameObject.activeInHierarchy).Count() == 0, "Unban did not restore ban-removed posts");

                var profileRect = Find("ProfileWindow").GetComponent<RectTransform>();
                profileRect.anchoredPosition = new Vector2(100000, 100000);
                Find("ProfileWindow").GetComponent<WindowController>().SendMessage("ClampInside");
                Require(Mathf.Abs(profileRect.anchoredPosition.x) < 10000, "Window escaped desktop bounds");
                Find("ProfileWindow").GetComponent<WindowController>().Close();
                Click("QueueUser_0");
                Debug.Log("[ModeratorSmoke] PASS: scrollable post history, profile actions, header ban, ban removal, map link/article and window bounds.");
            }
            catch (Exception exception) { Debug.LogError("[ModeratorSmoke] FAIL: " + exception.Message, this); }
            Destroy(this);
        }

        private static T[] All<T>() where T : Component => FindObjectsByType<T>(FindObjectsInactive.Include);
        private static GameObject Find(string objectName) => All<Transform>().First(t => t.name == objectName).gameObject;
        private static void Click(string objectName) => All<Button>().First(b => b.name == objectName && b.gameObject.activeInHierarchy).onClick.Invoke();
        private static Button Button(string objectName) => All<Button>().First(b => b.name == objectName && b.gameObject.activeInHierarchy);
        private static string ButtonLabel(string objectName) => All<Button>().First(b => b.name == objectName && b.gameObject.activeInHierarchy).GetComponentInChildren<Text>().text;
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
#endif
