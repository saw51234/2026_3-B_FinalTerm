using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadShopScene() => SceneManager.LoadScene("ShopScene");
    public void LoadInventoryScene() => SceneManager.LoadScene("InventoryScene");
    public void LoadLoginScene() => SceneManager.LoadScene("LoginScene");
    public void LoadUnitShopScene() => SceneManager.LoadScene("UnitShopScene");
    public void LoadGameScene() => SceneManager.LoadScene("GameScene");
}
