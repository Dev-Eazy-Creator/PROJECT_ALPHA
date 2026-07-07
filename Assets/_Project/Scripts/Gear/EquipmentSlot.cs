// Fixed equipment slots. Do not reorder — save data references these by index.
namespace ProjectAlpha
{
    public enum EquipmentSlot
    {
        PrimaryWeapon,
        SecondaryWeapon,   // shield / off-hand; empty for two-handed weapons
        Head,
        Chest,
        Gloves,
        Legs,
        Boots,
        Cape,
        Ring,              // single rare accessory (skill-boost effect deferred)
        Amulet             // single accessory (effect TBD)
    }
}
