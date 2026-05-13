using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    public void GameStart()
    {
        SceneManager.LoadScene("Stage_1");
    }

    public void GameExit()
    {
        Application.Quit();
    }

    public void Stage1()
    {
        SceneManager.LoadScene("Stage_1");
    }

    public void Stage2()
    {
        SceneManager.LoadScene("Stage_2");
    }

    public void Stage3()
    {
        SceneManager.LoadScene("Stage_3");
    }

    public void Stage4()
    {
        SceneManager.LoadScene("Stage_4");
    }

    public void Stage5()
    {
        SceneManager.LoadScene("Stage_5");
    }
}
