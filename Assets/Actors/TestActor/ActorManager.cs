using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorManager : MonoBehaviour
{
    Animator anim;
    // Start is called before the first frame update
    void Start()
    {
        anim = this.GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        



        if (GPTManager.currentActor == this.gameObject.name)
        {
            anim.SetBool("isTalking", true);
        }
        else
        {   
            anim.SetBool("isTalking", false);
            GameObject lookTarget = null;
            try
            {
                lookTarget = GameObject.Find(GPTManager.currentActor);
            }
            catch { }
            if (lookTarget != null)
            {
                Vector3 lookDirection = lookTarget.transform.position - this.transform.position;
                lookDirection.Normalize();

                this.transform.rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(lookDirection), 2 * Time.deltaTime);
            }
        }
    }


}
