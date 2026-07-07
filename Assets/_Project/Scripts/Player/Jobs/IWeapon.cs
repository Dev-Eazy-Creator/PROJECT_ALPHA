// Contract all weapon scripts must implement. Enables the combat system (Part 2) to interact
// with any weapon generically without knowing its concrete type.
namespace ProjectAlpha
{
    public interface IWeapon
    {
        JobType WeaponJobType { get; }
        void OnEquip();
        void OnUnequip();
    }
}
