using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    GameObject lookTarget;
    public float cameraSpeed;
    // Start is called before the first frame update
    void Start()
    {
        lookTarget = GameObject.Find("MiddlePoint");

    }

    // Update is called once per frame
    void Update()
    {
        //this.transform.LookAt(GameObject.Find("MiddlePoint").transform.position);

        GameObject temp = GameObject.Find(GPTManager.currentActor);
        if(temp != null)
        {
            lookTarget = temp;
        }

        Vector3 lookDirection = lookTarget.transform.position - this.transform.position;
        lookDirection.Normalize();

        this.transform.rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(lookDirection), cameraSpeed * Time.deltaTime);

    }
}
