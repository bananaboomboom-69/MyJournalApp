namespace myjournal.Models;

/// <summary>
/// Represents mood types for journal entries
/// </summary>
public enum MoodType
{
    Happy,
    Excited,
    Grateful,
    Calm,
    Neutral,
    Anxious,
    Sad,
    Angry,
    Tired,
    Stressed,
    Motivated,
    Peaceful,
    Loving,
    Hopeful,
    Confused
}

/// <summary>
/// Extension methods for MoodType
/// </summary>
public static class MoodExtensions
{
    public static string GetEmoji(this MoodType mood) => mood switch
    {
        MoodType.Happy => "😊",
        MoodType.Excited => "🎉",
        MoodType.Grateful => "🙏",
        MoodType.Calm => "😌",
        MoodType.Neutral => "😐",
        MoodType.Anxious => "😰",
        MoodType.Sad => "😢",
        MoodType.Angry => "😠",
        MoodType.Tired => "😴",
        MoodType.Stressed => "😫",
        MoodType.Motivated => "💪",
        MoodType.Peaceful => "☮️",
        MoodType.Loving => "❤️",
        MoodType.Hopeful => "🌟",
        MoodType.Confused => "😕",
        _ => "😐"
    };

    public static string GetColor(this MoodType mood) => mood switch
    {
        MoodType.Happy => "#FFD700",
        MoodType.Excited => "#FF6B6B",
        MoodType.Grateful => "#98D8C8",
        MoodType.Calm => "#87CEEB",
        MoodType.Neutral => "#B0B0B0",
        MoodType.Anxious => "#DDA0DD",
        MoodType.Sad => "#6495ED",
        MoodType.Angry => "#FF4500",
        MoodType.Tired => "#708090",
        MoodType.Stressed => "#FF8C00",
        MoodType.Motivated => "#32CD32",
        MoodType.Peaceful => "#E6E6FA",
        MoodType.Loving => "#FF69B4",
        MoodType.Hopeful => "#FFFACD",
        MoodType.Confused => "#D3D3D3",
        _ => "#B0B0B0"
    };
}
