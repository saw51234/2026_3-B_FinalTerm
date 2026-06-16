using Firebase.Database;
using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameResultManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("Firebase")]
    [SerializeField] string databaseUrl = "https://finalterm-b1737-default-rtdb.asia-southeast1.firebasedatabase.app/";

    [Header("UI")]
    [SerializeField] TextMeshProUGUI BestScoreText;
    [SerializeField] TextMeshProUGUI CurrentScoreText;
    [SerializeField] TextMeshProUGUI CoinText;
    [SerializeField] TextMeshProUGUI MessageText;
    [SerializeField] Button PlayButton;

    string userKey;
    int currentCoin;
    int bestScore;

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
            bestScore = int.Parse(snapshot.Child("Score").Value.ToString());

            dispatcher.Enqueue(() =>
            {
                RefreshUI(0);
                MessageText.text = "Press Play to start!";
            });
        });
    }

    void RefreshUI(int latestScore)
    {
        CoinText.text = "Coin : " + currentCoin;
        BestScoreText.text = "Best Score : " + bestScore;
        CurrentScoreText.text = "Score : " + latestScore;
    }

    public void OnClickPlay()
    {
        StartCoroutine(SimulateGame());
    }

    IEnumerator SimulateGame()
    {
        PlayButton.interactable = false;
        MessageText.text = "Playing...";

        yield return new WaitForSeconds(2f);

        int newScore = Random.Range(0, 1001);
        int rewardCoin = newScore / 10;

        currentCoin += rewardCoin;

        bool isNewBest = newScore > bestScore;
        if (isNewBest) bestScore = newScore;

        RefreshUI(newScore);
        SaveGameResult(newScore, isNewBest, rewardCoin);
    }

    void SaveGameResult(int newScore, bool isNewBest, int rewardCoin)
    {
        Dictionary<string, object> updateData = new Dictionary<string, object>();
        updateData["Coin"] = currentCoin;

        if (isNewBest)
            updateData["Score"] = bestScore;

        reference.Child("UserInfo").Child(userKey).UpdateChildrenAsync(updateData).ContinueWith(task =>
        {
            dispatcher.Enqueue(() =>
            {
                PlayButton.interactable = true;

                if (task.IsFaulted)
                {
                    MessageText.text = "Save failed.";
                    return;
                }

                string msg = "Game over! Score: " + newScore + " | Reward: +" + rewardCoin + " coins";
                if (isNewBest) msg += "\nNew best score!";
                MessageText.text = msg;
            });
        });
    }
}
