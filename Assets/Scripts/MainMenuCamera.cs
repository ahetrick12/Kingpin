using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuCamera : MonoBehaviour
{
    public Vector3 targetLookOffset;
    public Vector2 distanceFromTargetZY = new Vector2(50, 10);
    public float rotateSpeed = 1;

    private Transform target;
    private Vector2 lookAtPos;

    // Start is called before the first frame update
    void Awake()
    {
        target = FindObjectOfType<CityGeneration>().transform;
        transform.position = target.position - Vector3.forward * distanceFromTargetZY.x + Vector3.up * distanceFromTargetZY.y;
    }

    // Update is called once per frame
    void Update()
    {
        lookAtPos = target.position + targetLookOffset;
        transform.LookAt(lookAtPos);

        transform.RotateAround(target.position, Vector3.up, -rotateSpeed * Time.deltaTime);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("Hub");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ToggleSettings()
    {

    }
}
