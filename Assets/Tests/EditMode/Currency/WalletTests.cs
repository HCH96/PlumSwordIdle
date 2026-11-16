using System;
using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Currency;

namespace PlumSword.Core.Tests.Currency
{
    public class WalletTests
    {
        private static List<long> Record(Wallet wallet)
        {
            var received = new List<long>();
            wallet.BalanceChanged += value => received.Add(value);
            return received;
        }

        // 생성자

        [Test]
        public void New_NoArgument_BalanceIsZero()
        {
            Assert.That(new Wallet().Balance, Is.EqualTo(0));
        }

        [Test]
        public void New_WithInitialBalance_BalanceIsInitialBalance()
        {
            Assert.That(new Wallet(500).Balance, Is.EqualTo(500));
        }

        [Test]
        public void New_NegativeInitialBalance_BalanceIsZeroWithoutException()
        {
            Wallet wallet = null;
            Assert.DoesNotThrow(() => wallet = new Wallet(-5));
            Assert.That(wallet.Balance, Is.EqualTo(0));
        }

        // Add

        [Test]
        public void Add_TenTimesThenFifty_BalanceAccumulates()
        {
            var wallet = new Wallet();

            for (var i = 0; i < 10; i++)
            {
                wallet.Add(10);
            }

            Assert.That(wallet.Balance, Is.EqualTo(100));

            wallet.Add(50);

            Assert.That(wallet.Balance, Is.EqualTo(150));
        }

        [Test]
        public void Add_ThreeTimes_NotifiesThreeTimesInOrder()
        {
            var wallet = new Wallet();
            var received = Record(wallet);

            wallet.Add(10);
            wallet.Add(10);
            wallet.Add(10);

            Assert.That(received, Is.EqualTo(new long[] { 10, 20, 30 }));
        }

        [Test]
        public void Add_ReadBalanceInHandler_BalanceEqualsNotifiedValue()
        {
            var wallet = new Wallet();
            long notified = -1;
            long readInHandler = -1;
            wallet.BalanceChanged += value =>
            {
                notified = value;
                readInHandler = wallet.Balance;
            };

            wallet.Add(10);

            Assert.That(notified, Is.EqualTo(10));
            Assert.That(readInHandler, Is.EqualTo(10));
        }

        [Test]
        public void Add_Zero_BalanceUnchangedAndNoNotification()
        {
            var wallet = new Wallet(100);
            var received = Record(wallet);

            wallet.Add(0);

            Assert.That(wallet.Balance, Is.EqualTo(100));
            Assert.That(received, Is.Empty);
        }

        [Test]
        public void Add_Negative_ThrowsAndBalanceUnchangedAndNoNotification()
        {
            var wallet = new Wallet(100);
            var received = Record(wallet);

            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.Add(-1));

            Assert.That(wallet.Balance, Is.EqualTo(100));
            Assert.That(received, Is.Empty);
        }

        [Test]
        public void Add_BeyondMaxValue_ClampsToMaxValueAndNotifiesOnce()
        {
            var wallet = new Wallet(long.MaxValue - 5);
            var received = Record(wallet);

            Assert.DoesNotThrow(() => wallet.Add(10));

            Assert.That(wallet.Balance, Is.EqualTo(long.MaxValue));
            Assert.That(received, Is.EqualTo(new[] { long.MaxValue }));
        }

        [Test]
        public void Add_AtMaxValue_BalanceUnchangedAndNoNotification()
        {
            var wallet = new Wallet(long.MaxValue);
            var received = Record(wallet);

            wallet.Add(1);

            Assert.That(wallet.Balance, Is.EqualTo(long.MaxValue));
            Assert.That(received, Is.Empty);
        }

        [Test]
        public void Add_NoSubscriber_DoesNotThrow()
        {
            var wallet = new Wallet();

            Assert.DoesNotThrow(() => wallet.Add(10));
            Assert.That(wallet.Balance, Is.EqualTo(10));
        }

        // TrySpend

        [Test]
        public void TrySpend_LessThanBalance_SucceedsAndNotifiesNewBalance()
        {
            var wallet = new Wallet(150);
            var received = Record(wallet);

            var result = wallet.TrySpend(20);

            Assert.That(result, Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(130));
            Assert.That(received, Is.EqualTo(new long[] { 130 }));
        }

        [Test]
        public void TrySpend_MoreThanBalance_FailsAndBalanceUnchangedAndNoNotification()
        {
            var wallet = new Wallet(15);
            var received = Record(wallet);

            var result = wallet.TrySpend(20);

            Assert.That(result, Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(15));
            Assert.That(received, Is.Empty);
        }

        [Test]
        public void TrySpend_EqualToBalance_SucceedsAndBalanceIsZeroAndNotifiesZero()
        {
            var wallet = new Wallet(20);
            var received = Record(wallet);

            var result = wallet.TrySpend(20);

            Assert.That(result, Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(0));
            Assert.That(received, Is.EqualTo(new long[] { 0 }));
        }

        [Test]
        public void TrySpend_Zero_SucceedsAndBalanceUnchangedAndNoNotification()
        {
            var wallet = new Wallet(100);
            var received = Record(wallet);

            var result = wallet.TrySpend(0);

            Assert.That(result, Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(100));
            Assert.That(received, Is.Empty);
        }

        [Test]
        public void TrySpend_Negative_ThrowsAndBalanceUnchangedAndNoNotification()
        {
            var wallet = new Wallet(100);
            var received = Record(wallet);

            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.TrySpend(-1));

            Assert.That(wallet.Balance, Is.EqualTo(100));
            Assert.That(received, Is.Empty);
        }

        [Test]
        public void TrySpend_TwiceWithBalanceForOnce_SecondFails()
        {
            var wallet = new Wallet(30);

            var first = wallet.TrySpend(20);
            var second = wallet.TrySpend(20);

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(10));
        }

        // CanSpend

        [Test]
        public void CanSpend_VariousAmounts_ReturnsWhetherAffordableWithoutChange()
        {
            var wallet = new Wallet(20);
            var received = Record(wallet);

            Assert.That(wallet.CanSpend(20), Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(20));

            Assert.That(wallet.CanSpend(21), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(20));

            Assert.That(wallet.CanSpend(0), Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(20));

            Assert.That(received, Is.Empty);
        }

        [Test]
        public void CanSpend_Negative_ThrowsAndBalanceUnchanged()
        {
            var wallet = new Wallet(20);

            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.CanSpend(-1));

            Assert.That(wallet.Balance, Is.EqualTo(20));
        }
    }
}
