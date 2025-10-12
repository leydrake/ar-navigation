using UnityEngine;
using UnityEngine.SceneManagement;

public class GoBackToMainScene : MonoBehaviour
{
    public void GoBack()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void GoBackToScene1()
    {
        SceneManager.LoadScene("scene1");
    }


}
