using System.Collections;
using Firebase.Database;
using PimDeWitte.UnityMainThreadDispatcher;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserRegister : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("Firebase")]
    [SerializeField] string databaseUrl = "https://finalterm-b1737-default-rtdb.asia-southeast1.firebasedatabase.app/";

    [Header("UI")]
    [SerializeField] InputField NickNameInput;
    [SerializeField] TextMeshProUGUI CheckText;

    void Start()
    {
        database = FirebaseDatabase.GetInstance(databaseUrl);
        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();
    }

    public void OnClickRegister()
    {
        string nickName = NickNameInput.text.Trim();

        if (string.IsNullOrEmpty(nickName))
        {
            CheckText.text = "Please enter a name.";
            return;
        }

        CheckDuplicateNickName(nickName);
    }

    void CheckDuplicateNickName(string nickName)
    {
        reference
            .Child("UserInfo")
            .OrderByChild("NickName")
            .EqualTo(nickName)
            .GetValueAsync()
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        CheckText.text = "Read error.";
                    });
                    return;
                }

                DataSnapshot snapshot = task.Result;

                if (snapshot.HasChildren)
                {
                    dispatcher.Enqueue(() =>
                    {
                        CheckText.text = "Name already taken.";
                    });
                    return;
                }

                CreateUser(nickName);
            });
    }

    void CreateUser(string nickName)
    {
        DatabaseReference newUserRef = reference.Child("UserInfo").Push();
        string userKey = newUserRef.Key;

        UserData userData = new UserData(nickName);
        string json = JsonUtility.ToJson(userData);

        newUserRef.SetRawJsonValueAsync(json).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    CheckText.text = "Sign up failed.";
                });
                return;
            }

            dispatcher.Enqueue(() =>
            {
                PlayerPrefs.SetString("UserKey", userKey);
                PlayerPrefs.SetString("UserNickName", nickName);
                PlayerPrefs.Save();

                CheckText.text = "Sign up OK! Welcome, " + nickName + "!";
            });
        });
    }
}
