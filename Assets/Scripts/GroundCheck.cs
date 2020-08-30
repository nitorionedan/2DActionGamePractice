using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    private bool doesEnterGround;
    private bool doesStayGround;
    private bool doesExitGround;

    private void Start()
    {
        doesEnterGround = false;
        doesStayGround = false;
        doesExitGround = false;
    }

    public bool IsGrounded()
    {
        bool isGrounded = (doesEnterGround || doesStayGround);
        doesEnterGround = false;
        doesStayGround = false;
        doesExitGround = false;
        return isGrounded;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Ground")
        {
            doesEnterGround = true;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "Ground")
            doesStayGround = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Ground")
            doesExitGround = true;
    }
}
