// ============================================================
// ResourceType.cs — todos los recursos del GDD de Greenfall
// ============================================================
// IMPORTANTE: este enum se usa en WorkAreaSO, CommunityManager,
// inventario, crafting, drops, etc. Es el vocabulario común.
// ============================================================

public enum ResourceTypeProfesion
{
    // Comida
    Food,
    RawFood,
    CookedRation,
    MutantCrop,

    // Materiales de construcción
    Wood,
    Stone,
    Metal,
    Scrap,

    // Materiales funcionales
    Components,
    Cloth,
    Rope,

    // Biología / naturaleza infectada
    BiomatterVegetal,
    BiomatterAnimal,

    // Semillas
    SeedNormal,
    SeedMutant,

    // Munición
    AmmoPistol,
    AmmoRifle,
    Ammo50Cal,
    AmmoShotgun,
    AmmoArrow,
    AmmoNonLethal,  // dardos / redes (sistema de captura del GDD)

    // Especiales
    MedicalSupplies,
    Fuel,
    SpecialObject,  // llave / item de lore
}
