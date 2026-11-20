using NUnit.Framework;
using PlumSword.Core.Currency;
using PlumSword.Core.Realm;
using PlumSword.Game.Realm;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Tests.Realm
{
    public class RealmViewTests
    {
        [Test]
        public void Initialize_DisplaysArtAndUpdatesWithoutCreatingButton()
        {
            var root = new GameObject("realm test");
            try
            {
                var canvas = new GameObject("canvas", typeof(Canvas)).GetComponent<Canvas>();
                canvas.transform.SetParent(root.transform);
                var view = root.AddComponent<RealmView>();
                var wallet = new Wallet();
                var realm = new RealmProgress(wallet, 0, 29);
                view.Initialize(realm, wallet, canvas);
                var background = canvas.transform.Find("RealmStatus").GetComponent<Image>();
                var name = canvas.transform.Find("RealmName").GetComponent<Image>();
                Assert.That(background.sprite, Is.Not.Null);
                Assert.That(name.sprite.name, Is.EqualTo("name_third_rate"));
                Assert.That(canvas.GetComponentsInChildren<Button>(true), Is.Empty);
                realm.RecordKill();
                Assert.That(name.sprite.name, Is.EqualTo("name_second_rate"));
                Assert.That(wallet.Balance, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
