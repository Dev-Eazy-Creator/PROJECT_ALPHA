// Enum of every character attribute. Fixed identifiers — data assets reference these by name.
namespace ProjectAlpha
{
    public enum AttributeType
    {
        // Resource attributes (define/behave the depleting pools)
        MaxHealth,            // ceiling of the Health pool (Health pool itself is Part 3+)
        MaxStamina,           // ceiling of the Stamina pool (built this milestone)
        StaminaRegenRate,     // stamina points restored per second when not draining
        StaminaDrainSprint,   // stamina points drained per second while sprinting
        StaminaDrainClimb,    // stamina points drained per second while climbing

        // Offensive attributes (feed damage formulas — Part 3+)
        PhysicalAttack,       // physical damage output (Warrior, Berserker, Rogue, Strider)
        MagickAttack,         // magick damage output (Mage, Magic Knight)

        // Defensive attributes (feed damage-taken / stagger formulas — Part 3+)
        PhysicalDefense,      // reduces incoming physical damage
        MagickDefense,        // reduces incoming magick damage

        // Combat-feel attributes (feed stagger/knockdown logic — Part 3+). DD1 model.
        StaggerResistance,    // resistance to being staggered (small interrupt) when hit
        KnockdownResistance,  // resistance to being knocked down (heavy interrupt) when hit

        // Movement attribute (consumed this milestone)
        MoveSpeed             // final walk speed; PlayerMovement reads this instead of its raw field
    }
}
