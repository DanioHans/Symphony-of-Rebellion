using UnityEngine;

public abstract class SpaceShooterBase : MonoBehaviour {
    public abstract void PlayerHit();
    public virtual void SmartBomb() {}
    public virtual void OnPickup(PowerUp.Kind k) {}
    public virtual void OnShieldHit(int hp) {}
}
