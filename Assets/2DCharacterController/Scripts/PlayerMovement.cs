using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    // Uses Blend Trees to switch between animations.
    Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");

        // bool isWalking = 0 < (Mathf.Abs(inputX) + Mathf.Abs(inputY));
        bool isWalking = inputX != 0 || inputY != 0;
        animator.SetBool("isWalking", isWalking);

        if (isWalking)
        {
            animator.SetFloat("x", inputX);
            animator.SetFloat("y", inputY);
            Vector3 direction = new Vector3(inputX, inputY, 0).normalized;
            transform.Translate(Time.deltaTime * direction);
        }
    }
}
