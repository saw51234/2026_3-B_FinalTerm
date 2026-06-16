using Firebase.Database;
using Newtonsoft.Json;
using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitShopManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("Firebase")]
    [SerializeField] string databaseUrl = "https://finalterm-b1737-default-rtdb.asia-southeast1.firebasedatabase.app/";

    [Header("UI")]
    [SerializeField] TextMeshProUGUI CoinText;
    [SerializeField] TextMeshProUGUI MessageText;
    [SerializeField] TextMeshProUGUI Unit2StatusText;
    [SerializeField] TextMeshProUGUI Unit3StatusText;
    [SerializeField] TextMeshProUGUI Unit4StatusText;

    string userKey;
    int currentCoin;
    Dictionary<string, bool> unitList = new Dictionary<string, bool>();

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
        reference.Child("UserInfo").Child(userKey).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() => MessageText.text = "Load failed.");
                return;
            }

            DataSnapshot snapshot = task.Result;
            if (!snapshot.Exists)
            {
                dispatcher.Enqueue(() => MessageText.text = "No user data.");
                return;
            }

            currentCoin = int.Parse(snapshot.Child("Coin").Value.ToString());
            string unitListJson = snapshot.Child("UnitList").Value.ToString();
            unitList = JsonConvert.DeserializeObject<Dictionary<string, bool>>(unitListJson);

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
        Unit2StatusText.text = "Unit2 (200 coins) : " + (GetUnitOwned("Unit2") ? "Owned" : "Not owned");
        Unit3StatusText.text = "Unit3 (300 coins) : " + (GetUnitOwned("Unit3") ? "Owned" : "Not owned");
        Unit4StatusText.text = "Unit4 (400 coins) : " + (GetUnitOwned("Unit4") ? "Owned" : "Not owned");
    }

    bool GetUnitOwned(string unitName)
    {
        return unitList.ContainsKey(unitName) && unitList[unitName];
    }

    public void OnClickBuyUnit2() { BuyUnit("Unit2", 200); }
    public void OnClickBuyUnit3() { BuyUnit("Unit3", 300); }
    public void OnClickBuyUnit4() { BuyUnit("Unit4", 400); }

    void BuyUnit(string unitName, int price)
    {
        if (GetUnitOwned(unitName))
        {
            MessageText.text = unitName + " is already owned.";
            return;
        }

        if (currentCoin < price)
        {
            MessageText.text = "Not enough coins. (Have: " + currentCoin + " / Need: " + price + ")";
            return;
        }

        currentCoin -= price;
        unitList[unitName] = true;
        SaveUserData(unitName, price);
    }

    void SaveUserData(string boughtUnitName, int price)
    {
        string unitListJson = JsonConvert.SerializeObject(unitList);

        Dictionary<string, object> updateData = new Dictionary<string, object>();
        updateData["Coin"] = currentCoin;
        updateData["UnitList"] = unitListJson;

        reference.Child("UserInfo").Child(userKey).UpdateChildrenAsync(updateData).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                currentCoin += price;
                unitList[boughtUnitName] = false;

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = "Save failed.";
                });
                return;
            }

            dispatcher.Enqueue(() =>
            {
                RefreshUI();
                MessageText.text = boughtUnitName + " purchased!";
            });
        });
    }
}
