using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SkinManager : MonoBehaviour
{
    private const string SelectedSkinKey = "Runner.Skins.Selected";
    private const string FallbackSkinId = "default";

    [SerializeField] private string defaultSkinId = "default";
    [SerializeField] private string[] availableSkinIds = new string[0];
    private bool notifyingSkinChange;

    public event Action<string> SkinChanged;
    public event Action<string> SkinUnlocked;

    private void Awake()
    {
        if (!RunnerPersistence.IsValidId(defaultSkinId))
        {
            RecoverDefaultSkin();
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
        RecoverDefaultSkin();
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
            try
            {
                RunnerPersistence.SetOwned(skinId);
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                try { RunnerPersistence.SetUnowned(skinId); PlayerPrefs.Save(); }
                catch (Exception recoveryException) { Debug.LogException(recoveryException, this); }
                return false;
            }
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

        string previous = GetSelectedSkin();
        try
        {
            PlayerPrefs.SetString(SelectedSkinKey, skinId);
            PlayerPrefs.Save();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            try { PlayerPrefs.SetString(SelectedSkinKey, previous); PlayerPrefs.Save(); }
            catch (Exception recoveryException) { Debug.LogException(recoveryException, this); }
            return false;
        }
        NotifySkinChanged(skinId);
        return true;
    }

    public string GetSelectedSkin()
    {
        RecoverDefaultSkin();

        string selected = PlayerPrefs.GetString(SelectedSkinKey, string.Empty);
        if (IsSkinOwned(selected))
        {
            return selected;
        }

        // Corrupt, removed or unowned selections safely fall back to the default.
        EnsureDefaultOwned();
        try
        {
            PlayerPrefs.SetString(SelectedSkinKey, defaultSkinId);
            PlayerPrefs.Save();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
        if (!string.IsNullOrEmpty(selected)) NotifySkinChanged(defaultSkinId);
        return defaultSkinId;
    }

    private bool IsAvailable(string skinId)
    {
        RecoverDefaultSkin();
        if (!RunnerPersistence.IsValidId(skinId))
        {
            return false;
        }

        if (skinId == defaultSkinId)
        {
            return true;
        }

        if (availableSkinIds != null)
        {
            foreach (string availableId in availableSkinIds)
            {
                // Repeated skin catalog entries represent one entitlement.
                if (availableId == skinId) return true;
            }
        }

        return false;
    }

    private void EnsureDefaultOwned()
    {
        RecoverDefaultSkin();
        if (!RunnerPersistence.TryReadOwned(defaultSkinId, out bool owned) || !owned)
        {
            try
            {
                RunnerPersistence.SetOwned(defaultSkinId);
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                // The default remains available even if local storage is unwritable.
                Debug.LogException(exception, this);
            }
        }
    }

    private void RecoverDefaultSkin()
    {
        if (!RunnerPersistence.IsValidId(defaultSkinId))
        {
            Debug.LogWarning("Invalid default skin ID; using 'default'.", this);
            defaultSkinId = FallbackSkinId;
        }
    }

    private void NotifySkinChanged(string id)
    {
        if (notifyingSkinChange) return;
        notifyingSkinChange = true;
        try { RunnerPersistence.InvokeSafely(SkinChanged, id, this); }
        finally { notifyingSkinChange = false; }
    }
}
