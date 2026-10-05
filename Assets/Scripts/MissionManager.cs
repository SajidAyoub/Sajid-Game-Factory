using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MissionManager : MonoBehaviour
{
    // Local prototype data, not secure against editing/deletion. Never reuse mission IDs.
    private const int SaveSchemaVersion = 1;

    [Serializable]
    private class MissionSave
    {
        public int schemaVersion = -1;
        public int definitionVersion = -1;
        public int missionType = -1;
        public int targetAmount = -1;
        public int rewardAmount = -1;
        public int progress = -1;
        // Nonzero encodings detect omitted fields (JsonUtility can default them to zero).
        public int claimState;
    }

    [SerializeField] private RewardManager rewardManager;
    [SerializeField] private MissionDefinition[] activeMissions = new MissionDefinition[0];
    private readonly HashSet<string> warnings = new HashSet<string>();
    private readonly HashSet<string> claimsInProgress = new HashSet<string>();
    private bool pendingSave;

    public event Action<string, int> MissionProgressChanged;
    public event Action<string> MissionCompleted;
    public event Action<string, int> MissionRewardClaimed;

    public void ReportCoinsCollected(int amount = 1) => ReportProgress(MissionType.CollectCoins, amount);
    public void ReportScore(int score) => ReportProgress(MissionType.ReachScore, score);
    public void ReportLevelCompleted() => ReportProgress(MissionType.CompleteLevels, 1);
    public void ReportRunPlayed() => ReportProgress(MissionType.PlayRuns, 1);

    public void ReportProgress(MissionType type, int amount)
    {
        if (amount <= 0 || activeMissions == null || !Enum.IsDefined(typeof(MissionType), type)) return;

        foreach (MissionDefinition definition in activeMissions)
        {
            if (definition == null || definition.Type != type ||
                !TryGetDefinition(definition.MissionId, out MissionDefinition mission) ||
                !TryReadState(mission, out int progress, out bool claimed)) continue;

            int next = type == MissionType.ReachScore
                ? Math.Min(mission.TargetAmount, Math.Max(progress, amount))
                : progress + Math.Min(amount, mission.TargetAmount - progress);
            if (next == progress || !WriteState(mission, next, claimed)) continue;

            RunnerPersistence.InvokeSafely(MissionProgressChanged, mission.MissionId, next, this);
            if (progress < mission.TargetAmount && next == mission.TargetAmount)
                RunnerPersistence.InvokeSafely(MissionCompleted, mission.MissionId, this);
        }
    }

    public int GetMissionProgress(string id)
    {
        return TryGetDefinition(id, out MissionDefinition mission) &&
            TryReadState(mission, out int progress, out bool claimed) ? progress : -1;
    }

    public bool IsMissionComplete(string id)
    {
        return TryGetDefinition(id, out MissionDefinition mission) &&
            TryReadState(mission, out int progress, out bool claimed) && progress == mission.TargetAmount;
    }

    public bool IsMissionRewardClaimed(string id)
    {
        return TryGetDefinition(id, out MissionDefinition mission) &&
            TryReadState(mission, out int progress, out bool claimed) && claimed;
    }

    public bool CanClaimMissionReward(string id)
    {
        return rewardManager != null && RunnerPersistence.IsValidId(id) &&
            !claimsInProgress.Contains(id) && TryGetDefinition(id, out MissionDefinition mission) &&
            TryReadState(mission, out int progress, out bool claimed) && !claimed &&
            progress == mission.TargetAmount &&
            mission.RewardAmount <= int.MaxValue - rewardManager.GetRewardBalance();
    }

    public bool ClaimMissionReward(string id)
    {
        if (!CanClaimMissionReward(id) || !TryGetDefinition(id, out MissionDefinition mission)) return false;
        claimsInProgress.Add(id);
        try
        {
            // Stage record and permanent claim markers before currency events.
            if (!rewardManager.AddRewardsWithPersistence(mission.RewardAmount, () =>
            {
                if (!WriteState(mission, mission.TargetAmount, true, false))
                    throw new InvalidOperationException("Mission claim data could not be staged.");
            })) return false;

            RunnerPersistence.InvokeSafely(MissionRewardClaimed, id, mission.RewardAmount, this);
            return true;
        }
        catch (Exception exception)
        {
            // Preserve any claim markers: storage failure is not permission to award twice.
            pendingSave = true;
            Debug.LogException(exception, this);
            return false;
        }
        finally
        {
            claimsInProgress.Remove(id);
        }
    }

    private bool TryGetDefinition(string id, out MissionDefinition mission)
    {
        mission = null;
        if (!RunnerPersistence.IsValidId(id) || activeMissions == null) return false;
        foreach (MissionDefinition candidate in activeMissions)
        {
            if (candidate == null || candidate.MissionId != id) continue;
            if (mission != null)
            {
                Warn(id, "Duplicate active mission ID; definition rejected.");
                return false;
            }
            mission = candidate;
        }
        if (mission == null || !mission.IsValid()) return false;

        var identities = new HashSet<string>(mission.GetPreviousMissionIds()) { id };
        foreach (MissionDefinition other in activeMissions)
        {
            if (other == null || other == mission) continue;
            if (identities.Contains(other.MissionId))
            {
                Warn(id, "Mission ID or rename alias overlaps another active mission.");
                return false;
            }
            foreach (string alias in other.GetPreviousMissionIds())
            {
                if (!identities.Contains(alias)) continue;
                Warn(id, "Mission rename aliases overlap another active mission.");
                return false;
            }
        }
        return true;
    }

    private bool TryReadState(MissionDefinition mission, out int progress, out bool claimed)
    {
        progress = 0;
        claimed = false;
        var ids = new List<string> { mission.MissionId };
        ids.AddRange(mission.GetPreviousMissionIds());
        bool hasCanonicalRecord = PlayerPrefs.HasKey(RecordKey(mission.MissionId));
        foreach (string id in ids)
        {
            if (!RunnerPersistence.TryReadFlag(ClaimedKey(id), out bool oldClaimed))
            {
                oldClaimed = true; // Unknown history is not eligible for another reward.
                Warn(id, "Invalid claim history; reward conservatively marked consumed.");
            }
            claimed |= oldClaimed;

            if (PlayerPrefs.HasKey(RecordKey(id)))
            {
                MissionSave saved;
                try { saved = JsonUtility.FromJson<MissionSave>(PlayerPrefs.GetString(RecordKey(id), string.Empty)); }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                    saved = null;
                }
                if (saved != null && saved.schemaVersion > SaveSchemaVersion)
                {
                    // Never overwrite data from a newer application/schema.
                    Warn(id, "Newer mission schema is unsupported; this mission is unavailable.");
                    return false;
                }
                if (saved == null || saved.schemaVersion != SaveSchemaVersion ||
                    saved.definitionVersion <= 0 || saved.targetAmount <= 0 || saved.rewardAmount <= 0 ||
                    saved.missionType <= 0 || !Enum.IsDefined(typeof(MissionType), saved.missionType - 1) ||
                    (saved.claimState != 1 && saved.claimState != 2))
                {
                    claimed = true;
                    Warn(id, "Corrupt mission record reset; uncertain reward history marked consumed.");
                    continue;
                }
                claimed |= saved.claimState == 2;
                // Import alias progress only once. Re-importing an old record would
                // undo later definition-version resets on the canonical mission.
                if (id != mission.MissionId && hasCanonicalRecord) continue;
                int validProgress = Math.Max(0, Math.Min(saved.progress, saved.targetAmount));
                if (saved.progress != validProgress)
                    Warn(id, "Invalid mission progress clamped to its saved target.");

                if (saved.definitionVersion == mission.DefinitionVersion && saved.missionType == (int)mission.Type + 1)
                {
                    progress = Math.Max(progress, Math.Min(validProgress, mission.TargetAmount));
                }
                else
                {
                    // Version/type changes reset progress, but never clear claim history.
                    Warn(id, "Mission definition changed; progress reset while claim history is retained.");
                }
            }
            else if (PlayerPrefs.HasKey(ProgressKey(id)))
            {
                if (id != mission.MissionId && hasCanonicalRecord) continue;
                // Migration of the original two-key save format; metadata was not available.
                int legacy = PlayerPrefs.GetInt(ProgressKey(id), -1);
                if (legacy < 0)
                {
                    Warn(id, "Invalid legacy mission progress reset to zero.");
                }
                else
                {
                    progress = Math.Max(progress, Math.Min(legacy, mission.TargetAmount));
                }
            }
        }
        return WriteState(mission, progress, claimed);
    }

    private bool WriteState(MissionDefinition mission, int progress, bool claimed, bool save = true)
    {
        var record = new MissionSave
        {
            schemaVersion = SaveSchemaVersion,
            definitionVersion = mission.DefinitionVersion,
            missionType = (int)mission.Type + 1,
            targetAmount = mission.TargetAmount,
            rewardAmount = mission.RewardAmount,
            progress = progress,
            claimState = claimed ? 2 : 1
        };
        try
        {
            string json = JsonUtility.ToJson(record);
            bool changed = PlayerPrefs.GetString(RecordKey(mission.MissionId), string.Empty) != json;
            if (changed) PlayerPrefs.SetString(RecordKey(mission.MissionId), json);
            if (claimed)
            {
                var ids = new List<string> { mission.MissionId };
                ids.AddRange(mission.GetPreviousMissionIds());
                foreach (string id in ids)
                {
                    // Retain these tombstones even if missions disappear from the catalog.
                    if (PlayerPrefs.GetInt(ClaimedKey(id), -1) == 1) continue;
                    PlayerPrefs.SetInt(ClaimedKey(id), 1);
                    changed = true;
                }
            }
            if (save && (changed || pendingSave))
            {
                PlayerPrefs.Save();
                pendingSave = false;
            }
            return true;
        }
        catch (Exception exception)
        {
            pendingSave = true;
            Debug.LogException(exception, this);
            return false;
        }
    }

    private void Warn(string id, string message)
    {
        if (warnings.Add(id + ":" + message)) Debug.LogWarning(message + " ID: " + id, this);
    }

    private static string RecordKey(string id) => "Runner.Missions.Record." + id;
    private static string ProgressKey(string id) => "Runner.Missions.Progress." + id;
    private static string ClaimedKey(string id) => "Runner.Missions.Claimed." + id;
}
