using UnityEngine;
using UnityEngine.UI;

public class PlayerStock : MonoBehaviour
{
    [Header("一の位：Shell1〜Shell9")]
    [SerializeField] private GameObject[] shell1to9;

    [Header("十の位：Shells10〜Shells50")]
    [SerializeField] private GameObject[] shells10to50;

    [Header("地雷ストック：Mine1〜Mine3")]
    [SerializeField] private GameObject[] mineImages;

    // ---------------------- 砲弾 UI 更新 ----------------------
    public void UpdateShellStock(int currentShells)
    {
        int ones = currentShells % 10;
        int tens = currentShells / 10;

        for (int i = 0; i < shell1to9.Length; i++)
            shell1to9[i].SetActive(i < ones);

        for (int i = 0; i < shells10to50.Length; i++)
            shells10to50[i].SetActive(i < tens);

        Debug.Log($"[PlayerStock] Shells={currentShells}, tens={tens}, ones={ones}");
    }

    // ---------------------- 地雷 UI 更新 ----------------------
    public void UpdateMineStock(int currentMines)
    {
        for (int i = 0; i < mineImages.Length; i++)
            mineImages[i].SetActive(i < currentMines);

        Debug.Log($"[PlayerStock] Mines={currentMines}");
    }
}
