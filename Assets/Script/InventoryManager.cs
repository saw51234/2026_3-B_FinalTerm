using Firebase.Database;
using Newtonsoft.Json;
using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("Firebase")]
    [SerializeField] string databaseUrl = "https://finalterm-b1737-default-rtdb.asia-southeast1.firebasedatabase.app/";

    [Header("UI")]
    [SerializeField] TextMeshProUGUI EnergyDrinkCountText;
    [SerializeField] TextMeshProUGUI FireScrollCountText;
    [SerializeField] TextMeshProUGUI GoldKeyCountText;
    [SerializeField] TextMeshProUGUI MessageText;

    string userKey;
    Dictionary<string, int> inventory = new Dictionary<string, int>();

    void Start()
    {
        database = FirebaseDatabase.GetInstance(databaseUrl);
        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();

        userKey = PlayerPrefs.GetString("UserKey");

        if (string.IsNullOrEmpty(userKey))
        {
            MessageText.text = "Not logged in.";
            return;
        }

        LoadInventory();
    }

    void LoadInventory()
    {
        reference
            .Child("UserInfo")
            .Child(userKey)
            .Child("Inventory")
            .GetValueAsync()
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "Load failed.";
                    });
                    return;
                }

                DataSnapshot snapshot = task.Result;

                if (snapshot.Value == null)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "No inventory data.";
                    });
                    return;
                }

                string inventoryJson = snapshot.Value.ToString();
                inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = "Load complete.";
                });
            });
    }

    void RefreshUI()
    {
        EnergyDrinkCountText.text = "EnergyDrink : " + GetItemCount("EnergyDrink");
        FireScrollCountText.text = "FireScroll : " + GetItemCount("FireScroll");
        GoldKeyCountText.text = "GoldKey : " + GetItemCount("GoldKey");
    }

    int GetItemCount(string itemName)
    {
        if (inventory.ContainsKey(itemName))
        {
            return inventory[itemName];
        }

        return 0;
    }

    public void OnClickUseEnergyDrink()
    {
        UseItem("EnergyDrink");
    }

    public void OnClickUseFireScroll()
    {
        UseItem("FireScroll");
    }

    public void OnClickUseGoldKey()
    {
        UseItem("GoldKey");
    }

    void UseItem(string itemName)
    {
        if (!inventory.ContainsKey(itemName) || inventory[itemName] <= 0)
        {
            MessageText.text = itemName + " is empty.";
            return;
        }

        inventory[itemName]--;
        SaveInventory(itemName);
    }

    string GetUseMessage(string itemName)
    {
        switch (itemName)
        {
            case "EnergyDrink":
                return "EnergyDrink used! HP restored.";
            case "FireScroll":
                return "FireScroll used! Fire magic cast.";
            case "GoldKey":
                return "GoldKey used! Gold box opened.";
            default:
                return itemName + " used.";
        }
    }

    void SaveInventory(string usedItemName)
    {
        int savedCount = inventory[usedItemName];

        string inventoryJson = JsonConvert.SerializeObject(inventory);

        reference
            .Child("UserInfo")
            .Child(userKey)
            .Child("Inventory")
            .SetValueAsync(inventoryJson)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    inventory[usedItemName] = savedCount + 1;

                    dispatcher.Enqueue(() =>
                    {
                        RefreshUI();
                        MessageText.text = "Save failed. Try again.";
                    });
                    return;
                }

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = GetUseMessage(usedItemName);
                });
            });
    }
}
