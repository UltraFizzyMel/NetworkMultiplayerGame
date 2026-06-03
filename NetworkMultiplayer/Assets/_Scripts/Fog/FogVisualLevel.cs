// Tiny shared enum — in its own file so both Player and FogZoneManager
// can reference it without circular dependencies.
public enum FogVisualLevel { None, Warning, Death }