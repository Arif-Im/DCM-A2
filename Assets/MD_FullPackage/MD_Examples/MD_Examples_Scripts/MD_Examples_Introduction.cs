using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sample script for MD Package example content. Legacy input only!
/// </summary>
public class MD_Examples_Introduction : MonoBehaviour
{ 
#if UNITY_EDITOR
    private void Awake()
    {
        MD_Package_Editor.MD_StartupWizard.GenerateScenesToBuild();
    }
#endif

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    public void LoadLevel(string LVL_Name)
	{
        if (SceneManager.sceneCountInBuildSettings > 1)
        {
            try
            {
                SceneManager.LoadScene(LVL_Name);
            }
            catch
            {
                Debug.Log("Can't load level. Please press 'stop' and press 'play' once again to refresh Build Settings");
            }
        }
        else Debug.Log("Can't load level. Please press 'stop' and press 'play' once again to refresh Build Settings");
    }

    public void OpenUrl(string url)
    {
        Application.OpenURL(url);
    }

    public void OpenDocumentation()
    {
        Application.OpenURL("https://docs.google.com/presentation/d/13Utk_hVY304c7QoQPSVG7nHXV5W5RjXzgZIhsKFvUDE/edit?usp=sharing");
    }

    public void OpenDiscord()
    {
        Application.OpenURL("https://discord.gg/WdcYHBtCfr");
    }
}