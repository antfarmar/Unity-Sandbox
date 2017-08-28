using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        //SceneManager.LoadSceneAsync("MapScene");
        SceneManager.LoadSceneAsync("MapScene", LoadSceneMode.Additive);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
			//SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().buildIndex);
			SceneManager.UnloadSceneAsync("MapScene");
            SceneManager.LoadSceneAsync("MapScene", LoadSceneMode.Additive);
        }
    }
}
