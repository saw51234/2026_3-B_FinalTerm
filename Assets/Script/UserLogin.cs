using System.Collections;
using Firebase.Database;
using PimDeWitte.UnityMainThreadDispatcher;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UserLogin : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("Firebase")]
    [SerializeField] string databaseUrl = "https://finalterm-b1737-default-rtdb.asia-southeast1.firebasedatabase.app/";

    [Header("UI")]
    [SerializeField] InputField NickNameInput;
    [SerializeField] TextMeshProUGUI CheckText;

    [Header("Scene")]
    [SerializeField] string NextSceneName = "ShopScene";

    void Start()
    {
        database = FirebaseDatabase.GetInstance(databaseUrl);
        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();
    }

    public void OnClickLogin()
    {
        string nickName = NickNameInput.text.Trim();

        if (string.IsNullOrEmpty(nickName))
        {
            CheckText.text = "Please enter a name.";
            return;
        }

        Login(nickName);
    }

    void Login(string nickName)
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

                if (!snapshot.HasChildren)
                {
                    dispatcher.Enqueue(() =>
                    {
                        CheckText.text = "Name not found.";
                    });
                    return;
                }

                foreach (DataSnapshot userSnapshot in snapshot.Children)
                {
                    string userKey = userSnapshot.Key;

                    dispatcher.Enqueue(() =>
                    {
                        PlayerPrefs.SetString("UserKey", userKey);
                        PlayerPrefs.SetString("UserNickName", nickName);
                        PlayerPrefs.Save();

                        CheckText.text = "Login OK! Hello, " + nickName + "!";
                        StartCoroutine(LoadSceneAfterDelay(3f));
                    });

                    break;
                }
            });
    }

    IEnumerator LoadSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(NextSceneName);
    }
}
