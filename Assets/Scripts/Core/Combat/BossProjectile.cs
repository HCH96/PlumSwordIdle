namespace PlumSword.Core.Combat
{
    /// <summary>종남 장로가 왼쪽으로 날린 눈꽃 검기. 전투가 좌표와 적중을 관리한다.</summary>
    public sealed class BossProjectile
    {
        internal BossProjectile(double position, long damage) { Position = position; Damage = damage; }
        public double Position { get; internal set; }
        public long Damage { get; }
    }
}
