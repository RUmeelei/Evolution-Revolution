namespace ER.World.Population
{
    public enum Gender : byte
    {
        Male = 0,
        Female = 1
    }

    public enum PopLifeStage : byte
    {
        Infant = 0,
        Child = 1,
        Youth = 2,
        Adult = 3,
        Elder = 4
    }

    public enum PopActivityState : byte
    {
        Idle = 0,
        Moving = 1,
        Working = 2,
        Resting = 3,
        Fleeing = 4,
        InCombat = 5
    }
}