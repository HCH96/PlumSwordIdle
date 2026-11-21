using System;
using PlumSword.Core.Combat;
using PlumSword.Core.Realm;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Audio
{
    public sealed class SfxTriggers : IDisposable
    {
        private readonly SfxQueue queue;
        private readonly Battle battle;
        private readonly StageProgress stage;
        private readonly StatUpgrades upgrades;
        private readonly RealmProgress realm;
        private StagePhase previousPhase;
        private bool disposed;

        public SfxTriggers(SfxQueue queue, Battle battle, StageProgress stage, StatUpgrades upgrades, RealmProgress realm)
        {
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
            this.battle = battle ?? throw new ArgumentNullException(nameof(battle));
            this.stage = stage ?? throw new ArgumentNullException(nameof(stage));
            this.upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            this.realm = realm ?? throw new ArgumentNullException(nameof(realm));
            previousPhase = stage.Phase;
            battle.EnemyDamaged += OnEnemyDamaged;
            battle.SkillUsed += OnSkillUsed;
            battle.EnemyDefeated += OnEnemyDefeated;
            battle.EnemyAdded += OnEnemyAdded;
            stage.StageCleared += OnStageCleared;
            stage.StageFailed += OnStageFailed;
            stage.Changed += OnStageChanged;
            upgrades.Upgraded += OnUpgraded;
            realm.BrokeThrough += OnBrokeThrough;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            battle.EnemyDamaged -= OnEnemyDamaged;
            battle.SkillUsed -= OnSkillUsed;
            battle.EnemyDefeated -= OnEnemyDefeated;
            battle.EnemyAdded -= OnEnemyAdded;
            stage.StageCleared -= OnStageCleared;
            stage.StageFailed -= OnStageFailed;
            stage.Changed -= OnStageChanged;
            upgrades.Upgraded -= OnUpgraded;
            realm.BrokeThrough -= OnBrokeThrough;
        }

        private void OnEnemyDamaged(Enemy enemy) { queue.Request(SfxId.SwordSwing); queue.Request(SfxId.AttackHit); }
        private void OnSkillUsed() { queue.Request(SfxId.PlumSwordSkill); }
        private void OnEnemyDefeated(Enemy enemy) { if (enemy.Kind == EnemyKind.Normal) queue.Request(SfxId.EnemyDefeated); }
        private void OnEnemyAdded(Enemy enemy) { if (enemy.Kind == EnemyKind.Boss) queue.Request(SfxId.BossAppear); }
        private void OnStageCleared(int number) { queue.Request(SfxId.BossDefeated); }
        private void OnStageFailed(int number) { queue.Request(SfxId.PlayerDefeated); }
        private void OnStageChanged()
        {
            StagePhase current = stage.Phase;
            if (previousPhase != StagePhase.FadingOut && current == StagePhase.FadingOut && stage.Boss == null && !battle.IsDiscipleDefeated)
                queue.Request(SfxId.StageTransition);
            previousPhase = current;
        }
        private void OnUpgraded(StatType stat) { queue.Request(SfxId.UpgradeSuccess); }
        private void OnBrokeThrough(RealmLevel level) { queue.Request(SfxId.RealmBreakthrough); }
    }
}
