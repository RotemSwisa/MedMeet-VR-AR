using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public void GoToSimulationTest()
    {
        SceneManager.LoadScene("Simulation_Test");
    }
}