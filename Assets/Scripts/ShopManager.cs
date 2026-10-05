using System;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Serializable]
    public class ShopItem
    {
        public string itemId;
        [Min(0)] public int price;
    }

    public enum PurchaseFailure
    {
        InvalidItem,
        InvalidOwnershipData,
        AlreadyOwned,
        MissingRewardManager,
        InsufficientRewards,
        PurchaseInProgress,
        PersistenceError
    }

    [SerializeField] private RewardManager rewardManager;
    [SerializeField] private ShopItem[] items = new ShopItem[0];
    private bool purchasing;
    private bool notifyingFailure;

    public event Action<string> PurchaseSucceeded;
    public event Action<string, PurchaseFailure> PurchaseFailed;

    public bool CanPurchase(string itemId)
    {
        return !TryGetFailure(itemId, out _);
    }

    public bool PurchaseItem(string itemId)
    {
        if (TryGetFailure(itemId, out PurchaseFailure failure))
        {
            NotifyFailure(itemId, failure);
            return false;
        }

        int price = GetItemPrice(itemId);
        int balanceBefore = rewardManager.GetRewardBalance();
        bool debitAttempted = false;
        purchasing = true;
        try
        {
            // Stage ownership before currency callbacks to prevent duplicate purchases.
            // SpendRewards flushes both changes together; PlayerPrefs is not transactional.
            RunnerPersistence.SetOwned(itemId);
            debitAttempted = price > 0;
            if (debitAttempted && !rewardManager.SpendRewards(price))
            {
                RunnerPersistence.SetUnowned(itemId);

                PlayerPrefs.Save();
                NotifyFailure(itemId, PurchaseFailure.InsufficientRewards);
                return false;
            }

            if (price == 0)
            {
                PlayerPrefs.Save();
            }

            RunnerPersistence.InvokeSafely(PurchaseSucceeded, itemId, this);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            // A failed Save can leave changed values in memory. Undo ownership and
            // compensate a detected debit before reporting failure. No callbacks run
            // between the staged ownership write and SpendRewards' save.
            try
            {
                RunnerPersistence.SetUnowned(itemId);

                if (debitAttempted && rewardManager != null &&
                    rewardManager.GetRewardBalance() == balanceBefore - price)
                {
                    if (!rewardManager.AddRewards(price))
                        Debug.LogError("Purchase refund could not be applied.", this);
                }
                else
                {
                    PlayerPrefs.Save();
                }
            }
            catch (Exception recoveryException)
            {
                Debug.LogException(recoveryException, this);
                Debug.LogError("Purchase recovery could not be persisted; local storage requires attention.", this);
            }

            NotifyFailure(itemId, PurchaseFailure.PersistenceError);
            return false;
        }
        finally
        {
            purchasing = false;
        }
    }

    public bool IsOwned(string itemId)
    {
        return RunnerPersistence.TryReadOwned(itemId, out bool owned) && owned;
    }

    private void NotifyFailure(string itemId, PurchaseFailure failure)
    {
        // A retry inside a failure callback must not recursively emit failures forever.
        if (notifyingFailure)
        {
            return;
        }

        notifyingFailure = true;
        try
        {
            RunnerPersistence.InvokeSafely(PurchaseFailed, itemId, failure, this);
        }
        finally
        {
            notifyingFailure = false;
        }
    }

    public int GetItemPrice(string itemId)
    {
        return TryGetItem(itemId, out ShopItem item) ? item.price : -1;
    }

    private bool TryGetFailure(string itemId, out PurchaseFailure failure)
    {
        failure = PurchaseFailure.InvalidItem;
        if (purchasing)
        {
            failure = PurchaseFailure.PurchaseInProgress;
            return true;
        }

        if (!TryGetItem(itemId, out ShopItem item))
        {
            return true;
        }

        if (!RunnerPersistence.TryReadOwned(itemId, out bool owned))
        {
            failure = PurchaseFailure.InvalidOwnershipData;
            return true;
        }

        if (owned)
        {
            failure = PurchaseFailure.AlreadyOwned;
            return true;
        }

        if (rewardManager == null)
        {
            failure = PurchaseFailure.MissingRewardManager;
            return true;
        }

        if (rewardManager.GetRewardBalance() < item.price)
        {
            failure = PurchaseFailure.InsufficientRewards;
            return true;
        }

        return false;
    }

    private bool TryGetItem(string itemId, out ShopItem item)
    {
        item = null;
        if (!RunnerPersistence.IsValidId(itemId) || items == null)
        {
            return false;
        }

        foreach (ShopItem candidate in items)
        {
            if (candidate == null || candidate.itemId != itemId)
            {
                continue;
            }

            // Ambiguous IDs are rejected rather than picking an arbitrary price.
            if (item != null)
            {
                return false;
            }

            item = candidate;
        }

        return item != null && item.price >= 0;
    }
}

// Local prototype persistence only: editable, not secure or transactional.
// Shared data contract, not a dependency between ShopManager and SkinManager objects.
internal static class RunnerPersistence
{
    internal static bool IsValidId(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length > 64)
        {
            return false;
        }

        foreach (char character in id)
        {
            if (!((character >= 'a' && character <= 'z') ||
                (character >= 'A' && character <= 'Z') ||
                (character >= '0' && character <= '9') ||
                character == '_' || character == '-' || character == '.'))
            {
                return false;
            }
        }

        return true;
    }

    internal static string OwnershipKey(string id) => "Runner.Items.Owned." + id;
    private static string OwnershipBackupKey(string id) => "Runner.Items.OwnedBackup." + id;

    internal static bool TryReadOwned(string id, out bool owned)
    {
        owned = false;
        if (!IsValidId(id)) return false;
        bool validPrimary = TryReadFlag(OwnershipKey(id), out bool primary);
        bool validBackup = TryReadFlag(OwnershipBackupKey(id), out bool backup);
        owned = (validPrimary && primary) || (validBackup && backup);
        if (validPrimary && validBackup && primary == backup) return true;

        // Preserve a valid owned copy. If neither copy proves ownership, recover as
        // unowned. This redundant copy detects accidental damage, not deliberate edits.
        if (!validPrimary || !validBackup || (!primary && backup))
            Debug.LogWarning("Recovering inconsistent local ownership: " + id);
        try
        {
            if (owned) SetOwned(id);
            else SetUnowned(id);
            PlayerPrefs.Save();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return false;
        }
    }

    internal static bool TryReadFlag(string key, out bool value)
    {
        value = false;
        if (!PlayerPrefs.HasKey(key))
        {
            return true;
        }

        int saved = PlayerPrefs.GetInt(key, -1);
        if (saved != 0 && saved != 1)
        {
            return false;
        }

        value = saved == 1;
        return true;
    }

    internal static void SetOwned(string id)
    {
        PlayerPrefs.SetInt(OwnershipKey(id), 1);
        PlayerPrefs.SetInt(OwnershipBackupKey(id), 1);
    }

    internal static void SetUnowned(string id)
    {
        PlayerPrefs.SetInt(OwnershipKey(id), 0);
        PlayerPrefs.SetInt(OwnershipBackupKey(id), 0);
    }

    internal static void InvokeSafely<T>(Action<T> listeners, T value, UnityEngine.Object context)
    {
        if (listeners == null)
        {
            return;
        }

        foreach (Action<T> listener in listeners.GetInvocationList())
        {
            try
            {
                listener(value);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, context);
            }
        }
    }

    internal static void InvokeSafely<T1, T2>(Action<T1, T2> listeners,
        T1 first, T2 second, UnityEngine.Object context)
    {
        if (listeners == null)
        {
            return;
        }

        foreach (Action<T1, T2> listener in listeners.GetInvocationList())
        {
            try
            {
                listener(first, second);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, context);
            }
        }
    }
}
