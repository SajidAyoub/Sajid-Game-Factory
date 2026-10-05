using System;
using UnityEngine;

public class SkinManager : MonoBehaviour
{
    private const string SelectedSkinKey = "Runner.Skins.Selected";

    [SerializeField] private string defaultSkinId = "default";
    [SerializeField] private string[] availableSkinIds = new string[0];

    public event Action<string> SkinChanged;
    public event Action<string> SkinUnlocked;

    private void Awake()
    {
        if (!RunnerPersistence.IsValidId(defaultSkinId))
        {
            Debug.LogError("Configure a valid default skin ID.", this);
            return;
        }

        EnsureDefaultOwned();
        GetSelectedSkin();
    }

    public bool IsSkinOwned(string skinId)
    {
        if (!IsAvailable(skinId))
        {
            return false;
        }

        if (skinId == defaultSkinId)
        {
            EnsureDefaultOwned();
            return true;
        }

        return RunnerPersistence.TryReadOwned(skinId, out bool owned) && owned;
    }

    public bool UnlockSkin(string skinId)
    {
        if (!IsAvailable(skinId))
        {
            return false;
        }

        if (skinId == defaultSkinId)
        {
            EnsureDefaultOwned();
            return true;
        }

        if (!RunnerPersistence.TryReadOwned(skinId, out bool owned))
        {
            return false;
        }

        if (!owned)
        {
            RunnerPersistence.SetOwned(skinId);
            PlayerPrefs.Save();
            RunnerPersistence.InvokeSafely(SkinUnlocked, skinId, this);
        }

        return true;
    }

    public bool SelectSkin(string skinId)
    {
        if (!IsSkinOwned(skinId))
        {
            return false;
        }

        if (GetSelectedSkin() == skinId)
        {
            return true;
        }

        PlayerPrefs.SetString(SelectedSkinKey, skinId);
        PlayerPrefs.Save();
        RunnerPersistence.InvokeSafely(SkinChanged, skinId, this);
        return true;
    }

    public string GetSelectedSkin()
    {
        if (!RunnerPersistence.IsValidId(defaultSkinId))
        {
            return string.Empty;
        }

        string selected = PlayerPrefs.GetString(SelectedSkinKey, string.Empty);
        if (IsSkinOwned(selected))
        {
            return selected;
        }

        // Corrupt, removed or unowned selections safely fall back to the default.
        EnsureDefaultOwned();
        PlayerPrefs.SetString(SelectedSkinKey, defaultSkinId);
        PlayerPrefs.Save();
        return defaultSkinId;
    }

    private bool IsAvailable(string skinId)
    {
        if (!RunnerPersistence.IsValidId(skinId) ||
            !RunnerPersistence.IsValidId(defaultSkinId))
        {
            return false;
        }

        if (skinId == defaultSkinId)
        {
            return true;
        }

        int matches = 0;
        if (availableSkinIds != null)
        {
            foreach (string availableId in availableSkinIds)
            {
                if (availableId == skinId) matches++;
            }
        }

        return matches == 1;
    }

    private void EnsureDefaultOwned()
    {
        if (!RunnerPersistence.TryReadOwned(defaultSkinId, out bool owned) || !owned)
        {
            RunnerPersistence.SetOwned(defaultSkinId);
            PlayerPrefs.Save();
        }
    }
}
