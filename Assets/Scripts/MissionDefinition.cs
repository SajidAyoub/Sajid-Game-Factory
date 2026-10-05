using UnityEngine;

public enum MissionType
{
    CollectCoins,
    ReachScore,
    CompleteLevels,
    PlayRuns
}

[CreateAssetMenu(fileName = "Mission", menuName = "Endless Runner/Mission")]
public class MissionDefinition : ScriptableObject
{
    [SerializeField] private string missionId;
    [SerializeField] private MissionType missionType;
    [SerializeField, Min(1)] private int targetAmount = 1;
    [SerializeField, Min(1)] private int rewardAmount = 10;

    public string MissionId => missionId;
    public MissionType Type => missionType;
    public int TargetAmount => targetAmount;
    public int RewardAmount => rewardAmount;

    public bool IsValid()
    {
        return RunnerPersistence.IsValidId(missionId) && targetAmount > 0 &&
            rewardAmount > 0 && System.Enum.IsDefined(typeof(MissionType), missionType);
    }
}
