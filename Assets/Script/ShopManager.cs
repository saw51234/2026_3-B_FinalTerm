using Firebase.Database;
using Newtonsoft.Json;
using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("Firebase")]
    [SerializeField] string databaseUrl = "https://finalterm-b1737-default-rtdb.asia-southeast1.firebasedatabase.app/";

    [Header("UI")]
    [SerializeField] TextMeshProUGUI CoinText;
    [SerializeField] TextMeshProUGUI MessageText;

    string userKey;
    int currentCoin;
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

        LoadUserData();
    }

    void LoadUserData()
    {
        reference
            .Child("UserInfo")
            .Child(userKey)
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

                if (!snapshot.Exists)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "No user data. Please log in again.";
                    });
                    return;
                }

                currentCoin = int.Parse(snapshot.Child("Coin").Value.ToString());

                string inventoryJson = snapshot.Child("Inventory").Value.ToString();
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
        CoinText.text = "Coin : " + currentCoin;
    }

    // EnergyDrink: 80 coins / FireScroll: 150 coins / GoldKey: 250 coins
    public void OnClickBuyEnergyDrink()
    {
        BuyItem("EnergyDrink", 80);
    }

    public void OnClickBuyFireScroll()
    {
        BuyItem("FireScroll", 150);
    }

    public void OnClickBuyGoldKey()
    {
        BuyItem("GoldKey", 250);
    }

    void BuyItem(string itemName, int price)
    {
        if (currentCoin < price)
        {
            MessageText.text = "Not enough coins. (Have: " + currentCoin + " / Need: " + price + ")";
            return;
        }

        currentCoin -= price;

        if (inventory.ContainsKey(itemName))
        {
            inventory[itemName]++;
        }
        else
        {
            inventory[itemName] = 1;
        }

        SaveUserData(itemName);
    }

    void SaveUserData(string boughtItemName)
    {
        string inventoryJson = JsonConvert.SerializeObject(inventory);

        Dictionary<string, object> updateData = new Dictionary<string, object>();
        updateData["Coin"] = currentCoin;
        updateData["Inventory"] = inventoryJson;

        reference
            .Child("UserInfo")
            .Child(userKey)
            .UpdateChildrenAsync(updateData)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "Save failed.";
                    });
                    return;
                }

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = boughtItemName + " bought! (Coins left: " + currentCoin + ")";
                });
            });
    }
}
