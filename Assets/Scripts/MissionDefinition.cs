using UnityEngine;

public enum MissionType
{
    CollectCoins = 0,
    ReachScore = 1,
    CompleteLevels = 2,
    PlayRuns = 3
}

[CreateAssetMenu(fileName = "Mission", menuName = "Endless Runner/Mission")]
public class MissionDefinition : ScriptableObject
{
    [SerializeField] private string missionId;
    [SerializeField] private MissionType missionType;
    [SerializeField, Min(1)] private int targetAmount = 1;
    [SerializeField, Min(1)] private int rewardAmount = 10;
    [Tooltip("Increase for incompatible semantics. Progress resets; claimed rewards stay consumed.")]
    [SerializeField, Min(1)] private int definitionVersion = 1;
    [Tooltip("Previous IDs for this same mission. Never reuse IDs or aliases for different missions.")]
    [SerializeField] private string[] previousMissionIds = new string[0];

    public string MissionId => missionId;
    public MissionType Type => missionType;
    public int TargetAmount => targetAmount;
    public int RewardAmount => rewardAmount;
    public int DefinitionVersion => definitionVersion;
    public string[] GetPreviousMissionIds() => previousMissionIds == null
        ? new string[0] : (string[])previousMissionIds.Clone();

    public bool IsValid()
    {
        if (!RunnerPersistence.IsValidId(missionId) || targetAmount <= 0 ||
            rewardAmount <= 0 || definitionVersion <= 0 ||
            !System.Enum.IsDefined(typeof(MissionType), missionType)) return false;

        var ids = new System.Collections.Generic.HashSet<string> { missionId };
        foreach (string oldId in GetPreviousMissionIds())
            if (!RunnerPersistence.IsValidId(oldId) || !ids.Add(oldId)) return false;
        return true;
    }
}
