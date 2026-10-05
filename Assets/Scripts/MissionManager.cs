using System;
using System.Collections.Generic;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    [SerializeField] private RewardManager rewardManager;
    [SerializeField] private MissionDefinition[] activeMissions = new MissionDefinition[0];
    private readonly HashSet<string> warnedAboutInvalidData = new HashSet<string>();
    private readonly HashSet<string> claimsInProgress = new HashSet<string>();

    public event Action<string, int> MissionProgressChanged;
    public event Action<string> MissionCompleted;
    public event Action<string, int> MissionRewardClaimed;

    public void ReportCoinsCollected(int amount = 1) => ReportProgress(MissionType.CollectCoins, amount);
    public void ReportScore(int score) => ReportProgress(MissionType.ReachScore, score);
    public void ReportLevelCompleted() => ReportProgress(MissionType.CompleteLevels, 1);
    public void ReportRunPlayed() => ReportProgress(MissionType.PlayRuns, 1);

    public void ReportProgress(MissionType type, int amount)
    {
        if (amount <= 0 || activeMissions == null || !Enum.IsDefined(typeof(MissionType), type))
        {
            return;
        }

        foreach (MissionDefinition definition in activeMissions)
        {
            if (definition == null || definition.Type != type ||
                !TryGetDefinition(definition.MissionId, out MissionDefinition mission) ||
                !TryReadState(mission, out int progress, out bool claimed) || claimed)
            {
                continue;
            }

            // Score missions track the highest reported score, not summed score updates.
            int nextProgress = type == MissionType.ReachScore
                ? Math.Min(mission.TargetAmount, Math.Max(progress, amount))
                : progress + Math.Min(amount, mission.TargetAmount - progress);
            if (nextProgress == progress)
            {
                continue;
            }

            PlayerPrefs.SetInt(ProgressKey(mission.MissionId), nextProgress);
            PlayerPrefs.Save();
            RunnerPersistence.InvokeSafely(MissionProgressChanged, mission.MissionId, nextProgress, this);
            if (progress < mission.TargetAmount && nextProgress == mission.TargetAmount)
            {
                RunnerPersistence.InvokeSafely(MissionCompleted, mission.MissionId, this);
            }
        }
    }

    public int GetMissionProgress(string missionId)
    {
        return TryGetDefinition(missionId, out MissionDefinition mission) &&
            TryReadState(mission, out int progress, out bool claimed) ? progress : -1;
    }

    public bool IsMissionComplete(string missionId)
    {
        return TryGetDefinition(missionId, out MissionDefinition mission) &&
            TryReadState(mission, out int progress, out bool claimed) &&
            progress == mission.TargetAmount;
    }

    public bool IsMissionRewardClaimed(string missionId)
    {
        return TryGetDefinition(missionId, out MissionDefinition mission) &&
            TryReadState(mission, out int progress, out bool claimed) && claimed;
    }

    public bool CanClaimMissionReward(string missionId)
    {
        return rewardManager != null && RunnerPersistence.IsValidId(missionId) &&
            !claimsInProgress.Contains(missionId) &&
            TryGetDefinition(missionId, out MissionDefinition mission) &&
            TryReadState(mission, out int progress, out bool claimed) && !claimed &&
            progress == mission.TargetAmount &&
            mission.RewardAmount <= int.MaxValue - rewardManager.GetRewardBalance();
    }

    public bool ClaimMissionReward(string missionId)
    {
        if (!CanClaimMissionReward(missionId) ||
            !TryGetDefinition(missionId, out MissionDefinition mission))
        {
            return false;
        }

        claimsInProgress.Add(missionId);
        try
        {
            // Persist claimed state with currency before reward/UI callbacks fire.
            if (!rewardManager.AddRewardsWithPersistence(mission.RewardAmount,
                () => PlayerPrefs.SetInt(ClaimedKey(missionId), 1)))
            {
                return false;
            }

            RunnerPersistence.InvokeSafely(MissionRewardClaimed, missionId, mission.RewardAmount, this);
            return true;
        }
        finally
        {
            claimsInProgress.Remove(missionId);
        }
    }

    private bool TryGetDefinition(string missionId, out MissionDefinition mission)
    {
        mission = null;
        if (!RunnerPersistence.IsValidId(missionId) || activeMissions == null)
        {
            return false;
        }

        foreach (MissionDefinition candidate in activeMissions)
        {
            if (candidate == null || candidate.MissionId != missionId) continue;
            if (mission != null) return false;
            mission = candidate;
        }

        return mission != null && mission.IsValid();
    }

    private bool TryReadState(MissionDefinition mission, out int progress, out bool claimed)
    {
        string key = ProgressKey(mission.MissionId);
        progress = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key, -1) : 0;
        bool validClaimed = RunnerPersistence.TryReadFlag(ClaimedKey(mission.MissionId), out claimed);
        if (progress < 0 || progress > mission.TargetAmount || !validClaimed ||
            (claimed && progress != mission.TargetAmount))
        {
            if (warnedAboutInvalidData.Add(mission.MissionId))
            {
                Debug.LogWarning("Invalid saved mission data; mission updates and claims are blocked: " +
                    mission.MissionId, this);
            }

            return false;
        }

        return true;
    }

    private static string ProgressKey(string id) => "Runner.Missions.Progress." + id;
    private static string ClaimedKey(string id) => "Runner.Missions.Claimed." + id;
}
